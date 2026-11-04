using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Alma
{
    /// <summary>
    /// An object Barro can carry back with the FETCH command: a key behind bars, a rope on a
    /// ledge, a pickup you can't reach. Put it on a Pickup and it's collected as soon as
    /// Barro drops it at your feet.
    /// </summary>
    public class BarroFetchable : MonoBehaviour
    {
        public float fetchRange = 16f;
        public UnityEvent onDelivered = new UnityEvent();

        static readonly List<BarroFetchable> active = new List<BarroFetchable>();
        public static IReadOnlyList<BarroFetchable> Active => active;

        public bool Carried { get; set; }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public static BarroFetchable Nearest(Vector2 from)
        {
            BarroFetchable best = null;
            float bestDistance = float.MaxValue;
            foreach (var f in active)
            {
                if (f == null || f.Carried) continue;
                float d = Vector2.Distance(from, f.transform.position);
                if (d > f.fetchRange || d >= bestDistance) continue;
                best = f;
                bestDistance = d;
            }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
