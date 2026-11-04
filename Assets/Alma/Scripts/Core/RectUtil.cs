using UnityEngine;

namespace Alma
{
    public static class RectUtil
    {
        /// <summary>World-space rectangle covered by a BoxCollider2D, even while the collider is disabled.</summary>
        public static Rect FromBox(BoxCollider2D box)
        {
            if (box == null) return new Rect();
            Transform t = box.transform;
            Vector2 center = t.TransformPoint(box.offset);
            Vector3 scale = t.lossyScale;
            Vector2 size = new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
            return new Rect(center - size * 0.5f, size);
        }
    }
}
