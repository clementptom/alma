using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>Something that raises Dread when the player is near: the Hollowed, the Drowned, Geryon.</summary>
    public class DreadSource : MonoBehaviour
    {
        public float radius = 6f;
        [Tooltip("Dread per second when the player is right next to it (less further away).")]
        public float ratePerSecond = 6f;

        static readonly List<DreadSource> active = new List<DreadSource>();
        public static IReadOnlyList<DreadSource> Active => active;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public float RateAt(Vector2 position)
        {
            float d = Vector2.Distance(position, transform.position);
            if (d > radius) return 0f;
            var enemy = GetComponent<Enemy>();
            if (enemy != null && enemy.IsDead) return 0f;
            return ratePerSecond * (1f - d / Mathf.Max(0.01f, radius));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.1f, 0.8f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
