using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Base for rectangular areas defined by a BoxCollider2D (water, hiding spots, dread
    /// zones). The collider is only an editing handle: it's disabled at runtime and the area
    /// is tested with a plain rectangle, so it never blocks or triggers physics.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class AreaVolume : MonoBehaviour
    {
        public Rect Area { get; private set; }

        protected virtual void Awake()
        {
            var box = GetComponent<BoxCollider2D>();
            Area = RectUtil.FromBox(box);
            box.enabled = false;
        }

        public bool Contains(Vector2 point) => Area.Contains(point);

        protected virtual Color GizmoColor => new Color(1f, 1f, 1f, 0.4f);

        protected virtual void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? Area : RectUtil.FromBox(box);
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
