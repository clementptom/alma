using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Sneaking, hiding and disguises. Hold SNEAK to move slowly and silently. Standing
    /// still or sneaking inside a HidingSpot hides you from sight. A disguise (Act III)
    /// fools the Hollowed as long as you don't run or fight.
    /// </summary>
    public class PlayerStealth : MonoBehaviour
    {
        [Range(0.1f, 1f)] public float sneakSpeedMultiplier = 0.45f;

        public bool IsSneaking { get; private set; }
        public bool IsHidden { get; private set; }
        public bool IsDisguised { get; private set; }

        /// <summary>Running, rolling or fighting: things that break a disguise or a hiding spot.</summary>
        public bool ActingSuspicious
        {
            get
            {
                if (controller == null) return false;
                bool running = !IsSneaking && Mathf.Abs(controller.Velocity.x) > controller.runSpeed * 0.6f;
                return running || controller.IsRolling || (controller.Combat != null && controller.Combat.IsAttacking);
            }
        }

        PlayerController controller;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            var input = InputReader.Instance;
            var gm = GameManager.Instance;
            bool canAct = controller != null && !controller.IsFrozen && (gm == null || gm.State == GameState.Playing);
            IsSneaking = canAct && input != null && input.SneakHeld && controller.IsGrounded && !controller.IsRolling;

            bool still = controller != null && Mathf.Abs(controller.Velocity.x) < 0.3f;
            IsHidden = controller != null && HidingSpot.At(controller.Center) != null && (IsSneaking || still)
                       && (controller.Combat == null || !controller.Combat.IsAttacking);
        }

        public void SetDisguised(bool disguised)
        {
            if (IsDisguised == disguised) return;
            IsDisguised = disguised;
            GameEvents.RaiseToast(disguised ? "Disguised. Walk slowly." : "Disguise removed.");
        }
    }
}
