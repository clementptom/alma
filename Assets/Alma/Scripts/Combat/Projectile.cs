using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A simple straight-line projectile. It moves itself (no physics body needed),
    /// damages whatever is on its target layers, and is destroyed by solid walls.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float speed = 9f;
        public int damage = 1;
        public float lifetime = 4f;
        public float radius = 0.25f;
        [Tooltip("True for enemy projectiles (hurts the player). False makes it hurt enemies.")]
        public bool hurtsPlayer = true;
        public bool destroyedByWalls = true;
        [Tooltip("Rotate the sprite to face its direction of travel.")]
        public bool rotateToDirection = true;
        public string hitEffect = EffectIds.ProjectileHit;
        public string hitSound = AudioIds.ProjectileHit;

        Vector2 direction = Vector2.right;
        float age;
        GameObject owner;

        public void Launch(Vector2 dir, GameObject launchedBy = null, float? overrideSpeed = null)
        {
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            owner = launchedBy;
            if (overrideSpeed.HasValue) speed = overrideSpeed.Value;
            if (rotateToDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 position = transform.position;
            Vector2 step = direction * speed * Time.fixedDeltaTime;

            if (destroyedByWalls)
            {
                var wall = PhysicsUtil.Raycast(position, direction, step.magnitude + radius, Layers.SolidMask);
                if (wall.collider != null)
                {
                    Impact(wall.point);
                    return;
                }
            }

            position += step;
            transform.position = position;

            int mask = hurtsPlayer ? Layers.PlayerMask : Layers.EnemyMask;
            var hits = PhysicsUtil.OverlapCircle(position, radius, mask);
            foreach (var hit in hits)
            {
                if (owner != null && hit.transform.IsChildOf(owner.transform)) continue;
                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null) continue;
                target.TakeDamage(new DamageInfo(damage, position, gameObject, DamageType.Projectile));
                Impact(position);
                return;
            }
        }

        void Impact(Vector2 point)
        {
            EffectsManager.Spawn(hitEffect, point);
            AudioManager.PlaySfx(hitSound);
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
