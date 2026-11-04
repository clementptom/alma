using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alma
{
    /// <summary>
    /// Owns the current save (progress, abilities, flags), the game state (playing, paused,
    /// dialogue...), the persistent player, death and respawn, pausing and hit-stop.
    /// Created automatically on startup by Bootstrapper, so pressing Play in any level works.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public SaveData Data { get; private set; }
        public int Slot { get; private set; } = SaveSystem.DebugSlot;
        public GameState State { get; private set; } = GameState.Menu;
        public PlayerController Player => PlayerController.Instance;
        public bool IsInGameplayScene { get; private set; }

        /// <summary>Spawn id to use when the next scene finishes loading.</summary>
        public string PendingSpawnId { get; set; }

        float hitStopTimer;
        bool hazardRespawning;
        bool dying;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Config = GameConfig.Get();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            // Normally sceneLoaded fires for the first scene too. If Enter Play Mode settings
            // skip the scene reload it doesn't, so handle the current scene here instead.
            if (!handledAnyScene) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
            Time.timeScale = 1f;
        }

        // ================================================================ State

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            stateChangedFrame = Time.frameCount;
            var input = InputReader.Instance;
            if (input != null) input.GameplayEnabled = newState == GameState.Playing;
            ApplyTimeScale();
            GameEvents.RaiseStateChanged(newState);
        }

        void ApplyTimeScale()
        {
            bool frozen = State == GameState.Paused || State == GameState.Map || State == GameState.Popup
                          || State == GameState.Overlay;
            if (frozen) Time.timeScale = 0f;
            else if (hitStopTimer > 0f) Time.timeScale = 0.02f;
            else Time.timeScale = 1f;
        }

        /// <summary>Freezes time very briefly to make hits feel heavy.</summary>
        public void HitStop(float seconds)
        {
            if (seconds <= 0f || State != GameState.Playing) return;
            hitStopTimer = Mathf.Max(hitStopTimer, seconds);
            ApplyTimeScale();
        }

        void Update()
        {
            if (hitStopTimer > 0f)
            {
                hitStopTimer -= Time.unscaledDeltaTime;
                if (hitStopTimer <= 0f) ApplyTimeScale();
            }

            if (Data != null && IsInGameplayScene && State != GameState.Menu)
                Data.playTimeSeconds += Time.unscaledDeltaTime;

            var input = InputReader.Instance;
            if (input == null || !IsInGameplayScene) return;
            // One menu action per frame: the press that just changed state shouldn't also act again.
            if (Time.frameCount == stateChangedFrame) return;

            switch (State)
            {
                case GameState.Playing:
                    if (input.PausePressed) Pause();
                    else if (input.MapPressed) OpenMap();
                    else if (input.InventoryPressed && UIManager.Instance != null) OpenOverlay(UIManager.Instance.Inventory);
                    break;
                case GameState.Paused:
                    if (input.PausePressed || input.CancelPressed) Resume();
                    break;
                case GameState.Map:
                    if (input.MapPressed || input.PausePressed || input.CancelPressed) CloseMap();
                    break;
                // Overlay panels handle their own Back input.
            }
        }

        int stateChangedFrame = -1;
        OverlayPanel overlay;
        GameState stateBeforeOverlay;

        /// <summary>Opens a full-screen menu and pauses the world. From the pause menu, closing it returns there.</summary>
        public void OpenOverlay(OverlayPanel panel)
        {
            if (panel == null || overlay != null) return;
            if (State != GameState.Playing && State != GameState.Paused) return;
            stateBeforeOverlay = State;
            if (State == GameState.Paused && UIManager.Instance != null) UIManager.Instance.Pause.Hide();
            overlay = panel;
            SetState(GameState.Overlay);
            panel.ShowFromGameManager();
        }

        public void CloseOverlay()
        {
            if (overlay == null) return;
            var panel = overlay;
            overlay = null;
            panel.HideFromGameManager();
            if (stateBeforeOverlay == GameState.Paused)
            {
                SetState(GameState.Paused);
                if (UIManager.Instance != null) UIManager.Instance.Pause.Show();
            }
            else
            {
                SetState(GameState.Playing);
            }
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
            AudioManager.PlaySfx(AudioIds.UiPause);
            if (UIManager.Instance != null) UIManager.Instance.Pause.Show();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            AudioManager.PlaySfx(AudioIds.UiBack);
            if (UIManager.Instance != null) UIManager.Instance.Pause.Hide();
            SetState(GameState.Playing);
        }

        public void OpenMap()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Map);
            AudioManager.PlaySfx(AudioIds.UiMapOpen);
            if (UIManager.Instance != null) UIManager.Instance.Map.Show();
        }

        public void CloseMap()
        {
            if (State != GameState.Map) return;
            AudioManager.PlaySfx(AudioIds.UiBack);
            if (UIManager.Instance != null) UIManager.Instance.Map.Hide();
            SetState(GameState.Playing);
        }

        /// <summary>Shows a centered message (new ability, upgrade) that pauses until dismissed.</summary>
        public void ShowPopup(string title, string body, Sprite icon = null)
        {
            var ui = UIManager.Instance;
            if (ui == null) return;
            var previous = State;
            SetState(GameState.Popup);
            ui.Popup.Show(title, body, icon, () =>
            {
                if (State == GameState.Popup) SetState(previous == GameState.Popup ? GameState.Playing : previous);
            });
        }

        // ============================================================ Sessions

        public void NewGame(int slot)
        {
            Slot = slot;
            Data = CreateFreshSave();
            Save(); // replaces whatever was in this slot
            StartCoroutine(NewGameRoutine());
        }

        IEnumerator NewGameRoutine()
        {
            if (Config.introCinematic != null && UIManager.Instance != null)
            {
                SetState(GameState.Cutscene);
                AudioManager.StopMusic(0.5f);
                bool finished = false;
                UIManager.Instance.Cinematic.Play(Config.introCinematic, true, () => finished = true);
                while (!finished) yield return null;
            }
            SceneLoader.Instance.Load(Config.startScene, Config.startSpawnId);
        }

        public bool ContinueGame(int slot)
        {
            var loaded = SaveSystem.Load(slot);
            if (loaded == null) return false;
            Slot = slot;
            Data = loaded;
            string scene = string.IsNullOrEmpty(Data.respawnScene) ? Config.startScene : Data.respawnScene;
            string spawn = string.IsNullOrEmpty(Data.respawnScene) ? Config.startSpawnId : Data.respawnPointId;
            SceneLoader.Instance.Load(scene, spawn);
            return true;
        }

        public void QuitToMainMenu()
        {
            if (overlay != null)
            {
                overlay.HideFromGameManager();
                overlay = null;
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.Pause.Hide();
                UIManager.Instance.Map.Hide();
            }
            SetState(GameState.Transition);
            SceneLoader.Instance.Load(Config.mainMenuScene, null);
        }

        SaveData CreateFreshSave()
        {
            var data = new SaveData
            {
                maxHealth = Mathf.Max(1, Config.startingMaxHealth),
                maxEnergy = Mathf.Max(0, Config.startingMaxEnergy),
                respawnScene = Config.startScene,
                respawnPointId = Config.startSpawnId
            };
            return data;
        }

        /// <summary>Pressing Play inside a level: start a throwaway session that never touches real saves.</summary>
        void StartDebugSession(string sceneName)
        {
            Slot = SaveSystem.DebugSlot;
            Data = CreateFreshSave();
            Data.respawnScene = sceneName;
            Data.respawnPointId = "";
            if (Config.debugUnlockAllAbilitiesWhenPlayingFromLevel)
            {
                foreach (AbilityType a in System.Enum.GetValues(typeof(AbilityType))) Data.AddAbility(a);
            }
            Debug.Log("[Alma] Started a test session from this scene. Progress saves to the debug slot only.");
        }

        public void Save()
        {
            if (Data == null) return;
            if (Player != null && Player.Energy != null) Data.energy = Player.Energy.Current;
            if (SaveSystem.Save(Slot, Data)) GameEvents.RaiseGameSaved();
        }

        public void SaveAtStation(SaveStation station)
        {
            if (Data == null) return;
            Data.respawnScene = SceneManager.GetActiveScene().name;
            Data.respawnPointId = station.SpawnId;
            var room = RoomTracker.Instance != null ? RoomTracker.Instance.Current : null;
            Data.locationName = room != null ? room.displayName : Data.respawnScene;
            RecordMarker(station.SpawnId, MapMarkerRecord.SaveStation, Data.locationName, station.transform.position);
            Save();
        }

        /// <summary>Adds or updates a map marker. Returns true if it's new.</summary>
        public bool RecordMarker(string id, string kind, string label, Vector2 position)
        {
            if (Data == null || string.IsNullOrEmpty(id)) return false;
            var marker = Data.FindMarker(id);
            bool isNew = marker == null;
            if (isNew)
            {
                marker = new MapMarkerRecord { id = id };
                Data.markers.Add(marker);
            }
            marker.kind = kind;
            marker.label = label;
            marker.scene = SceneManager.GetActiveScene().name;
            marker.x = position.x;
            marker.y = position.y;
            return isNew;
        }

        public void RegisterTravelPoint(FastTravelPoint point)
        {
            if (point == null) return;
            RecordMarker(point.SpawnId, MapMarkerRecord.Travel, point.pointName, point.transform.position);
        }

        /// <summary>Closes the travel menu and takes the player to another travel point.</summary>
        public void FastTravel(MapMarkerRecord destination)
        {
            if (destination == null) return;
            CloseOverlay();
            AudioManager.PlaySfx(AudioIds.TravelDepart);
            SceneLoader.Instance.Travel(destination.scene, destination.id);
        }

        // ======================================================= Progress API

        public bool HasFlag(string flag) => Data != null && Data.HasFlag(flag);

        public void SetFlag(string flag)
        {
            if (Data != null && Data.AddFlag(flag)) GameEvents.RaiseFlagSet(flag);
        }

        public bool HasAbility(AbilityType ability) => Data != null && Data.HasAbility(ability);

        public void UnlockAbility(AbilityType ability, string title = null, string description = null, Sprite icon = null, bool showPopup = true)
        {
            if (Data == null || !Data.AddAbility(ability)) return;
            GameEvents.RaiseAbilityUnlocked(ability);
            if (showPopup)
            {
                ShowPopup(title ?? AbilityInfo.DisplayName(ability), description ?? AbilityInfo.Description(ability), icon);
            }
        }

        public bool HasKeyItem(string item) => Data != null && Data.HasKeyItem(item);

        public void AddKeyItem(string item)
        {
            if (Data == null || string.IsNullOrEmpty(item) || Data.keyItems.Contains(item)) return;
            Data.keyItems.Add(item);
            GameEvents.RaiseKeyItemAdded(item);
        }

        public int Currency => Data != null ? Data.currency : 0;

        public void AddCurrency(int amount)
        {
            if (Data == null || amount == 0) return;
            Data.currency = Mathf.Max(0, Data.currency + amount);
            GameEvents.RaiseCurrencyChanged(Data.currency);
        }

        public bool TrySpendCurrency(int amount)
        {
            if (Data == null || Data.currency < amount) return false;
            AddCurrency(-amount);
            return true;
        }

        public void AddMaxHealth(int amount)
        {
            if (Data == null) return;
            Data.maxHealth += amount;
            if (Player != null) Player.PlayerHealth.ApplySave(Data.maxHealth, true);
        }

        public void AddMaxEnergy(int amount)
        {
            if (Data == null) return;
            Data.maxEnergy = Mathf.Max(0, Data.maxEnergy + amount);
            if (Player != null && Player.Energy != null) Player.Energy.SetMax(Data.maxEnergy, true);
        }

        public void DiscoverRoom(Room room)
        {
            if (Data == null || room == null || string.IsNullOrEmpty(room.Id)) return;
            var existing = Data.FindRoom(room.Id);
            Rect r = room.WorldRect;
            if (existing == null)
            {
                existing = new RoomRecord { id = room.Id };
                Data.rooms.Add(existing);
            }
            existing.scene = room.gameObject.scene.name;
            existing.displayName = room.displayName;
            existing.x = r.x;
            existing.y = r.y;
            existing.width = r.width;
            existing.height = r.height;
        }

        // ===================================================== Death / respawn

        public void OnPlayerDied()
        {
            if (dying) return;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            dying = true;
            SetState(GameState.Dead);
            GameEvents.RaisePlayerDied();
            yield return new WaitForSecondsRealtime(1.4f);

            if (Data != null && Config.currencyLostOnDeath > 0f)
            {
                int lost = Mathf.RoundToInt(Data.currency * Config.currencyLostOnDeath);
                AddCurrency(-lost);
            }

            string scene = Data != null && !string.IsNullOrEmpty(Data.respawnScene) ? Data.respawnScene : SceneManager.GetActiveScene().name;
            string spawn = Data != null ? Data.respawnPointId : null;
            while (SceneLoader.Instance.IsLoading) yield return null;
            SceneLoader.Instance.Load(scene, spawn, () =>
            {
                if (Player != null)
                {
                    Player.PlayerHealth.Revive();
                    Player.ClearFreeze();
                }
                dying = false;
            });
        }

        /// <summary>Spikes and pits: fade out, put the player back on the last safe ground, fade in.</summary>
        public void HazardRespawn()
        {
            if (hazardRespawning || dying) return;
            StartCoroutine(HazardRespawnRoutine());
        }

        IEnumerator HazardRespawnRoutine()
        {
            hazardRespawning = true;
            var player = Player;
            if (player != null) player.Freeze();
            yield return new WaitForSecondsRealtime(0.15f);
            if (UIManager.Instance != null) yield return UIManager.Instance.Fader.Fade(1f, 0.2f);
            if (player != null && !player.PlayerHealth.IsDead)
            {
                player.Teleport(player.LastSafePosition);
                if (CameraController.Main != null) CameraController.Main.Snap();
            }
            yield return new WaitForSecondsRealtime(0.1f);
            if (UIManager.Instance != null) yield return UIManager.Instance.Fader.Fade(0f, 0.2f);
            if (player != null) player.Unfreeze();
            hazardRespawning = false;
        }

        // ======================================================= Scene setup

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            handledAnyScene = true;
            hitStopTimer = 0f;
            hazardRespawning = false;

            EnsureCamera();

            bool isMenu = scene.name == Config.mainMenuScene || FindFirstObjectByType<MainMenu>() != null;
            IsInGameplayScene = !isMenu;

            if (isMenu)
            {
                DestroyPlayer();
                Data = null;
                SetState(GameState.Menu);
                if (UIManager.Instance != null) UIManager.Instance.SetHudVisible(false);
                return;
            }

            if (Data == null) StartDebugSession(scene.name);

            bool playerFromScene = PlayerController.Instance != null && !playerHealthInitialized;
            var player = EnsurePlayer();
            bool freshPlayer = !playerHealthInitialized;
            player.PlayerHealth.ApplySave(Data.maxHealth, player.PlayerHealth.IsDead || freshPlayer);
            player.Energy.SetMax(Data.maxEnergy, false);
            if (freshPlayer) player.Energy.SetCurrent(Data.energy);
            playerHealthInitialized = true;
            PlacePlayer(player, playerFromScene);

            if (CameraController.Main != null) CameraController.Main.SetTarget(player.transform, false);
            if (RoomTracker.Instance != null) RoomTracker.Instance.Refresh(true);
            else if (CameraController.Main != null) CameraController.Main.Snap();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHudVisible(true);
                UIManager.Instance.Hud.RefreshAll();
            }

            // SceneLoader switches to Playing after its fade-out; when pressing Play in the editor there's no fade.
            if (SceneLoader.Instance == null || !SceneLoader.Instance.IsLoading)
            {
                SetState(GameState.Playing);
                CompleteArrival();
            }
        }

        ISpawnLocation pendingArrival;

        /// <summary>Lets the spawn location react (walk out of a door...) once the player can move.</summary>
        public void CompleteArrival()
        {
            var location = pendingArrival;
            pendingArrival = null;
            if (location != null && Player != null && (location as MonoBehaviour) != null) location.OnPlayerArrived(Player);
        }

        public ISpawnLocation FindSpawnLocation(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId)) return null;
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISpawnLocation>()
                .FirstOrDefault(l => l.SpawnId == spawnId);
        }

        bool playerHealthInitialized;
        bool handledAnyScene;

        void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }
            if (cam.GetComponent<CameraController>() == null)
            {
                cam.orthographic = true;
                if (cam.orthographicSize < 1f || Mathf.Approximately(cam.orthographicSize, 5f)) cam.orthographicSize = 7f;
                cam.gameObject.AddComponent<CameraController>();
            }
        }

        PlayerController EnsurePlayer()
        {
            var player = PlayerController.Instance;
            if (player == null)
            {
                GameObject prefab = Config.playerPrefab != null ? Config.playerPrefab : Resources.Load<GameObject>("Player");
                GameObject go = prefab != null ? Instantiate(prefab) : PlayerFactory.CreatePlaceholderPlayer();
                go.name = "Player";
                player = go.GetComponent<PlayerController>();
                playerHealthInitialized = false;
            }
            if (player.transform.parent != null) player.transform.SetParent(null, true);
            DontDestroyOnLoad(player.gameObject);
            return player;
        }

        void DestroyPlayer()
        {
            var player = PlayerController.Instance;
            if (player != null) Destroy(player.gameObject);
            playerHealthInitialized = false;
        }

        void PlacePlayer(PlayerController player, bool playerFromScene)
        {
            string spawnId = PendingSpawnId;
            PendingSpawnId = null;

            var locations = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISpawnLocation>().ToList();
            ISpawnLocation location = string.IsNullOrEmpty(spawnId) ? null : locations.FirstOrDefault(l => l.SpawnId == spawnId);
            if (location == null && !string.IsNullOrEmpty(spawnId))
                Debug.LogWarning($"[Alma] No spawn location with id '{spawnId}' in scene '{SceneManager.GetActiveScene().name}'.");

            Vector2? position = null;
            if (location != null)
            {
                position = location.SpawnPosition;
            }
            else if (PlayerController.SceneDefaultSpawn.HasValue)
            {
                // The scene has its own Player placed for testing: use its position.
                position = PlayerController.SceneDefaultSpawn.Value;
            }
            else if (!(playerFromScene && string.IsNullOrEmpty(spawnId)))
            {
                // Fall back to the configured start point, then any spawn point.
                location = locations.FirstOrDefault(l => l.SpawnId == Config.startSpawnId)
                           ?? locations.FirstOrDefault(l => l is SpawnPoint);
                if (location != null) position = location.SpawnPosition;
            }
            PlayerController.SceneDefaultSpawn = null;

            if (position.HasValue) player.Teleport(position.Value);
            else player.SetLastSafePosition(player.transform.position);
            pendingArrival = location;
        }
    }
}
