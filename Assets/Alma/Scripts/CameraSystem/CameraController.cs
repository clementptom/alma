using UnityEngine;

namespace Alma
{
    /// <summary>
    /// 2D follow camera: smooth follow with look-ahead, look up/down by holding UP/DOWN
    /// while standing, clamping to the current Room's bounds, and screen shake.
    /// Added to the scene's main camera automatically if it doesn't have one.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Main { get; private set; }

        [Header("Follow")]
        public float smoothTime = 0.12f;
        public Vector2 offset = new Vector2(0f, 1.2f);
        public float lookAheadDistance = 1.5f;
        public float lookAheadSmoothTime = 0.5f;

        [Header("Look Up / Down")]
        public float lookDistance = 3.5f;
        public float lookHoldDelay = 0.45f;

        [Header("Shake")]
        public float maxShake = 0.6f;

        public Camera Camera { get; private set; }
        public Rect? Bounds { get; private set; }

        Transform target;
        Vector3 velocity;
        float lookAhead;
        float lookAheadVelocity;
        float lookTimer;
        float lookOffset;
        float lookOffsetVelocity;

        float shakeAmplitude;
        float shakeTimer;
        float shakeDuration;
        Vector3 shakeOffset;

        void Awake()
        {
            Camera = GetComponent<Camera>();
            Main = this;
        }

        void OnEnable()
        {
            Main = this;
        }

        void OnDestroy()
        {
            if (Main == this) Main = null;
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap) Snap();
        }

        public void SetBounds(Rect? bounds, bool snap = false)
        {
            Bounds = bounds;
            if (snap) Snap();
        }

        /// <summary>Jump straight to the target with no smoothing.</summary>
        public void Snap()
        {
            if (target == null) AcquireTarget();
            if (target == null) return;
            lookAhead = 0f;
            lookOffset = 0f;
            velocity = Vector3.zero;
            Vector3 desired = Clamp(DesiredPosition());
            transform.position = desired;
        }

        void AcquireTarget()
        {
            var player = PlayerController.Instance;
            if (player != null) target = player.transform;
        }

        void LateUpdate()
        {
            if (target == null) AcquireTarget();
            if (target == null) return;

            var player = PlayerController.Instance;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            // Look ahead in the direction we're moving.
            float desiredLookAhead = 0f;
            if (player != null && Mathf.Abs(player.Velocity.x) > 0.5f) desiredLookAhead = player.Facing * lookAheadDistance;
            lookAhead = Mathf.SmoothDamp(lookAhead, desiredLookAhead, ref lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, dt);

            // Look up/down when holding vertical input while standing still.
            float desiredLook = 0f;
            if (player != null && player.IsGrounded && player.InputY != 0 && Mathf.Abs(player.Velocity.x) < 0.1f)
            {
                lookTimer += dt;
                if (lookTimer > lookHoldDelay) desiredLook = player.InputY * lookDistance;
            }
            else
            {
                lookTimer = 0f;
            }
            lookOffset = Mathf.SmoothDamp(lookOffset, desiredLook, ref lookOffsetVelocity, 0.25f, Mathf.Infinity, dt);

            Vector3 desired = Clamp(DesiredPosition());
            Vector3 position = Vector3.SmoothDamp(transform.position - shakeOffset, desired, ref velocity, smoothTime, Mathf.Infinity, dt);

            UpdateShake();
            transform.position = position + shakeOffset;
        }

        Vector3 DesiredPosition()
        {
            Vector3 p = target.position;
            return new Vector3(p.x + offset.x + lookAhead, p.y + offset.y + lookOffset, transform.position.z);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!Bounds.HasValue || Camera == null) return p;
            Rect r = Bounds.Value;
            float halfH = Camera.orthographicSize;
            float halfW = halfH * Camera.aspect;

            p.x = r.width <= halfW * 2f ? r.center.x : Mathf.Clamp(p.x, r.xMin + halfW, r.xMax - halfW);
            p.y = r.height <= halfH * 2f ? r.center.y : Mathf.Clamp(p.y, r.yMin + halfH, r.yMax - halfH);
            return p;
        }

        void UpdateShake()
        {
            if (shakeTimer <= 0f)
            {
                shakeOffset = Vector3.zero;
                return;
            }
            shakeTimer -= Time.unscaledDeltaTime;
            float strength = shakeAmplitude * Mathf.Clamp01(shakeTimer / Mathf.Max(0.0001f, shakeDuration));
            shakeOffset = (Vector3)(Random.insideUnitCircle * strength);
        }

        /// <summary>Shakes the main camera. Amplitude is in world units.</summary>
        public static void Shake(float amplitude, float duration)
        {
            if (Main == null || !GameSettings.ScreenShake) return;
            float remaining = Main.shakeTimer > 0f ? Main.shakeAmplitude * Main.shakeTimer / Mathf.Max(0.0001f, Main.shakeDuration) : 0f;
            if (amplitude < remaining) return; // a stronger shake is already playing
            Main.shakeAmplitude = Mathf.Min(amplitude, Main.maxShake);
            Main.shakeDuration = duration;
            Main.shakeTimer = duration;
        }
    }
}
