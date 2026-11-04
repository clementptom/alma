using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Water the player can wade or swim through: slower, floatier, and you can keep
    /// "jumping" (swimming) upward. Drowned Ones lurk in it.
    /// </summary>
    public class WaterVolume : AreaVolume
    {
        [Range(0.1f, 1f)] public float speedMultiplier = 0.55f;
        [Range(0.1f, 1f)] public float jumpMultiplier = 0.6f;
        [Range(0.05f, 1f)] public float gravityMultiplier = 0.35f;
        public float maxFallSpeed = 3f;

        static readonly List<WaterVolume> active = new List<WaterVolume>();

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public static WaterVolume At(Vector2 point)
        {
            foreach (var w in active)
                if (w != null && w.Contains(point)) return w;
            return null;
        }

        protected override Color GizmoColor => new Color(0.2f, 0.5f, 1f, 0.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
