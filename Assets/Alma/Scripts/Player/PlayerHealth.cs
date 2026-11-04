using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Player-specific reactions to damage: knockback, invulnerability flicker, hit-stop,
    /// camera shake, hazard respawn and death. The numbers live on the Health component.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Seconds of invulnerability after being hit.")]
        public float invulnerabilityTime = 1.1f;
        public float hitStopDuration = 0.08f;
        public float flickerInterval = 0.08f;
        public float hitShake = 0.25f;

        public Health Health { get; private set; }
        public float LastDamageTime { get; private set; } = -99f;
        public bool IsDead => Health != null && Health.IsDead;

        PlayerController controller;
        SpriteRenderer[] renderers;
        float flickerTimer;
        bool flickerOn;

        void Awake()
        {
            Health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            Health.invulnerabilityTime = invulnerabilityTime;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            Health.Changed += OnChanged;
            Health.deathGuard = info => BarroCompanion.Instance != null && BarroCompanion.Instance.TryRescue(info);
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        void OnDestroy()
        {
            if (Health == null) return;
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
            Health.Changed -= OnChanged;
        }

        void Start()
        {
            GameEvents.RaisePlayerHealthChanged(Health.Current, Health.Max);
        }

        /// <summary>
        /// Entry point for all damage to the player (contact, hazards, enemy strikes, projectiles).
        /// Handles dodge-roll invulnerability and parries. Returns true if the hit landed.
        /// </summary>
        public bool ApplyDamage(DamageInfo info)
        {
            if (Health.IsDead) return false;
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing && gm.State != GameState.Dialogue
                && gm.State != GameState.Lockpicking) return false;

            if (info.respawnAtSafeGround)
            {
                // Hazards always send you back to safe ground, even during invulnerability.
                bool hurt = Health.TakeDamage(info);
                if (!Health.IsDead && gm != null) gm.HazardRespawn();
                return hurt;
            }

            if (controller != null)
            {
                if (controller.RollInvulnerable) return false;
                if (controller.Combat != null && controller.Combat.TryParry(info)) return false;
            }
            return Health.TakeDamage(info);
        }

        void OnDamaged(DamageInfo info)
        {
            LastDamageTime = Time.time;
            if (!info.respawnAtSafeGround && controller != null) controller.ApplyKnockback(info.sourcePosition);
            AudioManager.PlaySfx(AudioIds.PlayerHurt);
            EffectsManager.Spawn(EffectIds.PlayerHurt, controller != null ? controller.Center : (Vector2)transform.position);
            CameraController.Shake(hitShake, 0.2f);
            if (GameManager.Instance != null) GameManager.Instance.HitStop(hitStopDuration);
            DreadMeter.Spike(6f);
        }

        void OnDied()
        {
            SetRenderersVisible(true);
            AudioManager.PlaySfx(AudioIds.PlayerDeath);
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
        }

        void OnChanged(int current, int max)
        {
            GameEvents.RaisePlayerHealthChanged(current, max);
        }

        void Update()
        {
            if (Health.IsInvulnerable && !Health.GodMode && !Health.IsDead)
            {
                flickerTimer -= Time.deltaTime;
                if (flickerTimer <= 0f)
                {
                    flickerTimer = flickerInterval;
                    flickerOn = !flickerOn;
                    SetRenderersVisible(flickerOn);
                }
            }
            else if (!flickerOn)
            {
                flickerOn = true;
                SetRenderersVisible(true);
            }
        }

        void SetRenderersVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var c = r.color;
                c.a = visible ? 1f : 0.3f;
                r.color = c;
            }
        }

        /// <summary>Applies max health from the save file. Refill restores all hit points.</summary>
        public void ApplySave(int maxHealth, bool refill)
        {
            Health.SetMax(maxHealth, refill);
        }

        public void Revive()
        {
            Health.Revive();
            Health.SetInvulnerable(1f);
        }
    }
}
