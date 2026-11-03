using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Hurts the player when they touch this object's collider. Used by enemies,
    /// bosses and hazards (spikes, thorns, lava, bottomless pits).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ContactDamage : MonoBehaviour
    {
        public int damage = 1;
        [Tooltip("Hazard mode: after the hit the player is put back on the last safe ground (spikes, pits).")]
        public bool isHazard = false;
        [Tooltip("Hazards: can the player bounce off this with a downward attack (pogo)?")]
        public bool pogoable = true;

        Collider2D[] colliders;

        void Awake()
        {
            colliders = GetComponents<Collider2D>();
        }

        void FixedUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null || player.PlayerHealth == null || player.PlayerHealth.IsDead) return;

            foreach (var col in colliders)
            {
                if (col == null || !col.enabled) continue;
                var hits = PhysicsUtil.OverlapCollider(col, Layers.PlayerMask, true);
                foreach (var hit in hits)
                {
                    if (hit.attachedRigidbody == null || hit.attachedRigidbody.gameObject != player.gameObject) continue;
                    var info = new DamageInfo(damage, col.bounds.center, gameObject,
                        isHazard ? DamageType.Hazard : DamageType.Contact)
                    {
                        respawnAtSafeGround = isHazard
                    };
                    player.PlayerHealth.ApplyDamage(info);
                    return;
                }
            }
        }
    }
}
