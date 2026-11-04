using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Example ground boss with three attacks:
    ///   Charge  - telegraphs, then rushes across the arena; stunned if it hits a wall.
    ///   Leap    - jumps at the player and sends shockwaves along the ground on landing.
    ///   Volley  - (phase 2 only) fires a fan of projectiles.
    /// Phase 2 makes everything faster. Tune the numbers in the inspector, or copy
    /// this class as a starting point for new bosses.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BruteBoss : Boss
    {
        [Header("Movement")]
        public float walkSpeed = 3f;
        public float walkTime = 1f;
        public float recoverTime = 0.9f;
        public float phaseTwoSpeedMultiplier = 1.3f;

        [Header("Charge")]
        public float chargeTelegraph = 0.6f;
        public float chargeSpeed = 13f;
        public float chargeMaxTime = 1.6f;
        public float wallStunTime = 1.1f;

        [Header("Leap")]
        public float leapTelegraph = 0.45f;
        public float leapHeight = 5f;
        public float maxLeapHorizontalSpeed = 12f;
        public Projectile shockwavePrefab;
        public float shockwaveSpeed = 9f;

        [Header("Volley (phase 2)")]
        public Projectile volleyPrefab;
        public int volleyCount = 5;
        public float volleySpreadDegrees = 50f;
        public float volleySpeed = 8f;

        string animState = AnimStates.Idle;

        protected override void Awake()
        {
            base.Awake();
            currencyDrop = new Vector2Int(20, 30);
        }

        protected override void OnAwake()
        {
            StartCoroutine(Brain());
        }

        protected override string AnimationState() => animState;

        IEnumerator Brain()
        {
            int lastChoice = -1;
            while (!IsDead)
            {
                float speed = Phase == 2 ? phaseTwoSpeedMultiplier : 1f;
                int options = Phase == 2 && volleyPrefab != null ? 3 : 2;
                int choice = Random.Range(0, options);
                if (choice == lastChoice) choice = (choice + 1) % options;
                lastChoice = choice;

                if (choice == 0) yield return Charge(speed);
                else if (choice == 1) yield return Leap(speed);
                else yield return Volley();

                StopHorizontal();
                animState = AnimStates.Idle;
                yield return Wait(recoverTime / speed);
                yield return WalkTowardPlayer(walkTime / speed, speed);
            }
        }

        void StopHorizontal()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        IEnumerator WalkTowardPlayer(float duration, float speed)
        {
            animState = AnimStates.Move;
            float t = 0f;
            while (t < duration && !IsDead)
            {
                Facing = DirectionToPlayer();
                float vx = Player != null && Mathf.Abs(Player.Center.x - Center.x) > 1f ? Facing * walkSpeed * speed : 0f;
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            StopHorizontal();
        }

        IEnumerator Charge(float speed)
        {
            Facing = DirectionToPlayer();
            animState = AnimStates.Telegraph;
            StopHorizontal();
            yield return Wait(chargeTelegraph / speed);

            animState = AnimStates.Charge;
            AudioManager.PlaySfx(AudioIds.BossCharge);
            float t = 0f;
            while (t < chargeMaxTime && !IsDead)
            {
                if (WallAhead(Facing))
                {
                    StopHorizontal();
                    AudioManager.PlaySfx(AudioIds.BossSlam);
                    CameraController.Shake(0.4f, 0.4f);
                    animState = AnimStates.Stunned;
                    yield return Wait(wallStunTime);
                    yield break;
                }
                rb.linearVelocity = new Vector2(Facing * chargeSpeed * speed, rb.linearVelocity.y);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator Leap(float speed)
        {
            Facing = DirectionToPlayer();
            animState = AnimStates.Telegraph;
            StopHorizontal();
            yield return Wait(leapTelegraph / speed);
            if (IsDead) yield break;

            float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            if (gravity < 0.01f) gravity = 9.81f;
            float vy = Mathf.Sqrt(2f * gravity * leapHeight);
            float flightTime = 2f * vy / gravity;
            float targetX = Player != null ? Player.Center.x : Center.x;
            float vx = Mathf.Clamp((targetX - Center.x) / flightTime, -maxLeapHorizontalSpeed, maxLeapHorizontalSpeed);

            animState = AnimStates.Leap;
            rb.linearVelocity = new Vector2(vx, vy);
            yield return Wait(0.2f);

            float airTime = 0f;
            while (!IsDead && airTime < 4f && !IsGroundedNow())
            {
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
                airTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            if (IsDead) yield break;

            StopHorizontal();
            animState = AnimStates.Attack;
            AudioManager.PlaySfx(AudioIds.BossSlam);
            CameraController.Shake(0.45f, 0.35f);
            var b = col.bounds;
            Vector2 feet = new Vector2(b.center.x, b.min.y + 0.4f);
            EffectsManager.Spawn(EffectIds.BossSlam, feet);
            if (shockwavePrefab != null)
            {
                SpawnProjectile(shockwavePrefab, feet + Vector2.left * b.extents.x, Vector2.left, shockwaveSpeed);
                SpawnProjectile(shockwavePrefab, feet + Vector2.right * b.extents.x, Vector2.right, shockwaveSpeed);
            }
            yield return Wait(0.35f);
        }

        IEnumerator Volley()
        {
            Facing = DirectionToPlayer();
            animState = AnimStates.Telegraph;
            StopHorizontal();
            yield return Wait(0.5f);
            if (IsDead || Player == null) yield break;

            animState = AnimStates.Attack;
            Vector2 origin = Center + Vector2.up * 0.5f;
            Vector2 toPlayer = (Player.Center - origin).normalized;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            int count = Mathf.Max(1, volleyCount);
            for (int i = 0; i < count; i++)
            {
                float k = count == 1 ? 0.5f : (float)i / (count - 1);
                float angle = (baseAngle - volleySpreadDegrees * 0.5f + volleySpreadDegrees * k) * Mathf.Deg2Rad;
                SpawnProjectile(volleyPrefab, origin, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), volleySpeed);
            }
            AudioManager.PlaySfx(AudioIds.EnemyShoot);
            yield return Wait(0.5f);
        }

        void SpawnProjectile(Projectile prefab, Vector2 at, Vector2 dir, float speed)
        {
            if (prefab == null) return;
            var p = Instantiate(prefab, at, Quaternion.identity);
            p.Launch(dir, gameObject, speed);
        }
    }
}
