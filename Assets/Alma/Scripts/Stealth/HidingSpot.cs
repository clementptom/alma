using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Shadows, alcoves, hay, bushes. Standing still or sneaking inside one hides the player
    /// from enemy sight (unless an enemy is right next to them).
    /// </summary>
    public class HidingSpot : AreaVolume
    {
        static readonly List<HidingSpot> active = new List<HidingSpot>();

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public static HidingSpot At(Vector2 point)
        {
            foreach (var h in active)
                if (h != null && h.Contains(point)) return h;
            return null;
        }

        protected override Color GizmoColor => new Color(0.3f, 0.3f, 0.3f, 0.8f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
