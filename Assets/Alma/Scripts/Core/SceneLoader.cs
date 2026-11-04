using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alma
{
    /// <summary>Fades out, loads a scene, places the player at a spawn id, and fades back in.</summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public float fadeOutTime = 0.3f;
        public float fadeInTime = 0.35f;

        public bool IsLoading { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Used by doors: loads another scene, or moves within this one if the scene is empty/current.</summary>
        public void Travel(string sceneName, string spawnId)
        {
            if (IsLoading) return;
            if (string.IsNullOrEmpty(sceneName) || sceneName == SceneManager.GetActiveScene().name)
                StartCoroutine(SameSceneRoutine(spawnId));
            else
                Load(sceneName, spawnId);
        }

        public void Load(string sceneName, string spawnId, Action onLoaded = null)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName, spawnId, onLoaded));
        }

        ScreenFader Fader => UIManager.Instance != null ? UIManager.Instance.Fader : null;

        IEnumerator LoadRoutine(string sceneName, string spawnId, Action onLoaded)
        {
            IsLoading = true;
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Dead) gm.SetState(GameState.Transition);

            var player = PlayerController.Instance;
            if (player != null) player.Freeze();

            if (Fader != null) yield return Fader.Fade(1f, fadeOutTime);

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Alma] Scene '{sceneName}' isn't in File > Build Profiles (Build Settings), or doesn't exist.");
                if (player != null) player.Unfreeze();
                if (gm != null) gm.SetState(gm.IsInGameplayScene ? GameState.Playing : GameState.Menu);
                if (Fader != null) yield return Fader.Fade(0f, fadeInTime);
                IsLoading = false;
                yield break;
            }

            if (gm != null) gm.PendingSpawnId = spawnId;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return null; // let Start() run on the new scene's objects

            onLoaded?.Invoke();

            if (player != null && player == PlayerController.Instance) player.Unfreeze();
            if (gm != null)
            {
                gm.SetState(gm.IsInGameplayScene ? GameState.Playing : GameState.Menu);
                gm.CompleteArrival();
            }

            if (Fader != null) yield return Fader.Fade(0f, fadeInTime);
            IsLoading = false;
        }

        IEnumerator SameSceneRoutine(string spawnId)
        {
            IsLoading = true;
            var gm = GameManager.Instance;
            if (gm != null) gm.SetState(GameState.Transition);
            var player = PlayerController.Instance;
            if (player != null) player.Freeze();

            if (Fader != null) yield return Fader.Fade(1f, fadeOutTime);

            var location = gm != null ? gm.FindSpawnLocation(spawnId) : null;
            if (location != null && player != null) player.Teleport(location.SpawnPosition);
            else Debug.LogWarning($"[Alma] No spawn location with id '{spawnId}' in this scene.");
            if (RoomTracker.Instance != null) RoomTracker.Instance.Refresh(true);

            if (player != null) player.Unfreeze();
            if (gm != null) gm.SetState(GameState.Playing);
            if (location != null && player != null) location.OnPlayerArrived(player);

            if (Fader != null) yield return Fader.Fade(0f, fadeInTime);
            IsLoading = false;
        }
    }
}
