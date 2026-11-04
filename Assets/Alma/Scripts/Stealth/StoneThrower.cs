using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Throw a stone with THROW. Where it lands it makes a noise, drawing nearby enemies to
    /// look. Hold UP to lob it high. High Dread makes your aim shake.
    /// </summary>
    public class StoneThrower : MonoBehaviour
    {
        public float cooldown = 0.7f;
        public Vector2 throwVelocity = new Vector2(9f, 6f);
        public Vector2 lobVelocity = new Vector2(5.5f, 11f);
        public float noiseRadius = 7.5f;
        [Tooltip("Optional stone prefab (needs a Rigidbody2D, a Collider2D and ThrownStone). One is generated if empty.")]
        public ThrownStone stonePrefab;
        public float throwAnimationTime = 0.2f;

        public bool IsThrowing => throwTimer > 0f;

        PlayerController controller;
        float cooldownTimer;
        float throwTimer;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (throwTimer > 0f) throwTimer -= Time.deltaTime;

            var input = InputReader.Instance;
            var gm = GameManager.Instance;
            if (input == null || controller == null || !input.ThrowPressed || cooldownTimer > 0f) return;
            if (gm != null && gm.State != GameState.Playing) return;
            if (controller.IsFrozen || controller.IsRolling || controller.PlayerHealth.IsDead) return;
            if (controller.Profile != null && !controller.Profile.CanThrowStones) return;
            Throw();
        }

        void Throw()
        {
            cooldownTimer = cooldown;
            throwTimer = throwAnimationTime;
            Vector2 v = controller.InputY > 0 ? lobVelocity : throwVelocity;
            v.x *= controller.Facing;
            float jitter = DreadMeter.AimJitterDegrees;
            if (jitter > 0f) v = Quaternion.Euler(0f, 0f, Random.Range(-jitter, jitter)) * v;

            Vector2 origin = controller.Center + new Vector2(controller.Facing * 0.5f, 0.4f);
            var stone = stonePrefab != null ? Instantiate(stonePrefab, origin, Quaternion.identity) : ThrownStone.CreateDefault(origin);
            stone.Launch(v, noiseRadius, gameObject);
            AudioManager.PlaySfx(AudioIds.StoneThrow);
        }
    }
}
