using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Ground enemy that patrols back and forth, turning at walls and ledges.
    /// Optionally chases the player when it can see them (and stops at ledges).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class WalkerEnemy : Enemy
    {
        [Header("Patrol")]
        public float patrolSpeed = 2.2f;
        [Tooltip("Turns around after walking this far from its start point. 0 = only turn at walls/ledges.")]
        public float patrolRange = 0f;

        [Header("Chase")]
        public bool chasesPlayer = true;
        public float chaseSpeed = 4.2f;
        public float sightRange = 6f;
        [Tooltip("Stops chasing once the player is this far away.")]
        public float loseSightRange = 9f;
        [Tooltip("Played when it spots the player.")]
        public string alertSound = AudioIds.EnemyAlert;

        bool chasing;

        protected override void Tick()
        {
            bool sees = chasesPlayer && CanSeePlayer(chasing ? loseSightRange : sightRange);
            if (sees && !chasing) AudioManager.PlaySfx(alertSound);
            chasing = sees;

            float speed;
            if (chasing)
            {
                Facing = DirectionToPlayer();
                speed = LedgeOrWallAhead() ? 0f : chaseSpeed;
                if (Player != null && Mathf.Abs(Player.Center.x - Center.x) < 0.2f) speed = 0f;
            }
            else
            {
                if (LedgeOrWallAhead() || (patrolRange > 0f && Mathf.Abs(transform.position.x - homePosition.x) > patrolRange
                                           && Mathf.Sign(transform.position.x - homePosition.x) == Facing))
                {
                    Facing = -Facing;
                }
                speed = patrolSpeed;
            }

            rb.linearVelocity = new Vector2(Facing * speed, rb.linearVelocity.y);
        }

        bool LedgeOrWallAhead()
        {
            var b = col.bounds;
            Vector2 front = new Vector2(Facing > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.min.y + 0.1f);
            bool groundAhead = PhysicsUtil.Raycast(front, Vector2.down, 0.6f, Layers.GroundMask).collider != null;
            bool wallAhead = PhysicsUtil.Raycast(b.center, new Vector2(Facing, 0f), b.extents.x + 0.1f, Layers.SolidMask).collider != null;
            return !groundAhead || wallAhead;
        }

        protected override string AnimationState()
        {
            return Mathf.Abs(rb.linearVelocity.x) > 0.1f ? AnimStates.Move : AnimStates.Idle;
        }
    }
}
