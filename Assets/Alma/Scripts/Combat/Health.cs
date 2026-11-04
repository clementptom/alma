using System;
using UnityEngine;

namespace Alma
{
    public enum DamageType
    {
        Attack,
        Contact,
        Projectile,
        Hazard
    }

    public struct DamageInfo
    {
        public int amount;
        public Vector2 sourcePosition;
        public GameObject source;
        public DamageType type;
        /// <summary>Hazard damage also teleports the player back to safe ground.</summary>
        public bool respawnAtSafeGround;
        /// <summary>A blow the player can parry (most melee attacks). Red-flash attacks are not.</summary>
        public bool parryable;

        public DamageInfo(int amount, Vector2 sourcePosition, GameObject source, DamageType type)
        {
            this.amount = amount;
            this.sourcePosition = sourcePosition;
            this.source = source;
            this.type = type;
            respawnAtSafeGround = false;
            parryable = false;
        }
    }

    /// <summary>Anything the player's attack can hit: enemies, breakable walls, levers, spikes (for pogo).</summary>
    public interface IDamageable
    {
        /// <returns>True if the hit registered (used for pogo and hit effects).</returns>
        bool TakeDamage(DamageInfo info);
    }

    /// <summary>Generic hit points for the player, enemies and bosses.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public int maxHealth = 3;
        [Tooltip("Seconds of invulnerability after taking a hit.")]
        public float invulnerabilityTime = 0f;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead { get; private set; }
        public bool GodMode { get; set; }
        public bool IsInvulnerable => GodMode || invulnerableTimer > 0f;
        public float Fraction => maxHealth <= 0 ? 0f : (float)Current / maxHealth;

        /// <summary>(current, max)</summary>
        public event Action<int, int> Changed;
        public event Action<DamageInfo> Damaged;
        public event Action Died;

        /// <summary>
        /// Called when a hit would be fatal. Return true to survive on 1 hit point instead
        /// (Barro dragging Corvin out of a fight).
        /// </summary>
        public Func<DamageInfo, bool> deathGuard;

        float invulnerableTimer;
        bool initialized;

        void Awake()
        {
            if (!initialized)
            {
                Current = maxHealth;
                initialized = true;
            }
        }

        void Update()
        {
            if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (!enabled || IsDead || IsInvulnerable || info.amount <= 0) return false;

            if (Current - info.amount <= 0 && deathGuard != null && deathGuard(info))
            {
                Current = 1;
                invulnerableTimer = invulnerabilityTime;
                Changed?.Invoke(Current, maxHealth);
                Damaged?.Invoke(info);
                return true;
            }

            Current = Mathf.Max(0, Current - info.amount);
            invulnerableTimer = invulnerabilityTime;
            Changed?.Invoke(Current, maxHealth);
            Damaged?.Invoke(info);

            if (Current == 0)
            {
                IsDead = true;
                Died?.Invoke();
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void HealFull() => Heal(maxHealth);

        public void SetMax(int newMax, bool refill)
        {
            maxHealth = Mathf.Max(1, newMax);
            initialized = true;
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>Restores a dead character to full health.</summary>
        public void Revive()
        {
            IsDead = false;
            invulnerableTimer = 0f;
            Current = maxHealth;
            initialized = true;
            Changed?.Invoke(Current, maxHealth);
        }

        public void SetInvulnerable(float seconds)
        {
            invulnerableTimer = Mathf.Max(invulnerableTimer, seconds);
        }

        public void Kill()
        {
            if (IsDead) return;
            Current = 0;
            IsDead = true;
            Changed?.Invoke(Current, maxHealth);
            Died?.Invoke();
        }
    }
}
