using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Shows a hint at the bottom of the screen the first time the player walks into this
    /// area. Write {Action} to insert the current key or button, e.g. "Hold {Sneak} to sneak."
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PersistentId))]
    public class TutorialHint : MonoBehaviour
    {
        [TextArea] public string text = "";
        [Tooltip("Show again every visit instead of once per save.")]
        public bool repeat = false;

        PersistentId pid;
        Rect area;
        bool inside;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            var box = GetComponent<BoxCollider2D>();
            area = RectUtil.FromBox(box);
            box.enabled = false;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            bool now = area.Contains(player.Center);
            if (now && !inside && (repeat || !pid.IsFlagSet()))
            {
                pid.SetFlag();
                GameEvents.RaiseHint(Format(text));
            }
            inside = now;
        }

        /// <summary>Replaces {Jump}, {Sneak}, {Throw}... with the player's current bindings.</summary>
        public static string Format(string text)
        {
            var input = InputReader.Instance;
            if (input == null || string.IsNullOrEmpty(text)) return text;
            foreach (var action in InputReader.RebindableActions)
                text = text.Replace("{" + action + "}", input.PromptFor(action));
            return text.Replace("{Pause}", input.PromptFor("Pause"));
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? area : RectUtil.FromBox(box);
            Gizmos.color = new Color(0.6f, 1f, 0.6f, 0.5f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
