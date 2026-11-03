using System;
using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Base class for bosses. A boss sleeps until its BossArena wakes it, shows its
    /// health bar while awake, enters phase 2 below a health threshold, and raises
    /// GameEvents.BossDefeated when it dies. Subclass it (see BruteBoss) for attack patterns.
    /// </summary>
    public class Boss : Enemy
    {
        [Header("Boss")]
        public string bossMusic = AudioIds.MusicBoss;
        [Tooltip("Phase 2 starts when health drops to this fraction.")]
        [Range(0f, 1f)] public float phaseTwoAt = 0.5f;
        [Tooltip("Seconds between the roar and the first attack.")]
        public float wakeDuration = 1.2f;

        public bool IsAwake { get; private set; }
        public bool IsWaking { get; private set; }
        public int Phase => Health.Fraction <= phaseTwoAt ? 2 : 1;

        public event Action Defeated;

        ContactDamage[] contactDamage;

        protected override void Awake()
        {
            base.Awake();
            knockbackResistance = 1f;
            hurtSound = AudioIds.BossHurt;
            deathSound = AudioIds.BossDeath;
            contactDamage = GetComponentsInChildren<ContactDamage>();
            SetDormant(true);
        }

        void SetDormant(bool dormant)
        {
            Health.GodMode = dormant;
            foreach (var c in contactDamage) c.enabled = !dormant;
        }

        public void WakeUp()
        {
            if (IsAwake || IsWaking || IsDead) return;
            StartCoroutine(WakeRoutine());
        }

        IEnumerator WakeRoutine()
        {
            IsWaking = true;
            AudioManager.PlaySfx(AudioIds.BossRoar);
            CameraController.Shake(0.3f, wakeDuration * 0.6f);
            yield return new WaitForSeconds(wakeDuration);
            IsWaking = false;
            IsAwake = true;
            SetDormant(false);
            GameEvents.RaiseBossStarted(this);
            OnAwake();
        }

        /// <summary>Start the attack loop here.</summary>
        protected virtual void OnAwake() { }

        protected override void FixedUpdate()
        {
            if (!IsAwake) return;
            base.FixedUpdate();
        }

        protected override void Update()
        {
            if (IsWaking)
            {
                anim.Play(AnimStates.Wake);
                return;
            }
            base.Update();
        }

        protected override void OnDied()
        {
            StopAllCoroutines();
            CameraController.Shake(0.5f, 0.8f);
            if (GameManager.Instance != null) GameManager.Instance.HitStop(0.25f);
            base.OnDied();
            GameEvents.RaiseBossDefeated(this);
            Defeated?.Invoke();
        }

        /// <summary>Waits in physics time; stops early if the boss dies.</summary>
        protected IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds && !IsDead)
            {
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        protected bool IsGroundedNow()
        {
            if (col == null) return true;
            var b = col.bounds;
            return PhysicsUtil.BoxCast(new Vector2(b.center.x, b.min.y + 0.1f), new Vector2(b.size.x * 0.8f, 0.05f),
                Vector2.down, 0.2f, Layers.GroundMask, null, 0.6f).collider != null;
        }

        protected bool WallAhead(int dir)
        {
            if (col == null) return false;
            var b = col.bounds;
            return PhysicsUtil.BoxCast(b.center, new Vector2(0.05f, b.size.y * 0.6f), new Vector2(dir, 0f),
                b.extents.x + 0.15f, Layers.SolidMask, null, 0.9f).collider != null;
        }
    }
}
