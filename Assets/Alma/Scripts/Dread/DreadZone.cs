using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// An area that raises Dread while the player is inside (crypts, dark channels, the set
    /// tables) or lowers it when the rate is negative (by a fire, in the hermitage).
    /// </summary>
    public class DreadZone : AreaVolume
    {
        [Tooltip("Dread per second while inside. Negative values calm the player.")]
        public float ratePerSecond = 5f;

        static readonly List<DreadZone> active = new List<DreadZone>();
        public static IReadOnlyList<DreadZone> Active => active;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        protected override Color GizmoColor => ratePerSecond >= 0f ? new Color(0.6f, 0.1f, 0.8f, 0.6f) : new Color(1f, 0.7f, 0.3f, 0.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
