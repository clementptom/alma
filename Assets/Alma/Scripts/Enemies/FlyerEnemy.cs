using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Flying enemy that hovers near its home point and swoops at the player
    /// when they come close. It returns home if pulled too far away.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FlyerEnemy : Enemy
    {
        public float hoverAmplitude = 0.4f;
        public float hoverSpeed = 2f;
        public float aggroRange = 7f;
        public float leashRange = 12f;
        public float chaseSpeed = 4f;
        public float acceleration = 10f;
        public bool requiresLineOfSight = true;

        float hoverTime;

        protected override void Awake()
        {
            base.Awake();
            rb.gravityScale = 0f;
            hoverTime = Random.value * 10f;
        }

        protected override void Tick()
        {
            hoverTime += Time.fixedDeltaTime;
            Vector2 desiredVelocity;

            bool playerClose = Player != null && !Player.PlayerHealth.IsDead
                               && DistanceToPlayer() < aggroRange
                               && Vector2.Distance(Center, homePosition) < leashRange
                               && (!requiresLineOfSight || PhysicsUtil.HasLineOfSight(Center, Player.Center));

            if (playerClose)
            {
                Vector2 toPlayer = Player.Center - Center;
                desiredVelocity = toPlayer.normalized * chaseSpeed;
                Facing = toPlayer.x >= 0 ? 1 : -1;
            }
            else
            {
                Vector2 hoverTarget = homePosition + new Vector2(0f, Mathf.Sin(hoverTime * hoverSpeed) * hoverAmplitude);
                Vector2 toHome = hoverTarget - (Vector2)transform.position;
                desiredVelocity = Vector2.ClampMagnitude(toHome * 3f, chaseSpeed * 0.6f);
                if (Mathf.Abs(desiredVelocity.x) > 0.2f) Facing = desiredVelocity.x > 0 ? 1 : -1;
            }

            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
        }

        protected override void OnDamaged(DamageInfo info)
        {
            base.OnDamaged(info);
            // Knock flyers straight away from the hit, including vertically.
            if (rb != null && knockbackResistance < 1f)
            {
                Vector2 away = ((Vector2)transform.position - info.sourcePosition).normalized;
                rb.linearVelocity = away * knockbackSpeed * (1f - knockbackResistance);
            }
        }

        protected override string AnimationState() => AnimStates.Move;
    }
}
