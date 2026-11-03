using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Stationary enemy (turret, plant, sentry) that fires projectiles at the player
    /// when it can see them. It telegraphs each shot by playing "Attack" first.
    /// </summary>
    public class ShooterEnemy : Enemy
    {
        public Projectile projectilePrefab;
        public Transform muzzle;
        public float range = 9f;
        public float fireInterval = 2f;
        [Tooltip("Delay between starting the Attack animation and the projectile appearing.")]
        public float telegraphTime = 0.35f;
        public float projectileSpeed = 8f;
        [Tooltip("Aim straight at the player. If false, shoots horizontally.")]
        public bool aimAtPlayer = true;

        float cooldown;
        float telegraphTimer = -1f;

        protected override void Awake()
        {
            base.Awake();
            cooldown = fireInterval * 0.5f;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        protected override void Tick()
        {
            bool sees = CanSeePlayer(range);
            if (sees) Facing = DirectionToPlayer();

            if (telegraphTimer >= 0f)
            {
                telegraphTimer -= Time.fixedDeltaTime;
                if (telegraphTimer < 0f) Fire();
                return;
            }

            cooldown -= Time.fixedDeltaTime;
            if (sees && cooldown <= 0f)
            {
                telegraphTimer = telegraphTime;
                cooldown = fireInterval;
            }
        }

        void Fire()
        {
            if (projectilePrefab == null || Player == null) return;
            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : Center + new Vector2(Facing * 0.6f, 0f);
            Vector2 dir = aimAtPlayer ? (Player.Center - origin) : new Vector2(Facing, 0f);
            var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(dir, gameObject, projectileSpeed);
            AudioManager.PlaySfx(AudioIds.EnemyShoot);
        }

        protected override string AnimationState() => telegraphTimer >= 0f ? AnimStates.Attack : AnimStates.Idle;
    }
}
