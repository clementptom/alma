using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Player movement: run, jump (coyote time, jump buffer, variable height), drop through
    /// one-way platforms, moving-platform riding, plus the unlockable abilities
    /// (dash, wall slide / wall jump, double jump, glide), knockback and pogo bouncing.
    ///
    /// Input is read in Update and applied in FixedUpdate. Animation is handled by
    /// PlayerAnimator, which reads the public state below.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerCombat))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        /// <summary>
        /// When a level scene contains its own Player (handy for testing) but a persistent
        /// player already exists, the scene copy is removed and its position is kept here.
        /// </summary>
        public static Vector2? SceneDefaultSpawn { get; set; }

        [Header("Run")]
        public float runSpeed = 8f;
        public float groundAcceleration = 90f;
        public float groundDeceleration = 120f;
        public float airAcceleration = 70f;
        public float airDeceleration = 45f;

        [Header("Jump")]
        [Tooltip("Height in world units of a full (held) jump.")]
        public float jumpHeight = 3.4f;
        [Tooltip("Seconds to reach the top of a full jump.")]
        public float timeToApex = 0.38f;
        public float fallGravityMultiplier = 1.7f;
        [Tooltip("Upward speed is multiplied by this when JUMP is released early.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        public float maxFallSpeed = 20f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;

        [Header("Double Jump")]
        public float doubleJumpHeight = 2.6f;

        [Header("Wall Slide / Wall Jump")]
        public float wallSlideSpeed = 3f;
        public Vector2 wallJumpVelocity = new Vector2(9f, 15.5f);
        [Tooltip("Horizontal input is ignored briefly after a wall jump so the jump pushes you away.")]
        public float wallJumpInputLock = 0.16f;
        public float wallCoyoteTime = 0.1f;

        [Header("Dash")]
        public float dashSpeed = 22f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.45f;
        public bool dashInvulnerable = false;

        [Header("Glide")]
        public float glideFallSpeed = 2.2f;

        [Header("Getting Hit")]
        public Vector2 knockbackVelocity = new Vector2(8f, 9f);
        public float knockbackControlLock = 0.2f;

        [Header("Detection")]
        public float groundCheckDistance = 0.08f;
        public float wallCheckDistance = 0.08f;

        [Header("References")]
        [Tooltip("Child holding the sprites/animator. It is mirrored to face left/right.")]
        public Transform visual;

        // ---- Public state (read by animation, combat, camera, AI) ----
        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsWallSliding { get; private set; }
        public bool IsGliding { get; private set; }
        public bool IsFrozen => freezeCount > 0;
        public Vector2 Velocity => rb.linearVelocity;
        public int InputX { get; private set; }
        public int InputY { get; private set; }
        /// <summary>Set for one frame-ish after a double jump, for animation.</summary>
        public float DoubleJumpTimer { get; private set; }
        public float TimeSinceLanded { get; private set; }
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        public Rigidbody2D Body => rb;
        public CapsuleCollider2D Collider => col;
        public PlayerHealth PlayerHealth { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public Vector2 LastSafePosition => lastSafePosition;

        Rigidbody2D rb;
        CapsuleCollider2D col;

        float baseGravityScale;
        float jumpVelocity;
        float doubleJumpVelocity;

        float moveX;
        float coyoteTimer;
        float jumpBufferTimer;
        float dashRequestTimer;
        bool jumpHeld;
        bool isJumping;
        bool usedDoubleJump;
        bool usedAirDash;
        bool jumpedThisStep;
        bool launched;

        float wallCoyoteTimer;
        int wallCoyoteDir;
        int wallDir;

        float dashTimer;
        float dashCooldownTimer;
        int dashDir;

        float inputLockTimer;
        float forcedMoveTimer;
        int forcedMoveDir;
        int freezeCount;

        Collider2D groundCollider;
        MovingPlatform groundPlatform;
        Collider2D droppingThrough;
        float dropTimer;

        Vector2 lastSafePosition;
        Vector2 pendingSafePosition;
        float safeGroundTimer;
        float footstepTimer;
        bool wasGrounded;

        SpriteRenderer[] spriteRenderers;
        static PhysicsMaterial2D frictionless;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A persistent player already exists: this is a scene copy used for testing.
                SceneDefaultSpawn = transform.position;
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CapsuleCollider2D>();
            PlayerHealth = GetComponent<PlayerHealth>();
            Combat = GetComponent<PlayerCombat>();

            if (frictionless == null) frictionless = new PhysicsMaterial2D("PlayerFrictionless") { friction = 0f, bounciness = 0f };
            rb.sharedMaterial = frictionless;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            int playerLayer = Layers.Index(Layers.Player);
            if (playerLayer >= 0) gameObject.layer = playerLayer;

            if (visual == null) visual = transform.Find("Visual");
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            RecalculateJumpPhysics();
            lastSafePosition = transform.position;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            SceneDefaultSpawn = null;
        }

        void OnValidate()
        {
            if (timeToApex < 0.05f) timeToApex = 0.05f;
            if (jumpHeight < 0.1f) jumpHeight = 0.1f;
        }

        /// <summary>Converts jump height / time-to-apex into gravity and launch speed.</summary>
        public void RecalculateJumpPhysics()
        {
            float gravity = 2f * jumpHeight / (timeToApex * timeToApex);
            jumpVelocity = gravity * timeToApex;
            doubleJumpVelocity = Mathf.Sqrt(2f * gravity * doubleJumpHeight);
            float worldGravity = Mathf.Abs(Physics2D.gravity.y) > 0.01f ? Mathf.Abs(Physics2D.gravity.y) : 9.81f;
            baseGravityScale = gravity / worldGravity;
            if (rb != null) rb.gravityScale = baseGravityScale;
        }

        bool Has(AbilityType ability)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.HasAbility(ability);
        }

        bool CanControl
        {
            get
            {
                var gm = GameManager.Instance;
                bool stateAllows = gm == null || gm.State == GameState.Playing;
                return stateAllows && !IsFrozen && !PlayerHealth.IsDead;
            }
        }

        // ------------------------------------------------------------------ Update

        void Update()
        {
            var input = InputReader.Instance;
            if (input == null) return;

            if (CanControl)
            {
                InputX = input.MoveX;
                InputY = input.MoveY;
                jumpHeld = input.JumpHeld;
                if (input.JumpPressed) jumpBufferTimer = jumpBufferTime;
                if (input.DashPressed) dashRequestTimer = 0.1f;
            }
            else
            {
                InputX = 0;
                InputY = 0;
                jumpHeld = false;
                jumpBufferTimer = 0f;
                dashRequestTimer = 0f;
            }

            if (DoubleJumpTimer > 0f) DoubleJumpTimer -= Time.deltaTime;
        }

        // ------------------------------------------------------------- FixedUpdate

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            jumpedThisStep = false;

            Tick(ref jumpBufferTimer, dt);
            Tick(ref dashRequestTimer, dt);
            Tick(ref dashCooldownTimer, dt);
            Tick(ref inputLockTimer, dt);
            Tick(ref forcedMoveTimer, dt);
            UpdateDropThrough(dt);

            CheckGround();
            CheckWalls();
            UpdateGroundedState(dt);

            if (PlayerHealth.IsDead)
            {
                moveX = Mathf.MoveTowards(moveX, 0f, groundDeceleration * dt);
                rb.linearVelocity = new Vector2(IsGrounded ? 0f : moveX, rb.linearVelocity.y);
                return;
            }

            if (IsFrozen)
            {
                moveX = 0f;
                IsDashing = false;
                IsWallSliding = false;
                IsGliding = false;
                rb.gravityScale = baseGravityScale;
                rb.linearVelocity = new Vector2(0f, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
                return;
            }

            if (IsDashing)
            {
                DashStep(dt);
                return;
            }

            int inputX = forcedMoveTimer > 0f ? forcedMoveDir : InputX;
            float vy = rb.linearVelocity.y;

            // Horizontal movement
            if (inputLockTimer <= 0f)
            {
                float target = inputX * runSpeed;
                float accel = IsGrounded
                    ? (inputX != 0 ? groundAcceleration : groundDeceleration)
                    : (inputX != 0 ? airAcceleration : airDeceleration);
                moveX = Mathf.MoveTowards(moveX, target, accel * dt);
                if (inputX != 0 && !Combat.IsAttacking) Facing = inputX;
            }
            if (wallDir != 0 && Mathf.Sign(moveX) == wallDir && Mathf.Abs(moveX) > 0.01f) moveX = 0f;

            // Wall slide
            bool canWallSlide = Has(AbilityType.WallJump) && !IsGrounded && wallDir != 0 && vy < 0.1f;
            IsWallSliding = canWallSlide && inputX == wallDir;
            if (IsWallSliding)
            {
                wallCoyoteTimer = wallCoyoteTime;
                wallCoyoteDir = wallDir;
                usedDoubleJump = false;
                usedAirDash = false;
                Facing = -wallDir;
            }
            else
            {
                Tick(ref wallCoyoteTimer, dt);
            }

            // Jumping
            if (jumpBufferTimer > 0f)
            {
                if (IsGrounded && InputY < 0 && IsOneWay(groundCollider))
                {
                    StartDropThrough(groundCollider);
                    jumpBufferTimer = 0f;
                    vy = -2f;
                }
                else if (IsGrounded || coyoteTimer > 0f)
                {
                    vy = Jump(jumpVelocity, true);
                }
                else if (wallCoyoteTimer > 0f && Has(AbilityType.WallJump))
                {
                    vy = WallJump(wallCoyoteDir);
                }
                else if (Has(AbilityType.DoubleJump) && !usedDoubleJump)
                {
                    usedDoubleJump = true;
                    DoubleJumpTimer = 0.3f;
                    vy = Jump(doubleJumpVelocity, false);
                    AudioManager.PlaySfx(AudioIds.PlayerDoubleJump);
                    EffectsManager.Spawn(EffectIds.DoubleJump, FeetPosition);
                }
            }

            // Dash
            if (dashRequestTimer > 0f && dashCooldownTimer <= 0f && Has(AbilityType.Dash) && (IsGrounded || !usedAirDash))
            {
                StartDash(inputX);
                return;
            }

            // Variable jump height
            if (isJumping && !jumpHeld && vy > 0f)
            {
                vy *= jumpCutMultiplier;
                isJumping = false;
            }
            if (vy <= 0f) isJumping = false;

            // Gravity and fall speed
            IsGliding = Has(AbilityType.Glide) && !IsGrounded && !IsWallSliding && jumpHeld && vy < 0f;
            rb.gravityScale = baseGravityScale * (vy < 0f ? fallGravityMultiplier : 1f);
            float maxFall = IsWallSliding ? wallSlideSpeed : IsGliding ? glideFallSpeed : maxFallSpeed;
            if (vy < -maxFall) vy = -maxFall;

            // Moving platforms carry the player
            Vector2 carry = groundPlatform != null && IsGrounded ? groundPlatform.Velocity : Vector2.zero;
            if (groundPlatform != null && IsGrounded && !jumpedThisStep) vy = carry.y;

            rb.linearVelocity = new Vector2(moveX + carry.x, vy);

            UpdateFootsteps(dt);
        }

        static void Tick(ref float timer, float dt)
        {
            if (timer > 0f) timer -= dt;
        }

        Vector2 FeetPosition
        {
            get
            {
                var b = col.bounds;
                return new Vector2(b.center.x, b.min.y);
            }
        }

        // --------------------------------------------------------------- Ground

        void CheckGround()
        {
            var b = col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.1f);
            Vector2 size = new Vector2(b.size.x * 0.85f, 0.05f);
            var hit = PhysicsUtil.BoxCast(origin, size, Vector2.down, 0.1f + groundCheckDistance,
                Layers.GroundMask, droppingThrough, 0.6f);

            Collider2D newGround = hit.collider;
            MovingPlatform platform = null;
            if (newGround != null) platform = newGround.GetComponentInParent<MovingPlatform>();

            float platformVy = platform != null ? platform.Velocity.y : 0f;
            bool rising = rb.linearVelocity.y - platformVy > 0.01f;
            if (newGround != null)
            {
                // One-way platforms only count when we're above them, not passing up through.
                if (IsOneWay(newGround) && (rising || hit.distance <= 0f)) newGround = null;
                // Right after a jump or bounce the ground is still within reach: ignore it.
                else if ((isJumping || launched) && rising) newGround = null;
            }
            if (!rising) launched = false;

            IsGrounded = newGround != null;
            groundCollider = newGround;
            groundPlatform = IsGrounded ? platform : null;
        }

        void UpdateGroundedState(float dt)
        {
            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                usedDoubleJump = false;
                if (!IsDashing) usedAirDash = false;
                TimeSinceLanded += dt;

                if (!wasGrounded)
                {
                    TimeSinceLanded = 0f;
                    AudioManager.PlaySfx(AudioIds.PlayerLand);
                    EffectsManager.Spawn(EffectIds.DustLand, FeetPosition);
                }

                // Remember solid, still ground so hazards can put us back here.
                if (groundPlatform == null)
                {
                    if (Vector2.Distance(pendingSafePosition, rb.position) > 0.5f)
                    {
                        pendingSafePosition = rb.position;
                        safeGroundTimer = 0f;
                    }
                    safeGroundTimer += dt;
                    if (safeGroundTimer > 0.15f) lastSafePosition = pendingSafePosition;
                }
            }
            else
            {
                Tick(ref coyoteTimer, dt);
                safeGroundTimer = 0f;
            }
            wasGrounded = IsGrounded;
        }

        static bool IsOneWay(Collider2D c)
        {
            if (c == null) return false;
            return c.gameObject.layer == Layers.Index(Layers.OneWayPlatform) || c.usedByEffector;
        }

        void StartDropThrough(Collider2D platform)
        {
            if (platform == null) return;
            droppingThrough = platform;
            dropTimer = 0.3f;
            Physics2D.IgnoreCollision(col, platform, true);
            IsGrounded = false;
            groundCollider = null;
            groundPlatform = null;
        }

        void UpdateDropThrough(float dt)
        {
            if (droppingThrough == null) return;
            dropTimer -= dt;
            if (dropTimer <= 0f)
            {
                Physics2D.IgnoreCollision(col, droppingThrough, false);
                droppingThrough = null;
            }
        }

        // ---------------------------------------------------------------- Walls

        void CheckWalls()
        {
            wallDir = 0;
            var b = col.bounds;
            Vector2 size = new Vector2(0.05f, b.size.y * 0.5f);
            float distance = b.extents.x + wallCheckDistance;
            if (PhysicsUtil.BoxCast(b.center, size, Vector2.right, distance, Layers.SolidMask, null, 0.9f).collider != null)
                wallDir = 1;
            else if (PhysicsUtil.BoxCast(b.center, size, Vector2.left, distance, Layers.SolidMask, null, 0.9f).collider != null)
                wallDir = -1;
        }

        // ----------------------------------------------------------------- Jumps

        float Jump(float velocity, bool fromGround)
        {
            isJumping = true;
            jumpedThisStep = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            IsGrounded = false;
            groundPlatform = null;
            if (fromGround)
            {
                AudioManager.PlaySfx(AudioIds.PlayerJump);
                EffectsManager.Spawn(EffectIds.DustJump, FeetPosition);
            }
            return velocity;
        }

        float WallJump(int fromWallDir)
        {
            moveX = -fromWallDir * wallJumpVelocity.x;
            Facing = -fromWallDir;
            inputLockTimer = wallJumpInputLock;
            wallCoyoteTimer = 0f;
            IsWallSliding = false;
            AudioManager.PlaySfx(AudioIds.PlayerWallJump);
            EffectsManager.Spawn(EffectIds.DustWall, new Vector2(Center.x + fromWallDir * col.bounds.extents.x, Center.y));
            isJumping = true;
            jumpedThisStep = true;
            jumpBufferTimer = 0f;
            return wallJumpVelocity.y;
        }

        // ------------------------------------------------------------------ Dash

        void StartDash(int inputX)
        {
            IsDashing = true;
            dashRequestTimer = 0f;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown + dashDuration;
            // From a wall slide, always dash away from the wall.
            dashDir = IsWallSliding ? -wallDir : inputX != 0 ? inputX : Facing;
            Facing = dashDir;
            if (!IsGrounded) usedAirDash = true;
            isJumping = false;
            IsWallSliding = false;
            IsGliding = false;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            if (dashInvulnerable) PlayerHealth.Health.SetInvulnerable(dashDuration);
            AudioManager.PlaySfx(AudioIds.PlayerDash);
            EffectsManager.Spawn(EffectIds.Dash, Center, dashDir < 0);
        }

        void DashStep(float dt)
        {
            dashTimer -= dt;
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            if (dashTimer <= 0f || wallDir == dashDir) EndDash();
        }

        void EndDash()
        {
            if (!IsDashing) return;
            IsDashing = false;
            moveX = dashDir * runSpeed;
            rb.gravityScale = baseGravityScale;
            rb.linearVelocity = new Vector2(moveX, 0f);
        }

        // ------------------------------------------------------- External forces

        /// <summary>Pushes the player away from a damage source.</summary>
        public void ApplyKnockback(Vector2 sourcePosition)
        {
            EndDash();
            float dir = Mathf.Sign(rb.position.x - sourcePosition.x);
            if (Mathf.Abs(rb.position.x - sourcePosition.x) < 0.01f) dir = -Facing;
            moveX = dir * knockbackVelocity.x;
            inputLockTimer = knockbackControlLock;
            isJumping = false;
            launched = true;
            rb.linearVelocity = new Vector2(moveX, knockbackVelocity.y);
        }

        /// <summary>Bounce upward, e.g. after a downward attack hits something (pogo).</summary>
        public void Bounce(float upwardSpeed)
        {
            EndDash();
            isJumping = false;
            usedDoubleJump = false;
            usedAirDash = false;
            launched = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, upwardSpeed);
        }

        /// <summary>Small horizontal push, e.g. recoil from hitting an enemy.</summary>
        public void Recoil(float horizontalSpeed, float duration = 0.08f)
        {
            if (IsDashing) return;
            moveX = horizontalSpeed;
            inputLockTimer = Mathf.Max(inputLockTimer, duration);
        }

        /// <summary>Walks the player in a direction for a moment (used when arriving through a door).</summary>
        public void ForceMove(int direction, float duration)
        {
            forcedMoveDir = direction;
            forcedMoveTimer = duration;
            if (direction != 0) Facing = direction;
        }

        public void Freeze()
        {
            freezeCount++;
        }

        public void Unfreeze()
        {
            freezeCount = Mathf.Max(0, freezeCount - 1);
        }

        public void ClearFreeze()
        {
            freezeCount = 0;
        }

        /// <summary>Instantly moves the player and resets momentum.</summary>
        public void Teleport(Vector2 position)
        {
            EndDash();
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            moveX = 0f;
            isJumping = false;
            jumpBufferTimer = 0f;
            forcedMoveTimer = 0f;
            inputLockTimer = 0f;
            lastSafePosition = position;
            pendingSafePosition = position;
            Physics2D.SyncTransforms();
        }

        public void SetFacing(int direction)
        {
            if (direction != 0) Facing = direction > 0 ? 1 : -1;
        }

        public void SetLastSafePosition(Vector2 position)
        {
            lastSafePosition = position;
            pendingSafePosition = position;
        }

        // ------------------------------------------------------------ Feedback

        void UpdateFootsteps(float dt)
        {
            if (!IsGrounded || Mathf.Abs(moveX) < 1f)
            {
                footstepTimer = 0.1f;
                return;
            }
            footstepTimer -= dt;
            if (footstepTimer <= 0f)
            {
                footstepTimer = 0.3f;
                AudioManager.PlaySfx(AudioIds.PlayerFootstep);
            }
        }

        void LateUpdate()
        {
            if (visual != null)
            {
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * Facing;
                visual.localScale = scale;
            }
            else
            {
                // No "Visual" child: mirror the sprites directly.
                foreach (var sr in spriteRenderers)
                {
                    if (sr != null) sr.flipX = Facing < 0;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            var c = GetComponent<CapsuleCollider2D>();
            if (c == null) return;
            var b = c.bounds;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - groundCheckDistance * 0.5f), new Vector3(b.size.x * 0.85f, groundCheckDistance));
        }
    }
}
