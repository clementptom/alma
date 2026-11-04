using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A room is a rectangle of the world (set by the BoxCollider2D, used only for its shape).
    /// While the player is inside it, the camera stays within its bounds, its music and
    /// ambience play, and it's added to the map.
    ///
    /// Map tip: lay out every scene in one shared world coordinate system (for example, put
    /// Area2 to the right of Area1), so the map pieces line up across scenes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PersistentId))]
    public class Room : MonoBehaviour
    {
        public string displayName = "Room";
        [Tooltip("Music ID from AudioIds / AudioLibrary. Empty keeps the current music.")]
        public string musicId = "";
        [Tooltip("Ambience loop ID. Empty keeps the current ambience.")]
        public string ambienceId = "";
        public bool showOnMap = true;
        [Tooltip("Keep the camera inside this room.")]
        public bool clampCamera = true;

        static readonly List<Room> active = new List<Room>();
        public static IReadOnlyList<Room> Active => active;

        public Rect WorldRect { get; private set; }
        public string Id => pid != null ? pid.Id : "";

        PersistentId pid;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            var box = GetComponent<BoxCollider2D>();
            WorldRect = RectUtil.FromBox(box);
            box.enabled = false; // only used as an editing handle
        }

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        void OnDisable()
        {
            active.Remove(this);
        }

        public bool Contains(Vector2 point) => WorldRect.Contains(point);

        /// <summary>The smallest room containing the point (so a small room inside a big one wins).</summary>
        public static Room FindAt(Vector2 point)
        {
            Room best = null;
            float bestArea = float.MaxValue;
            foreach (var room in active)
            {
                if (room == null || !room.Contains(point)) continue;
                float area = room.WorldRect.width * room.WorldRect.height;
                if (area < bestArea)
                {
                    best = room;
                    bestArea = area;
                }
            }
            return best;
        }

        void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(32f, 18f);
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? WorldRect : RectUtil.FromBox(box);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(r.center, r.size);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
