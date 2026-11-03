using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Alma
{
    /// <summary>
    /// Creates and owns the persistent UI: HUD, dialogue box, popups, map, pause menu,
    /// cinematic player and screen fader. All of it is built in code from UISkin, so it
    /// works with no prefabs. Swap pieces for designed prefabs later.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public Canvas Canvas { get; private set; }
        public HUD Hud { get; private set; }
        public DialogueUI Dialogue { get; private set; }
        public PopupUI Popup { get; private set; }
        public MapUI Map { get; private set; }
        public PauseMenu Pause { get; private set; }
        public CinematicPlayer Cinematic { get; private set; }
        public ScreenFader Fader { get; private set; }

        EventSystem eventSystem;
        GameObject lastSelected;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            CreateEventSystem();
            Canvas = UIBuilder.CreateCanvas("[UI]", 100, transform);
            var root = Canvas.transform;

            // Sibling order = draw order (later draws on top).
            Hud = Create<HUD>("HUD", root);
            Dialogue = Create<DialogueUI>("Dialogue", root);
            Popup = Create<PopupUI>("Popup", root);
            Map = Create<MapUI>("Map", root);
            Pause = Create<PauseMenu>("Pause", root);
            Cinematic = Create<CinematicPlayer>("Cinematic", root);
            Fader = Create<ScreenFader>("Fader", root);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        static T Create<T>(string name, Transform parent) where T : UIPanel
        {
            var rt = UIBuilder.CreateRect(name, parent);
            UIBuilder.Stretch(rt);
            var panel = rt.gameObject.AddComponent<T>();
            panel.Build();
            return panel;
        }

        void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            eventSystem = go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Keep a single EventSystem: remove ones that came with the scene.
            foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (es != eventSystem) Destroy(es.gameObject);
            }
            if (eventSystem != null) EventSystem.current = eventSystem;
        }

        void Update()
        {
            // Menu navigation sound.
            var es = EventSystem.current;
            if (es == null) return;
            var selected = es.currentSelectedGameObject;
            if (selected != lastSelected)
            {
                if (selected != null && lastSelected != null) AudioManager.PlaySfx(AudioIds.UiMove);
                lastSelected = selected;
            }
        }

        public void SetHudVisible(bool visible)
        {
            if (Hud != null) Hud.SetVisible(visible);
        }

        /// <summary>Selects a UI element for keyboard/gamepad navigation.</summary>
        public static void Select(GameObject target)
        {
            var es = EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(null);
            if (target != null) es.SetSelectedGameObject(target);
        }
    }
}
