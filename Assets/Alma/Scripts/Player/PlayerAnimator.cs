using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Chooses which animation the player shows. It plays Animator states BY NAME
    /// (see AnimStates), so your Animator Controller only needs states with those names
    /// and no transitions. Missing states fall back (e.g. no "Glide" plays "Fall", then "Jump").
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public Animator animator;
        public float hurtAnimationTime = 0.25f;
        public float landAnimationTime = 0.1f;

        PlayerController controller;
        PlayerCombat combat;
        PlayerHealth health;
        CharacterAnimator anim;
        float hurtTimer;
        int lastAttackSerial;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
        }

        void Start()
        {
            if (health != null && health.Health != null) health.Health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (health != null && health.Health != null) health.Health.Damaged -= OnDamaged;
        }

        void OnDamaged(DamageInfo info) => hurtTimer = hurtAnimationTime;

        void LateUpdate()
        {
            if (hurtTimer > 0f) hurtTimer -= Time.deltaTime;
            bool restart = false;
            string state = ChooseState(ref restart);
            anim.Play(state, restart);
        }

        string ChooseState(ref bool restart)
        {
            if (health != null && health.IsDead) return AnimStates.Death;
            if (hurtTimer > 0f) return AnimStates.Hurt;
            if (controller.Energy != null && controller.Energy.IsHealing) return AnimStates.Heal;
            if (controller.IsDashing) return AnimStates.Dash;
            if (combat.IsAttacking)
            {
                if (combat.AttackSerial != lastAttackSerial)
                {
                    lastAttackSerial = combat.AttackSerial;
                    restart = true;
                }
                return combat.AnimationState;
            }
            if (controller.IsWallSliding) return AnimStates.WallSlide;
            if (!controller.IsGrounded)
            {
                if (controller.IsGliding) return AnimStates.Glide;
                if (controller.Velocity.y > 0f)
                    return controller.DoubleJumpTimer > 0f ? AnimStates.DoubleJump : AnimStates.Jump;
                return AnimStates.Fall;
            }
            if (Mathf.Abs(controller.Velocity.x) > 0.5f && controller.InputX != 0) return AnimStates.Run;
            if (controller.TimeSinceLanded < landAnimationTime) return AnimStates.Land;
            return AnimStates.Idle;
        }
    }
}
