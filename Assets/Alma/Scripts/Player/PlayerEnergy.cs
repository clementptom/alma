using System;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Energy (soul / mana) and healing. Hitting enemies fills the meter; holding HEAL while
    /// standing still spends it to restore health. Taking damage interrupts the heal.
    /// The maximum is stored in the save file and raised by EnergyUpgradePickup.
    /// </summary>
    public class PlayerEnergy : MonoBehaviour
    {
        [Header("Gaining energy")]
        [Tooltip("Energy gained each time an attack hits an enemy.")]
        public int energyPerHit = 11;

        [Header("Healing (hold HEAL)")]
        public int healCost = 33;
        public int healAmount = 1;
        [Tooltip("Seconds HEAL must be held for each heal. Keep holding to heal again.")]
        public float healTime = 0.9f;
        public bool requireGrounded = true;

        public int Current { get; private set; }
        public int Max { get; private set; } = 99;
        public bool IsHealing { get; private set; }
        public float HealProgress => healTime <= 0f ? 1f : Mathf.Clamp01(healTimer / healTime);
        public bool CanAffordHeal => Current >= healCost;

        /// <summary>(current, max)</summary>
        public event Action<int, int> Changed;

        PlayerController controller;
        PlayerHealth health;
        PlayerCombat combat;
        float healTimer;
        GameObject chargeEffect;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombat>();
        }

        void Start()
        {
            if (health != null && health.Health != null) health.Health.Damaged += OnDamaged;
            Raise();
        }

        void OnDestroy()
        {
            if (health != null && health.Health != null) health.Health.Damaged -= OnDamaged;
        }

        // ------------------------------------------------------------ Amounts

        public void SetMax(int max, bool refill)
        {
            Max = Mathf.Max(0, max);
            Current = refill ? Max : Mathf.Min(Current, Max);
            Raise();
        }

        public void SetCurrent(int value)
        {
            Current = Mathf.Clamp(value, 0, Max);
            Raise();
        }

        public void Add(int amount)
        {
            int before = Current;
            Current = Mathf.Clamp(Current + amount, 0, Max);
            if (Current != before) Raise();
        }

        public bool Spend(int amount)
        {
            if (amount < 0 || Current < amount) return false;
            Current -= amount;
            Raise();
            return true;
        }

        /// <summary>Called by PlayerCombat when an attack hits an enemy.</summary>
        public void OnEnemyHit() => Add(energyPerHit);

        void Raise()
        {
            Changed?.Invoke(Current, Max);
            GameEvents.RaisePlayerEnergyChanged(Current, Max);
        }

        // ------------------------------------------------------------ Healing

        void Update()
        {
            var input = InputReader.Instance;
            if (IsHealing)
            {
                if (input == null || !input.HealHeld || !CanContinue())
                {
                    StopHealing();
                    return;
                }
                healTimer += Time.deltaTime;
                if (healTimer >= healTime)
                {
                    healTimer = 0f;
                    if (Spend(healCost))
                    {
                        health.Health.Heal(healAmount);
                        AudioManager.PlaySfx(AudioIds.PlayerHeal);
                        EffectsManager.Spawn(EffectIds.Heal, controller.Center);
                    }
                    if (!CanContinue()) StopHealing();
                }
            }
            else if (input != null && input.HealPressed && CanStart())
            {
                StartHealing();
            }
        }

        bool CanContinue()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing) return false;
            if (controller == null || health == null || health.Health == null) return false;
            if (controller.IsFrozen || health.IsDead) return false;
            if (requireGrounded && !controller.IsGrounded) return false;
            return health.Health.Current < health.Health.Max && Current >= healCost;
        }

        bool CanStart()
        {
            return CanContinue() && !controller.IsDashing && (combat == null || !combat.IsAttacking);
        }

        void StartHealing()
        {
            IsHealing = true;
            healTimer = 0f;
            AudioManager.PlaySfx(AudioIds.PlayerHealCharge);
            chargeEffect = EffectsManager.Spawn(EffectIds.HealCharge, controller.Center, 0f, false, transform);
        }

        public void StopHealing()
        {
            if (!IsHealing) return;
            IsHealing = false;
            healTimer = 0f;
            if (chargeEffect != null) Destroy(chargeEffect);
            chargeEffect = null;
        }

        void OnDamaged(DamageInfo info) => StopHealing();
    }
}
