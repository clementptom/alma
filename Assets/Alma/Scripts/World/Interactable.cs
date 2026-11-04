using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Base class for things the player can use with INTERACT: NPCs, save stations,
    /// levers, locked doors, signs. Shows a prompt when the player is within <see cref="radius"/>.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Shown on the HUD, e.g. \"Talk\", \"Rest\", \"Read\".")]
        public string prompt = "Interact";
        public float radius = 1.6f;
        public Vector2 promptOffset = new Vector2(0f, 2.2f);
        public bool requireGrounded = true;
        [Tooltip("Seconds INTERACT must be held (fixing a roof, digging). 0 = a single press.")]
        public float holdDuration = 0f;

        static readonly List<Interactable> active = new List<Interactable>();

        public Vector2 PromptWorldPosition => (Vector2)transform.position + promptOffset;

        protected virtual void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        protected virtual void OnDisable()
        {
            active.Remove(this);
        }

        public virtual bool CanInteract(PlayerController player) => isActiveAndEnabled;

        public abstract void Interact(PlayerController player);

        public static Interactable FindBest(PlayerController player)
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            Vector2 p = player.Center;
            foreach (var i in active)
            {
                if (i == null) continue;
                if (i.requireGrounded && !player.IsGrounded) continue;
                float d = Vector2.Distance(p, i.transform.position);
                if (d > i.radius || d >= bestDistance || !i.CanInteract(player)) continue;
                best = i;
                bestDistance = d;
            }
            return best;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
