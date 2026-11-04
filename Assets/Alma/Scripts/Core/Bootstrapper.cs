using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Runs before the first scene loads and creates the persistent [Systems] object
    /// (game manager, input, audio, scene loader, room tracker, UI). This is why you can
    /// press Play in any scene without setup.
    ///
    /// To customize, make a prefab at Resources/Systems.prefab: it's used instead, and any
    /// missing managers are added to it.
    /// </summary>
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (GameManager.Instance != null) return;

            if (!Layers.AllLayersExist())
            {
                Debug.LogWarning("[Alma] Some physics layers are missing (Ground, OneWayPlatform, Player, Enemy, Breakable, Pickup). " +
                                 "Run Alma > Setup > Create Layers, or reopen the project so the editor creates them.");
            }
            Layers.ConfigureCollisionMatrix();

            var prefab = Resources.Load<GameObject>("Systems");
            var systems = prefab != null ? Object.Instantiate(prefab) : new GameObject();
            systems.name = "[Systems]";
            Object.DontDestroyOnLoad(systems);

            // Order matters: later managers use earlier ones in Awake.
            Ensure<InputReader>(systems);
            Ensure<AudioManager>(systems);
            Ensure<DreadMeter>(systems);
            Ensure<SceneLoader>(systems);
            Ensure<RoomTracker>(systems);
            Ensure<UIManager>(systems);
            Ensure<GameManager>(systems);
            if (GameConfig.Get().enableCheats && (Application.isEditor || Debug.isDebugBuild)) Ensure<DebugCheats>(systems);
        }

        static T Ensure<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
