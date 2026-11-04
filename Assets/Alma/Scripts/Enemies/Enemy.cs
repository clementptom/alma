using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Base class for every enemy: hit flash, knockback, stun, death, currency drops and
    /// animation. Subclasses put their behavior in Tick() (called from FixedUpdate while
    /// alive and not stunned) and return their animation in AnimationState().
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy")]
        public string displayName = "Enemy";
        [Tooltip("Child holding the sprites/animator. It is mirrored to face left/right.")]
        public Transform visual;
        public Animator animator;
        [Tooltip("Set true if your art faces left by default.")]
        public bool artFacesLeft = false;

        [Header("Getting Hit")]
        public float knockbackSpeed = 6f;
        [Range(0f, 1f)] public float knockbackResistance = 0f;
        public float hurtStunTime = 0.15f;
        public Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        public float hitFlashTime = 0.08f;

        [Header("Parry")]
        [Tooltip("Seconds this enemy is staggered after the player parries it.")]
        public float parryStaggerTime = 1.2f;
        public Color unparryableFlashColor = new Color(1f, 0.15f, 0.1f, 1f);
        public Color windupFlashColor = new Color(1f, 1f, 0.75f, 1f);

        [Header("Death")]
        public Vector2Int currencyDrop = new Vector2Int(1, 3);
        public CurrencyPickup currencyPrefab;
        public float destroyDelay = 0.6f;

        [Header("Audio / FX")]
        public string hurtSound = AudioIds.EnemyHurt;
        public string deathSound = AudioIds.EnemyDeath;
        public string deathEffect = EffectIds.EnemyDeath;

        public Health Health { get; private set; }
        public bool IsDead => Health != null && Health.IsDead;
        public int Facing { get; protected set; } = 1;

        protected Rigidbody2D rb;
        protected Collider2D col;
        protected CharacterAnimator anim;
        protected float stunTimer;
        protected bool staggered;
        /// <summary>Animation forced by the current action (attacks); null uses AnimationState().</summary>
        protected string actionAnimation;
        protected bool strikeCancelled;
        readonly List<Collider2D> strikeHits = new List<Collider2D>();
        protected Vector2 homePosition;

        SpriteRenderer[] renderers;
        Color[] baseColors;
        Coroutine flashRoutine;

        protected PlayerController Player => PlayerController.Instance;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (visual == null) visual = transform.Find("Visual");
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
            homePosition = transform.position;

            int enemyLayer = Layers.Index(Layers.Enemy);
            if (enemyLayer >= 0) gameObject.layer = enemyLayer;
            if (rb != null) rb.freezeRotation = true;

            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        protected virtual void OnDestroy()
        {
            if (Health == null) return;
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead) return;
            if (stunTimer > 0f)
            {
                stunTimer -= Time.fixedDeltaTime;
                if (staggered && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                    rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, 30f * Time.fixedDeltaTime), rb.linearVelocity.y);
                if (stunTimer <= 0f) staggered = false;
                return;
            }
            Tick();
        }

        protected virtual void Update()
        {
            if (IsDead) anim.Play(AnimStates.Death);
            else if (stunTimer > 0f) anim.Play(staggered ? AnimStates.Stunned : AnimStates.Hurt);
            else anim.Play(!string.IsNullOrEmpty(actionAnimation) ? actionAnimation : AnimationState());
        }

        /// <summary>
        /// The player parried this enemy's blow: cancel the attack and stagger, opening a riposte.
        /// </summary>
        public virtual void OnParried()
        {
            if (IsDead) return;
            strikeCancelled = true;
            actionAnimation = null;
            staggered = true;
            stunTimer = Mathf.Max(stunTimer, parryStaggerTime);
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                rb.linearVelocity = new Vector2(-Facing * 3f, rb.linearVelocity.y);
            anim.Play(AnimStates.Stunned, true);
        }

        // ------------------------------------------------------------- Strikes

        /// <summary>
        /// A telegraphed melee attack: wind-up (the enemy flashes; red means it can't be
        /// parried), a short active window where the hitbox hurts, then recovery.
        /// The hitbox is relative to the enemy's center, facing right. Use from a coroutine:
        /// yield return Strike(...). Stops early if the enemy is parried, stunned or killed.
        /// </summary>
        protected IEnumerator Strike(Vector2 offset, Vector2 size, int damage, float windup, float active,
            float recovery, bool parryable, float lungeSpeed = 0f,
            string windupAnim = AnimStates.Telegraph, string strikeAnim = AnimStates.Attack)
        {
            strikeCancelled = false;
            actionAnimation = windupAnim;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Flash(parryable ? windupFlashColor : unparryableFlashColor, Mathf.Min(0.18f, windup));
            AudioManager.PlaySfx(parryable ? AudioIds.EnemyWindup : AudioIds.EnemyUnblockable);

            float t = 0f;
            while (t < windup)
            {
                if (StrikeInterrupted()) yield break;
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            actionAnimation = strikeAnim;
            AudioManager.PlaySfx(AudioIds.EnemySwing);
            bool hitPlayer = false;
            bool hitCompanion = false;
            t = 0f;
            while (t < active)
            {
                if (StrikeInterrupted()) yield break;
                if (lungeSpeed > 0f && rb != null) rb.linearVelocity = new Vector2(Facing * lungeSpeed, rb.linearVelocity.y);

                Vector2 center = Center + new Vector2(offset.x * Facing, offset.y);
                strikeHits.Clear();
                strikeHits.AddRange(PhysicsUtil.OverlapBox(center, size, Layers.PlayerMask | Layers.CompanionMask, true));
                foreach (var hit in strikeHits)
                {
                    if (hit == null) continue;
                    var info = new DamageInfo(damage, Center, gameObject, DamageType.Attack) { parryable = parryable };
                    ModifyStrike(ref info);
                    var playerHealth = hit.GetComponentInParent<PlayerHealth>();
                    if (playerHealth != null && !hitPlayer)
                    {
                        hitPlayer = true;
                        playerHealth.ApplyDamage(info);
                        if (strikeCancelled) yield break; // parried
                        continue;
                    }
                    var barro = hit.GetComponentInParent<BarroCompanion>();
                    if (barro != null && !hitCompanion)
                    {
                        hitCompanion = true;
                        barro.TakeHit(info);
                    }
                }
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            if (lungeSpeed > 0f && rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            actionAnimation = AnimStates.Idle;
            t = 0f;
            while (t < recovery)
            {
                if (StrikeInterrupted()) yield break;
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            actionAnimation = null;
        }

        /// <summary>Lets an enemy change its blows (e.g. the Drowned pull you under).</summary>
        protected virtual void ModifyStrike(ref DamageInfo info) { }

        bool StrikeInterrupted()
        {
            if (!strikeCancelled && !IsDead && stunTimer <= 0f) return false;
            actionAnimation = null;
            return true;
        }

        protected virtual void LateUpdate()
        {
            if (visual == null) return;
            Vector3 s = visual.localScale;
            int dir = artFacesLeft ? -Facing : Facing;
            s.x = Mathf.Abs(s.x) * dir;
            visual.localScale = s;
        }

        /// <summary>Behavior goes here. Called from FixedUpdate while alive and not stunned.</summary>
        protected virtual void Tick() { }

        protected virtual string AnimationState() => AnimStates.Idle;

        // ------------------------------------------------------------- Damage

        protected virtual void OnDamaged(DamageInfo info)
        {
            AudioManager.PlaySfx(hurtSound);
            Flash();

            float resist = Mathf.Clamp01(knockbackResistance);
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && resist < 1f)
            {
                float dir = Mathf.Sign(transform.position.x - info.sourcePosition.x);
                rb.linearVelocity = new Vector2(dir * knockbackSpeed * (1f - resist), Mathf.Max(rb.linearVelocity.y, 2f * (1f - resist)));
            }
            if (resist < 1f) stunTimer = hurtStunTime;
        }

        protected virtual void OnDied()
        {
            AudioManager.PlaySfx(deathSound);
            EffectsManager.Spawn(deathEffect, col != null ? (Vector2)col.bounds.center : (Vector2)transform.position);
            DropCurrency();

            foreach (var damage in GetComponentsInChildren<ContactDamage>()) damage.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            Destroy(gameObject, destroyDelay);
        }

        void DropCurrency()
        {
            int amount = Random.Range(currencyDrop.x, currencyDrop.y + 1);
            if (amount <= 0) return;
            Vector2 at = col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
            if (currencyPrefab == null)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCurrency(amount);
                return;
            }
            for (int i = 0; i < amount; i++)
            {
                var coin = Instantiate(currencyPrefab, at, Quaternion.identity);
                coin.Scatter();
            }
        }

        void Flash() => Flash(hitFlashColor, hitFlashTime);

        protected void Flash(Color color, float duration)
        {
            if (renderers.Length == 0) return;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].color = baseColors[i];
            }
            flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        IEnumerator FlashRoutine(Color color, float duration)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = color;
            yield return new WaitForSeconds(duration);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = baseColors[i];
            flashRoutine = null;
        }

        // ------------------------------------------------------------ Helpers

        protected float DistanceToPlayer()
        {
            return Player == null ? float.MaxValue : Vector2.Distance(Center, Player.Center);
        }

        protected int DirectionToPlayer()
        {
            if (Player == null) return Facing;
            return Player.Center.x >= Center.x ? 1 : -1;
        }

        protected bool CanSeePlayer(float range)
        {
            var player = Player;
            if (player == null || player.PlayerHealth.IsDead) return false;
            if (Vector2.Distance(Center, player.Center) > range) return false;
            return PhysicsUtil.HasLineOfSight(Center, player.Center);
        }

        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
    }
}
