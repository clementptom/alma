using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// An enemy that's hard to see: pale wolves in a snowstorm, disguised Hollowed, things in
    /// the walls. It stays faint until the player gets close or Barro growls at it.
    /// </summary>
    public class HiddenThreat : MonoBehaviour
    {
        [Range(0f, 1f)] public float hiddenAlpha = 0.12f;
        [Tooltip("Becomes visible when the player is this close.")]
        public float revealDistance = 3f;

        static readonly List<HiddenThreat> active = new List<HiddenThreat>();
        public static IReadOnlyList<HiddenThreat> Active => active;

        SpriteRenderer[] renderers;
        float revealTimer;
        float alpha;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            alpha = hiddenAlpha;
            Apply();
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// <summary>Show this threat for a few seconds (Barro growled at it).</summary>
        public void Reveal(float seconds) => revealTimer = Mathf.Max(revealTimer, seconds);

        void Update()
        {
            if (revealTimer > 0f) revealTimer -= Time.deltaTime;
            var player = PlayerController.Instance;
            bool close = player != null && Vector2.Distance(player.Center, transform.position) <= revealDistance;
            float target = revealTimer > 0f || close ? 1f : hiddenAlpha;
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * 2.5f);
            Apply();
        }

        void Apply()
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
