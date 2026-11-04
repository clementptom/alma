using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A human patrol (garrison soldier, brigand, sailor). Walks its patrol route, turns to
    /// investigate noises, chases once it spots the player, searches when it loses them.
    /// In combat it uses telegraphed, parryable sword blows. If the player can't fight
    /// (young Sisnando), it catches them instead and sends them back to the last checkpoint.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(StealthPerception))]
    public class GuardEnemy : Enemy
    {
        [Header("Patrol")]
        [Tooltip("Points relative to the start position. One point = stand guard there.")]
        public Vector2[] patrolPoints = { Vector2.zero, new Vector2(6f, 0f) };
        public float patrolSpeed = 1.6f;
        public float waitAtPoint = 1.5f;

        [Header("Chase")]
        public float investigateSpeed = 2.4f;
        public float chaseSpeed = 4.3f;

        [Header("Attack")]
        public float attackRange = 1.5f;
        public int attackDamage = 1;
        public float windup = 0.45f;
        public float activeTime = 0.15f;
        public float recovery = 0.6f;
        public Vector2 attackOffset = new Vector2(0.9f, 0f);
        public Vector2 attackSize = new Vector2(1.5f, 1.3f);
        [Tooltip("Every Nth attack can't be parried (0 = never).")]
        public int unparryableEvery = 0;

        [Header("Catching")]
        [Tooltip("Catch the player instead of fighting when they can't fight back.")]
        public bool catchDefenselessPlayer = true;
        public string caughtMessage = "Caught!";

        public StealthPerception Perception { get; private set; }

        int patrolIndex;
        float waitTimer;
        float lookTimer;
        bool attacking;
        int attackCount;
        Coroutine attackRoutine;

        protected override void Awake()
        {
            base.Awake();
            Perception = GetComponent<StealthPerception>();
            Perception.facing = () => Facing;
            Health.Damaged += _ => { if (Player != null) Perception.Alert(Player.Center); };
        }

        protected override void Tick()
        {
            if (attacking) return;
            switch (Perception.State)
            {
                case Awareness.Unaware: Patrol(); break;
                case Awareness.Suspicious: GoLook(investigateSpeed, true); break;
                case Awareness.Alert: Chase(); break;
                case Awareness.Searching: GoLook(investigateSpeed, true); break;
            }
        }

        void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                SetVelocity(0f);
                return;
            }
            Vector2 target = homePosition + patrolPoints[patrolIndex % patrolPoints.Length];
            float dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.15f || patrolPoints.Length == 1)
            {
                SetVelocity(0f);
                if (patrolPoints.Length == 1)
                {
                    // Standing guard: glance both ways now and then.
                    LookAround(3.5f);
                    return;
                }
                waitTimer += Time.fixedDeltaTime;
                if (waitTimer >= waitAtPoint)
                {
                    waitTimer = 0f;
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                }
                return;
            }
            Facing = dx > 0f ? 1 : -1;
            SetVelocity(BlockedAhead() ? 0f : patrolSpeed);
        }

        void GoLook(float speed, bool lookAround)
        {
            float dx = Perception.Target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.6f || BlockedAhead())
            {
                SetVelocity(0f);
                if (lookAround) LookAround(1.2f);
                return;
            }
            Facing = dx > 0f ? 1 : -1;
            SetVelocity(speed);
        }

        void Chase()
        {
            var player = Player;
            if (player == null)
            {
                SetVelocity(0f);
                return;
            }
            float dx = player.Center.x - Center.x;
            Facing = dx > 0f ? 1 : -1;
            float distance = Vector2.Distance(player.Center, Center);

            if (distance <= attackRange && Mathf.Abs(player.Center.y - Center.y) < 1.5f)
            {
                SetVelocity(0f);
                bool defenseless = player.Profile != null && !player.Profile.CanAttack;
                if (defenseless && catchDefenselessPlayer)
                {
                    Catch();
                    return;
                }
                attackRoutine = StartCoroutine(Attack());
                return;
            }
            SetVelocity(BlockedAhead() ? 0f : chaseSpeed);
        }

        IEnumerator Attack()
        {
            attacking = true;
            attackCount++;
            bool parryable = unparryableEvery <= 0 || attackCount % unparryableEvery != 0;
            yield return Strike(attackOffset, attackSize, attackDamage, windup, activeTime, recovery, parryable);
            attacking = false;
            attackRoutine = null;
        }

        void Catch()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            AudioManager.PlaySfx(AudioIds.Caught);
            gm.RestartFromCheckpoint(caughtMessage);
            Perception.ResetAwareness();
        }

        public override void OnParried()
        {
            base.OnParried();
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            attacking = false;
        }

        void LookAround(float interval)
        {
            lookTimer += Time.fixedDeltaTime;
            if (lookTimer >= interval)
            {
                lookTimer = 0f;
                Facing = -Facing;
            }
        }

        void SetVelocity(float speed)
        {
            if (rb == null) return;
            rb.linearVelocity = new Vector2(Facing * speed, rb.linearVelocity.y);
        }

        bool BlockedAhead()
        {
            if (col == null) return false;
            var b = col.bounds;
            Vector2 front = new Vector2(Facing > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.min.y + 0.1f);
            bool groundAhead = PhysicsUtil.Raycast(front, Vector2.down, 0.6f, Layers.GroundMask).collider != null;
            bool wallAhead = PhysicsUtil.Raycast(b.center, new Vector2(Facing, 0f), b.extents.x + 0.1f, Layers.SolidMask).collider != null;
            return !groundAhead || wallAhead;
        }

        protected override string AnimationState()
        {
            bool moving = rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            switch (Perception.State)
            {
                case Awareness.Alert: return moving ? AnimStates.Run : AnimStates.Idle;
                case Awareness.Searching:
                case Awareness.Suspicious: return moving ? AnimStates.Move : AnimStates.Search;
                default: return moving ? AnimStates.Move : AnimStates.Idle;
            }
        }
    }
}
