using UnityEngine;
using UnityEngine.Video;

namespace Alma
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Map,
        Dialogue,
        Cutscene,
        Popup,
        /// <summary>A full-screen menu (inventory, shop, fast travel, controls) is open.</summary>
        Overlay,
        Transition,
        Dead
    }

    /// <summary>
    /// Global game settings. Lives at Resources/GameConfig.asset (created by
    /// Alma > Create Data Assets). Every value has a safe default if the asset is missing.
    /// </summary>
    [CreateAssetMenu(menuName = "Alma/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Scenes")]
        public string mainMenuScene = "MainMenu";
        [Tooltip("Scene loaded when starting a new game.")]
        public string startScene = "Area1";
        [Tooltip("SpawnPoint id inside the start scene.")]
        public string startSpawnId = "start";

        [Header("Player")]
        [Tooltip("Prefab spawned when a gameplay scene has no player. If empty, uses Resources/Player, then a generated placeholder.")]
        public GameObject playerPrefab;
        public int startingMaxHealth = 5;
        [Tooltip("Energy capacity at the start. With the default heal cost of 33, 99 = three heals.")]
        public int startingMaxEnergy = 99;
        [Range(0f, 1f)] public float currencyLostOnDeath = 0f;

        [Header("Story")]
        [Tooltip("Optional video played when the player starts a new game.")]
        public VideoClip introCinematic;
        [Tooltip("Text scrolled by the end credits (one line per line). Empty uses a built-in placeholder.")]
        public TextAsset credits;

        [Header("Debug")]
        [Tooltip("When you press Play directly inside a level (not via the menu), unlock every ability.")]
        public bool debugUnlockAllAbilitiesWhenPlayingFromLevel = false;
        [Tooltip("Enable cheat keys in the editor and development builds (F1-F6). See DebugCheats.cs.")]
        public bool enableCheats = true;

        static GameConfig cached;

        public static GameConfig Get()
        {
            if (cached == null)
            {
                cached = Resources.Load<GameConfig>("GameConfig");
                if (cached == null)
                {
                    cached = CreateInstance<GameConfig>();
                    cached.name = "GameConfig (defaults)";
                }
            }
            return cached;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cached = null;
    }
}
