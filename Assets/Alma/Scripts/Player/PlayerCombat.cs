using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    public enum AttackDirection
    {
        Side,
        Up,
        Down
    }

    /// <summary>
    /// Melee attacks: forward (alternating two-hit combo), up, and down while airborne.
    /// A downward hit bounces the player up (pogo). Hitboxes are boxes relative to the player.
    ///
    /// Parry: press PARRY just before an enemy blow lands. A successful parry blocks it,
    /// staggers the attacker and opens a riposte: the next hit within a moment does extra
    /// damage. Attacks that flash red can't be parried; dodge those.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Damage")]
        public int damage = 1;

        [Header("Timing")]
        public float attackCooldown = 0.32f;
        [Tooltip("How long the attack animation plays.")]
        public float attackDuration = 0.25f;
        [Tooltip("How long the hitbox stays active after pressing attack.")]
        public float hitWindow = 0.12f;
        [Tooltip("Attack again within this time to play the second swing (Attack2).")]
        public float comboWindow = 0.5f;
        public float inputBuffer = 0.12f;

        [Header("Hitboxes (relative to the player's center, facing right)")]
        public Vector2 sideSize = new Vector2(1.8f, 1.2f);
        public Vector2 sideOffset = new Vector2(1.0f, 0.1f);
        public Vector2 upSize = new Vector2(1.4f, 1.8f);
        public Vector2 upOffset = new Vector2(0f, 1.5f);
        public Vector2 downSize = new Vector2(1.4f, 1.8f);
        public Vector2 downOffset = new Vector2(0f, -1.4f);

        [Header("Feel")]
        public float pogoSpeed = 14f;
        public float recoilSpeed = 4f;
        public float hitStop = 0.05f;
        public float hitShake = 0.08f;

        [Header("Parry")]
        [Tooltip("Seconds after pressing PARRY in which an incoming blow is parried.")]
        public float parryWindow = 0.2f;
        [Tooltip("Seconds you're stuck after a parry that caught nothing.")]
        public float parryRecovery = 0.35f;
        [Tooltip("After a successful parry, the next hit within this time is a riposte.")]
        public float riposteWindow = 1.2f;
        public int riposteDamageMultiplier = 3;

        [Header("Noise (stealth)")]
        public float attackNoiseRadius = 6f;

        [Header("Greybox")]
        [Tooltip("Shows a translucent box where the attack hits. Turn off once you have slash effects.")]
        public bool showPlaceholderSlash = false;

        public bool IsAttacking => attackTimer > 0f;
        /// <summary>Parry stance (active window or recovery): the player can't move.</summary>
        public bool IsParrying => parryTimer > 0f || parryRecoveryTimer > 0f;
        public bool ParryActive => parryTimer > 0f;
        public float LastParrySuccessTime { get; private set; } = -99f;
        public bool RiposteReady => riposteTimer > 0f;
        public AttackDirection Direction { get; private set; }
        public int ComboIndex { get; private set; }
        /// <summary>Increments on every attack so the animator can restart the clip.</summary>
        public int AttackSerial { get; private set; }

        public string AnimationState
        {
            get
            {
                switch (Direction)
                {
                    case AttackDirection.Up: return AnimStates.AttackUp;
                    case AttackDirection.Down: return AnimStates.AttackDown;
                    default: return ComboIndex % 2 == 0 ? AnimStates.Attack : AnimStates.Attack2;
                }
            }
        }

        PlayerController controller;
        float attackTimer;
        float activeTimer;
        float cooldownTimer;
        float bufferTimer;
        float timeSinceAttack = 99f;
        float parryTimer;
        float parryRecoveryTimer;
        float riposteTimer;
        bool hitSomethingThisSwing;
        readonly HashSet<object> hitThisSwing = new HashSet<object>();
        readonly List<Collider2D> hitBuffer = new List<Collider2D>();
        SpriteRenderer placeholderSlash;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (attackTimer > 0f) attackTimer -= dt;
            if (cooldownTimer > 0f) cooldownTimer -= dt;
            if (bufferTimer > 0f) bufferTimer -= dt;
            if (parryTimer > 0f) parryTimer -= dt;
            else if (parryRecoveryTimer > 0f) parryRecoveryTimer -= dt;
            if (riposteTimer > 0f) riposteTimer -= dt;
            timeSinceAttack += dt;

            var input = InputReader.Instance;
            var gm = GameManager.Instance;
            var profile = controller.Profile;
            bool canAct = input != null && (gm == null || gm.State == GameState.Playing)
                          && !controller.IsFrozen && !controller.IsDashing && !controller.IsRolling && !controller.PlayerHealth.IsDead
                          && (controller.Energy == null || !controller.Energy.IsHealing) && !IsParrying;
            if (!canAct)
            {
                bufferTimer = 0f;
                return;
            }

            if (input.ParryPressed && (profile == null || profile.CanParry) && !IsAttacking)
            {
                StartParry();
                return;
            }

            if (profile != null && !profile.CanAttack) return;
            if (input.AttackPressed) bufferTimer = inputBuffer;
            if (bufferTimer > 0f && cooldownTimer <= 0f) StartAttack();
        }

        // ---------------------------------------------------------------- Parry

        void StartParry()
        {
            bufferTimer = 0f;
            parryTimer = parryWindow;
            parryRecoveryTimer = parryRecovery;
            AudioManager.PlaySfx(AudioIds.PlayerParry);
        }

        /// <summary>
        /// Called by PlayerHealth before damage is applied. Returns true if the blow was parried.
        /// </summary>
        public bool TryParry(DamageInfo info)
        {
            if (parryTimer <= 0f || !info.parryable) return false;
            // You can only parry what's in front of you.
            float dx = info.sourcePosition.x - controller.Center.x;
            if (Mathf.Abs(dx) > 0.3f && Mathf.Sign(dx) != controller.Facing) return false;

            parryTimer = 0f;
            parryRecoveryTimer = 0f;
            riposteTimer = riposteWindow;
            cooldownTimer = 0f;
            LastParrySuccessTime = Time.time;

            Vector2 at = Vector2.Lerp(controller.Center, info.sourcePosition, 0.5f);
            AudioManager.PlaySfx(AudioIds.PlayerParrySuccess);
            EffectsManager.Spawn(EffectIds.ParrySpark, at);
            CameraController.Shake(0.2f, 0.15f);
            if (GameManager.Instance != null) GameManager.Instance.HitStop(0.12f);
            Noise.Emit(at, attackNoiseRadius, gameObject);

            // Melee attackers stagger; a parried arrow just breaks.
            var enemy = info.source != null ? info.source.GetComponentInParent<Enemy>() : null;
            if (enemy != null && Vector2.Distance(enemy.Center, controller.Center) < 3.5f) enemy.OnParried();
            return true;
        }

        void StartAttack()
        {
            bufferTimer = 0f;
            if (controller.InputY > 0) Direction = AttackDirection.Up;
            else if (controller.InputY < 0 && !controller.IsGrounded) Direction = AttackDirection.Down;
            else Direction = AttackDirection.Side;

            ComboIndex = timeSinceAttack < comboWindow ? ComboIndex + 1 : 0;
            timeSinceAttack = 0f;
            attackTimer = attackDuration;
            activeTimer = hitWindow;
            cooldownTimer = attackCooldown;
            AttackSerial++;
            hitThisSwing.Clear();
            hitSomethingThisSwing = false;

            AudioManager.PlaySfx(AudioIds.PlayerAttack);
            Noise.Emit(controller.Center, attackNoiseRadius, gameObject);
            GetHitbox(out Vector2 center, out Vector2 size);
            float angle = Direction == AttackDirection.Up ? 90f : Direction == AttackDirection.Down ? -90f : 0f;
            bool flip = controller.Facing < 0;
            if (Direction != AttackDirection.Side && flip) angle = -angle;
            string fx = Direction == AttackDirection.Side ? EffectIds.Slash : EffectIds.SlashVertical;
            EffectsManager.Spawn(fx, center, angle, flip, transform);

            ShowPlaceholder(center, size);
        }

        void FixedUpdate()
        {
            if (activeTimer <= 0f) return;
            activeTimer -= Time.fixedDeltaTime;
            CheckHits();
            if (activeTimer <= 0f && placeholderSlash != null) placeholderSlash.enabled = false;
        }

        void GetHitbox(out Vector2 center, out Vector2 size)
        {
            Vector2 offset;
            switch (Direction)
            {
                case AttackDirection.Up:
                    offset = upOffset;
                    size = upSize;
                    break;
                case AttackDirection.Down:
                    offset = downOffset;
                    size = downSize;
                    break;
                default:
                    offset = sideOffset;
                    size = sideSize;
                    break;
            }
            offset.x *= controller != null ? controller.Facing : 1;
            center = (controller != null ? controller.Center : (Vector2)transform.position) + offset;
        }

        void CheckHits()
        {
            GetHitbox(out Vector2 center, out Vector2 size);
            hitBuffer.Clear();
            hitBuffer.AddRange(PhysicsUtil.OverlapBox(center, size, Layers.PlayerAttackMask, true));

            bool hitEnemy = false;
            bool newHit = false;
            foreach (var col in hitBuffer)
            {
                if (col == null || col.transform.IsChildOf(transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null)
                {
                    // Spikes and other hazards can't be damaged, but a downward strike bounces off them.
                    var hazard = col.GetComponentInParent<ContactDamage>();
                    if (Direction == AttackDirection.Down && hazard != null && hazard.isHazard && hazard.pogoable
                        && hitThisSwing.Add(hazard))
                    {
                        newHit = true;
                        EffectsManager.Spawn(EffectIds.HitSpark, col.ClosestPoint(controller.Center));
                    }
                    continue;
                }
                if (hitThisSwing.Contains(target)) continue;
                hitThisSwing.Add(target);

                int amount = damage;
                bool riposte = riposteTimer > 0f && col.GetComponentInParent<Enemy>() != null;
                if (riposte) amount *= Mathf.Max(1, riposteDamageMultiplier);
                var info = new DamageInfo(amount, controller.Center, gameObject, DamageType.Attack);
                if (!target.TakeDamage(info)) continue;

                newHit = true;
                if (riposte)
                {
                    riposteTimer = 0f;
                    AudioManager.PlaySfx(AudioIds.PlayerRiposte);
                }
                if (col.GetComponentInParent<Enemy>() != null)
                {
                    hitEnemy = true;
                    if (controller.Energy != null) controller.Energy.OnEnemyHit();
                }
                EffectsManager.Spawn(EffectIds.HitSpark, col.ClosestPoint(controller.Center));
            }

            if (!newHit) return;

            AudioManager.PlaySfx(AudioIds.PlayerAttackHit);
            if (!hitSomethingThisSwing)
            {
                hitSomethingThisSwing = true;
                if (GameManager.Instance != null) GameManager.Instance.HitStop(hitStop);
                CameraController.Shake(hitShake, 0.1f);

                if (Direction == AttackDirection.Down) controller.Bounce(pogoSpeed);
                else if (Direction == AttackDirection.Side && hitEnemy) controller.Recoil(-controller.Facing * recoilSpeed);
            }
        }

        void ShowPlaceholder(Vector2 center, Vector2 size)
        {
            if (!showPlaceholderSlash) return;
            if (placeholderSlash == null)
            {
                var go = new GameObject("PlaceholderSlash");
                placeholderSlash = go.AddComponent<SpriteRenderer>();
                placeholderSlash.sprite = Placeholders.Square;
                placeholderSlash.color = new Color(1f, 1f, 1f, 0.35f);
                placeholderSlash.sortingOrder = 50;
            }
            var t = placeholderSlash.transform;
            t.SetParent(transform, true);
            t.position = center;
            t.rotation = Quaternion.identity;
            Vector3 parentScale = transform.lossyScale;
            t.localScale = new Vector3(size.x / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)), size.y / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)), 1f);
            placeholderSlash.enabled = true;
        }

        void OnDrawGizmosSelected()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            Vector2 c = controller != null ? (Vector2)transform.position : (Vector2)transform.position;
            var col = GetComponent<Collider2D>();
            if (col != null) c = col.bounds.center;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(c + sideOffset, sideSize);
            Gizmos.DrawWireCube(c + upOffset, upSize);
            Gizmos.DrawWireCube(c + downOffset, downSize);
        }
    }
}
