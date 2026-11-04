using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Barro, the old priest's mountain dog. Follows the player and obeys four commands:
    ///   TRACK  - follows the nearest scent: hidden doors, buried people, items (BarroPointOfInterest)
    ///   GUARD  - stays put and silent (press again to call him back)
    ///   FETCH  - brings back something out of reach (BarroFetchable)
    ///   ATTACK - goes for the nearest enemy
    /// He growls at hidden threats, drags the player out once per fight instead of letting
    /// them die, and can be knocked down for a while (which spikes Dread).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class BarroCompanion : MonoBehaviour
    {
        public static BarroCompanion Instance { get; private set; }

        public enum Mode
        {
            Follow,
            Guard,
            Tracking,
            Fetching,
            Attacking,
            Downed,
            Scripted
        }

        [Header("Following")]
        public float followDistance = 1.8f;
        public float runSpeed = 7f;
        public float sprintSpeed = 11f;
        public float jumpVelocity = 15f;
        [Tooltip("Catches up instantly (out of sight) when left this far behind.")]
        public float teleportDistance = 15f;

        [Header("Commands")]
        public float attackRange = 10f;
        public int biteDamage = 1;
        public float biteCooldown = 0.8f;
        public float attackDuration = 5f;
        public float barkNoiseRadius = 9f;

        [Header("Senses")]
        public float growlRange = 7f;
        public float growlInterval = 1.5f;

        [Header("Rescue")]
        [Tooltip("Barro must be this close to drag the player out of a fatal hit.")]
        public float rescueRange = 12f;
        [Tooltip("The rescue comes back after this many seconds without the player getting hurt.")]
        public float rescueResetTime = 25f;

        [Header("Toughness")]
        public float downedTime = 8f;
        public float regenDelay = 6f;

        [Header("References")]
        public Transform visual;
        public Animator animator;
        [Tooltip("The silver collar (shown once Corvin puts Sisnando's collar on him).")]
        public GameObject collar;
        [Tooltip("Where carried objects sit.")]
        public Transform mouth;

        public Mode Current { get; private set; } = Mode.Follow;
        public bool RescueAvailable { get; private set; } = true;
        public bool IsDowned => Current == Mode.Downed;
        public Health Health { get; private set; }
        public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

        Rigidbody2D rb;
        Collider2D col;
        CharacterAnimator anim;
        int facing = 1;
        bool grounded;

        BarroPointOfInterest trackTarget;
        BarroFetchable fetchTarget;
        Enemy attackTarget;
        float modeTimer;
        float biteTimer;
        float growlTimer;
        float stuckTimer;
        float idleTimer;
        float lastHurtTime = -99f;
        float regenTimer;
        string oneShotAnim;
        float oneShotTimer;
        Vector2 scriptedTarget;
        bool hasScriptedTarget;

        // ================================================================= Setup

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            Health = GetComponent<Health>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (rb.gravityScale < 0.1f) rb.gravityScale = 3.5f;
            if (visual == null) visual = transform.Find("Visual");
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            int layer = Layers.Index(Layers.Companion);
            if (layer >= 0) gameObject.layer = layer;

            Health.Damaged += OnHurt;
            Health.Died += OnKnockedDown;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Health == null) return;
            Health.Damaged -= OnHurt;
            Health.Died -= OnKnockedDown;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        /// <summary>Puts Barro next to the player (after a scene load or when he's left behind).</summary>
        public void PlaceNear(PlayerController player, bool withEffect = false)
        {
            if (player == null) return;
            Vector2 at = (Vector2)player.transform.position + new Vector2(-player.Facing * 1.2f, 0.1f);
            // Stay out of walls: if the spot behind is blocked, use the player's position.
            if (PhysicsUtil.Raycast(player.Center, new Vector2(-player.Facing, 0f), 1.4f, Layers.SolidMask).collider != null)
                at = (Vector2)player.transform.position + new Vector2(0f, 0.1f);
            rb.position = at;
            transform.position = at;
            rb.linearVelocity = Vector2.zero;
            facing = player.Facing;
            stuckTimer = 0f;
            if (withEffect) EffectsManager.Spawn(EffectIds.BarroPuff, at);
        }

        /// <summary>Called on scene load and at save stations: a fresh fight, a fresh rescue.</summary>
        public void ResetForNewArea()
        {
            RescueAvailable = true;
            if (Current != Mode.Scripted) SetMode(Mode.Follow);
            if (Health.IsDead) Health.Revive();
            ReleaseCarried(false);
        }

        public void SetCollarVisible(bool visible)
        {
            if (collar != null) collar.SetActive(visible);
        }

        // ================================================================ Update

        void Update()
        {
            if (oneShotTimer > 0f) oneShotTimer -= Time.deltaTime;
            ReadCommands();
            UpdateRescue();
            UpdateGrowl();
            UpdateRegen();
            anim.Play(ChooseAnimation());
        }

        void ReadCommands()
        {
            var input = InputReader.Instance;
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            if (input == null || player == null || player.IsFrozen) return;
            if (gm != null && gm.State != GameState.Playing) return;
            if (Current == Mode.Downed || Current == Mode.Scripted) return;

            if (input.BarroGuardPressed) CommandGuard();
            else if (input.BarroTrackPressed) CommandTrack();
            else if (input.BarroFetchPressed) CommandFetch();
            else if (input.BarroAttackPressed) CommandAttack();
        }

        public void CommandGuard()
        {
            if (Current == Mode.Guard)
            {
                SetMode(Mode.Follow);
                Bark(false);
                GameEvents.RaiseToast("Barro: heel");
                return;
            }
            SetMode(Mode.Guard);
            GameEvents.RaiseToast("Barro: stay (quiet)");
        }

        public void CommandTrack()
        {
            var player = PlayerController.Instance;
            var target = BarroPointOfInterest.Nearest(player != null ? player.Center : Position);
            if (target == null)
            {
                // Nothing to find: a confused sniff.
                PlayOnce(AnimStates.Sniff, 1f);
                AudioManager.PlaySfx(AudioIds.BarroSniff);
                GameEvents.RaiseToast("Barro finds no trail.");
                SetMode(Mode.Follow);
                return;
            }
            if (target.refuseToTrack)
            {
                // Story beat: he won't follow this trail.
                SetMode(Mode.Follow);
                PlayOnce(AnimStates.Howl, 2.5f);
                AudioManager.PlaySfx(AudioIds.BarroHowl);
                if (!string.IsNullOrEmpty(target.foundMessage)) GameEvents.RaiseToast(target.foundMessage);
                target.onFound.Invoke();
                return;
            }
            trackTarget = target;
            oneShotAnim = null;
            SetMode(Mode.Tracking);
            AudioManager.PlaySfx(AudioIds.BarroSniff);
        }

        public void CommandFetch()
        {
            var player = PlayerController.Instance;
            var target = BarroFetchable.Nearest(player != null ? player.Center : Position);
            if (target == null)
            {
                PlayOnce(AnimStates.Sniff, 0.8f);
                GameEvents.RaiseToast("Nothing for Barro to fetch.");
                return;
            }
            fetchTarget = target;
            SetMode(Mode.Fetching);
        }

        public void CommandAttack()
        {
            var player = PlayerController.Instance;
            Vector2 from = player != null ? player.Center : Position;
            Enemy best = null;
            float bestDistance = attackRange;
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (e == null || e.IsDead) continue;
                float d = Vector2.Distance(from, e.Center);
                if (d < bestDistance)
                {
                    best = e;
                    bestDistance = d;
                }
            }
            if (best == null)
            {
                GameEvents.RaiseToast("No one for Barro to attack.");
                return;
            }
            attackTarget = best;
            SetMode(Mode.Attacking);
            Bark(true);
        }

        void SetMode(Mode mode)
        {
            if (Current == Mode.Fetching && mode != Mode.Fetching) ReleaseCarried(false);
            Current = mode;
            modeTimer = 0f;
            stuckTimer = 0f;
        }

        // =========================================================== FixedUpdate

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            modeTimer += dt;
            grounded = CheckGrounded();
            var player = PlayerController.Instance;

            if (Current == Mode.Downed)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (modeTimer >= downedTime) GetUp();
                return;
            }
            if (player == null)
            {
                Stop();
                return;
            }

            if (Current != Mode.Guard && Current != Mode.Scripted)
            {
                float distance = Vector2.Distance(Position, player.Center);
                if (distance > teleportDistance || (stuckTimer > 2.5f && distance > 4f))
                {
                    PlaceNear(player, true);
                    if (Current != Mode.Follow && Current != Mode.Fetching) SetMode(Mode.Follow);
                }
            }

            switch (Current)
            {
                case Mode.Follow: FollowStep(player, dt); break;
                case Mode.Guard: Stop(); break;
                case Mode.Tracking: TrackStep(player); break;
                case Mode.Fetching: FetchStep(player); break;
                case Mode.Attacking: AttackStep(dt); break;
                case Mode.Scripted:
                    if (hasScriptedTarget) MoveToward(scriptedTarget, runSpeed, 0.2f);
                    else Stop();
                    break;
            }
        }

        void FollowStep(PlayerController player, float dt)
        {
            Vector2 target = (Vector2)player.transform.position + new Vector2(-player.Facing * followDistance, 0f);
            float distance = Vector2.Distance(Position, target);
            float speed = distance > 6f ? sprintSpeed : runSpeed;
            MoveToward(target, speed, 0.5f);
            idleTimer = Mathf.Abs(rb.linearVelocity.x) < 0.2f ? idleTimer + dt : 0f;
            if (Mathf.Abs(rb.linearVelocity.x) < 0.2f && distance < 1.5f) facing = player.Facing;
        }

        void TrackStep(PlayerController player)
        {
            if (trackTarget == null || !trackTarget.Available)
            {
                SetMode(Mode.Follow);
                return;
            }
            Vector2 target = trackTarget.transform.position;
            if (Vector2.Distance(Position, target) > 0.9f && modeTimer < 12f)
            {
                MoveToward(target, sprintSpeed, 0.3f);
                return;
            }
            Stop();
            if (oneShotTimer <= 0f && oneShotAnim != AnimStates.Sniff)
            {
                PlayOnce(AnimStates.Sniff, 1f);
                return;
            }
            if (oneShotTimer > 0f) return;
            Bark(true);
            trackTarget.MarkFound();
            trackTarget = null;
            SetMode(Mode.Follow);
        }

        void FetchStep(PlayerController player)
        {
            if (fetchTarget == null)
            {
                SetMode(Mode.Follow);
                return;
            }
            if (!fetchTarget.Carried)
            {
                Vector2 target = fetchTarget.transform.position;
                if (Vector2.Distance(Position, target) > 0.9f && modeTimer < 12f)
                {
                    MoveToward(target, sprintSpeed, 0.3f);
                    return;
                }
                PickUp(fetchTarget);
                return;
            }
            // Carrying: bring it to the player.
            Vector2 home = player.transform.position;
            if (Vector2.Distance(Position, home) > 1.2f && modeTimer < 20f)
            {
                MoveToward(home, sprintSpeed, 0.6f);
                return;
            }
            ReleaseCarried(true);
            SetMode(Mode.Follow);
        }

        void AttackStep(float dt)
        {
            if (attackTarget == null || attackTarget.IsDead || modeTimer > attackDuration)
            {
                attackTarget = null;
                SetMode(Mode.Follow);
                return;
            }
            Vector2 target = attackTarget.Center;
            if (Vector2.Distance(Position, target) > 1.3f)
            {
                MoveToward(target, sprintSpeed, 0.4f);
                return;
            }
            Stop();
            facing = target.x >= Position.x ? 1 : -1;
            biteTimer -= dt;
            if (biteTimer > 0f) return;
            biteTimer = biteCooldown;
            PlayOnce(AnimStates.Bite, 0.3f);
            AudioManager.PlaySfx(AudioIds.BarroBite);
            attackTarget.Health.TakeDamage(new DamageInfo(biteDamage, Position, gameObject, DamageType.Attack));
            var perception = attackTarget.GetComponent<StealthPerception>();
            if (perception != null) perception.Alert(Position);
        }

        // ========================================================== Movement

        void MoveToward(Vector2 target, float speed, float stopDistance)
        {
            float dx = target.x - Position.x;
            float vx;
            if (Mathf.Abs(dx) <= stopDistance)
            {
                vx = Mathf.MoveTowards(rb.linearVelocity.x, 0f, 40f * Time.fixedDeltaTime);
            }
            else
            {
                facing = dx > 0f ? 1 : -1;
                vx = facing * speed;
            }

            float vy = rb.linearVelocity.y;
            if (grounded && Mathf.Abs(dx) > stopDistance && ShouldJump(target, dx)) vy = JumpSpeedFor(target.y - Position.y);
            rb.linearVelocity = new Vector2(vx, vy);

            // Stuck detection: trying to move but not getting anywhere.
            bool tryingToMove = Mathf.Abs(dx) > stopDistance + 0.5f;
            stuckTimer = tryingToMove && Mathf.Abs(rb.linearVelocity.x) < 0.3f ? stuckTimer + Time.fixedDeltaTime : 0f;
        }

        bool ShouldJump(Vector2 target, float dx)
        {
            var b = col.bounds;
            Vector2 dir = new Vector2(Mathf.Sign(dx), 0f);
            bool wallAhead = PhysicsUtil.Raycast(b.center, dir, b.extents.x + 0.35f, Layers.SolidMask).collider != null;
            bool targetAbove = target.y > Position.y + 1.2f && Mathf.Abs(dx) < 5f;
            Vector2 front = new Vector2(dx > 0f ? b.max.x + 0.4f : b.min.x - 0.4f, b.min.y + 0.1f);
            bool gapAhead = PhysicsUtil.Raycast(front, Vector2.down, 1.5f, Layers.GroundMask).collider == null && target.y > Position.y - 1f;
            return wallAhead || targetAbove || gapAhead;
        }

        float JumpSpeedFor(float heightDifference)
        {
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            float needed = Mathf.Sqrt(2f * g * Mathf.Clamp(heightDifference + 1f, 1.2f, 4.5f));
            return Mathf.Min(Mathf.Max(needed, 8f), jumpVelocity * 1.2f);
        }

        void Stop()
        {
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, 40f * Time.fixedDeltaTime), rb.linearVelocity.y);
        }

        bool CheckGrounded()
        {
            if (col == null) return true;
            var b = col.bounds;
            return PhysicsUtil.BoxCast(new Vector2(b.center.x, b.min.y + 0.1f), new Vector2(b.size.x * 0.8f, 0.05f),
                Vector2.down, 0.18f, Layers.GroundMask, null, 0.6f).collider != null;
        }

        // ============================================================ Carrying

        void PickUp(BarroFetchable item)
        {
            item.Carried = true;
            var body = item.GetComponent<Rigidbody2D>();
            if (body != null) body.simulated = false;
            item.transform.SetParent(mouth != null ? mouth : transform, false);
            item.transform.localPosition = mouth != null ? Vector3.zero : new Vector3(0.7f, 0.6f, 0f);
            AudioManager.PlaySfx(AudioIds.BarroSniff);
        }

        void ReleaseCarried(bool delivered)
        {
            if (fetchTarget == null || !fetchTarget.Carried) return;
            var item = fetchTarget;
            fetchTarget = null;
            item.transform.SetParent(null, true);
            item.Carried = false;
            var body = item.GetComponent<Rigidbody2D>();
            if (body != null) body.simulated = true;
            if (delivered)
            {
                // Hand it over: the player collects pickups automatically when this close.
                var player = PlayerController.Instance;
                if (player != null) item.transform.position = player.Center;
                item.onDelivered.Invoke();
                Bark(false);
            }
        }

        // ========================================================= Senses, voice

        void UpdateGrowl()
        {
            if (Current == Mode.Downed || Current == Mode.Guard) return;
            growlTimer -= Time.deltaTime;
            if (growlTimer > 0f) return;
            growlTimer = growlInterval;
            foreach (var threat in HiddenThreat.Active)
            {
                if (threat == null) continue;
                if (Vector2.Distance(Position, threat.transform.position) > growlRange) continue;
                var enemy = threat.GetComponent<Enemy>();
                if (enemy != null && enemy.IsDead) continue;
                threat.Reveal(3f);
                facing = threat.transform.position.x >= Position.x ? 1 : -1;
                PlayOnce(AnimStates.Growl, 0.9f);
                AudioManager.PlaySfx(AudioIds.BarroGrowl);
                return;
            }
        }

        void Bark(bool loud)
        {
            PlayOnce(AnimStates.Bark, 0.4f);
            AudioManager.PlaySfx(AudioIds.BarroBark);
            if (loud) Noise.Emit(Position, barkNoiseRadius, gameObject);
        }

        void PlayOnce(string state, float seconds)
        {
            oneShotAnim = state;
            oneShotTimer = seconds;
        }

        string ChooseAnimation()
        {
            if (Current == Mode.Downed) return AnimStates.Downed;
            if (oneShotTimer > 0f && !string.IsNullOrEmpty(oneShotAnim)) return oneShotAnim;
            if (Current == Mode.Guard) return AnimStates.Sit;
            if (!grounded) return rb.linearVelocity.y > 0f ? AnimStates.Jump : AnimStates.Fall;
            if (Mathf.Abs(rb.linearVelocity.x) > 0.3f) return AnimStates.Run;
            return idleTimer > 3f ? AnimStates.Sit : AnimStates.Idle;
        }

        void LateUpdate()
        {
            if (visual == null) return;
            Vector3 s = visual.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            visual.localScale = s;
        }

        // =========================================================== Damage

        /// <summary>An enemy blow landed on Barro.</summary>
        public bool TakeHit(DamageInfo info)
        {
            if (Current == Mode.Downed || Current == Mode.Scripted) return false;
            return Health.TakeDamage(info);
        }

        void OnHurt(DamageInfo info)
        {
            lastHurtTime = Time.time;
            AudioManager.PlaySfx(AudioIds.BarroWhine);
            DreadMeter.Spike(12f);
            if (rb != null)
            {
                float dir = Mathf.Sign(Position.x - info.sourcePosition.x);
                rb.linearVelocity = new Vector2(dir * 5f, 6f);
            }
        }

        void OnKnockedDown()
        {
            ReleaseCarried(false);
            SetMode(Mode.Downed);
            DreadMeter.Spike(25f);
            GameEvents.RaiseToast("Barro is down!");
        }

        void GetUp()
        {
            Health.Revive();
            SetMode(Mode.Follow);
            AudioManager.PlaySfx(AudioIds.BarroBark);
        }

        void UpdateRegen()
        {
            if (Current == Mode.Downed || Health.Current >= Health.Max) return;
            if (Time.time - lastHurtTime < regenDelay) return;
            regenTimer -= Time.deltaTime;
            if (regenTimer > 0f) return;
            regenTimer = 3f;
            Health.Heal(1);
        }

        // ============================================================ Rescue

        /// <summary>Called by the player's Health when a blow would kill them.</summary>
        public bool TryRescue(DamageInfo info)
        {
            if (!RescueAvailable || Current == Mode.Downed || Current == Mode.Scripted || !isActiveAndEnabled) return false;
            var player = PlayerController.Instance;
            if (player == null || Vector2.Distance(Position, player.Center) > rescueRange) return false;
            RescueAvailable = false;
            StartCoroutine(RescueRoutine(player, info.sourcePosition));
            return true;
        }

        IEnumerator RescueRoutine(PlayerController player, Vector2 danger)
        {
            player.Freeze();
            player.PlayerHealth.Health.SetInvulnerable(3f);
            GameEvents.RaiseToast("Barro dragged you out!");
            AudioManager.PlaySfx(AudioIds.BarroGrowl);
            if (Vector2.Distance(Position, player.Center) > 2.5f) PlaceNear(player, true);

            float dir = Mathf.Sign(player.Center.x - danger.x);
            if (Mathf.Approximately(dir, 0f)) dir = -player.Facing;
            facing = (int)dir;
            PlayOnce(AnimStates.Drag, 1f);

            float t = 0f;
            while (t < 0.9f && player != null)
            {
                float step = 4.5f * Time.fixedDeltaTime;
                bool blocked = PhysicsUtil.Raycast(player.Center, new Vector2(dir, 0f), 0.6f + step, Layers.SolidMask).collider != null;
                if (!blocked)
                {
                    player.Body.position += new Vector2(dir * step, 0f);
                    rb.position = player.Body.position + new Vector2(dir * 0.9f, 0f);
                }
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            if (player != null) player.Unfreeze();
        }

        void UpdateRescue()
        {
            if (RescueAvailable) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            if (Time.time - player.PlayerHealth.LastDamageTime > rescueResetTime) RescueAvailable = true;
        }

        // ============================================================ Scripted

        /// <summary>Story control: walk to a point (or stop when null), ignoring commands.</summary>
        public void Script(Vector2? walkTo)
        {
            SetMode(Mode.Scripted);
            hasScriptedTarget = walkTo.HasValue;
            if (walkTo.HasValue) scriptedTarget = walkTo.Value;
        }

        public void EndScript()
        {
            hasScriptedTarget = false;
            SetMode(Mode.Follow);
        }

        public void PlayAnimation(string state, float seconds) => PlayOnce(state, seconds);
    }
}
