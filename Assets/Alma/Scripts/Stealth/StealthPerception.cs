using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    public enum Awareness
    {
        Unaware,
        Suspicious,
        Alert,
        Searching
    }

    /// <summary>
    /// Eyes and ears for an enemy. Sees the player inside a cone in front of it (blocked by
    /// walls, hiding spots and disguises), hears noises, and moves between awareness states:
    ///   Unaware    -> patrolling
    ///   Suspicious -> heard or glimpsed something; goes to look ("?")
    ///   Alert      -> has seen the player; chases ("!")
    ///   Searching  -> lost the player; checks the last known spot, then gives up
    /// Being seen fills a detection meter first, faster when close and slower when sneaking.
    /// </summary>
    public class StealthPerception : MonoBehaviour
    {
        [Header("Sight")]
        public float viewDistance = 8f;
        [Tooltip("Half-angle of the view cone in degrees.")]
        [Range(5f, 180f)] public float viewAngle = 35f;
        [Tooltip("Within this distance the player is noticed in any direction (unless hidden).")]
        public float closeRange = 1.6f;
        public Vector2 eyeOffset = new Vector2(0f, 1.4f);
        [Tooltip("Seconds of being seen at mid range before the alarm.")]
        public float detectionTime = 1.1f;

        [Header("Hearing")]
        [Tooltip("Scales every noise radius for this enemy (2 = hears twice as far).")]
        public float hearingMultiplier = 1f;

        [Header("Memory")]
        public float loseSightTime = 2.5f;
        public float searchTime = 6f;
        public float suspicionTime = 5f;

        [Header("Disguise")]
        [Tooltip("The Hollowed ignore a disguised player who isn't running or fighting.")]
        public bool fooledByDisguise = false;

        static readonly List<StealthPerception> active = new List<StealthPerception>();
        public static IReadOnlyList<StealthPerception> Active => active;

        public Awareness State { get; private set; } = Awareness.Unaware;
        /// <summary>0..1: how close this enemy is to spotting the player.</summary>
        public float Detection { get; private set; }
        /// <summary>Where the enemy thinks the player (or a noise) is.</summary>
        public Vector2 Target { get; private set; }
        public bool CanSeePlayerNow { get; private set; }
        public Vector2 Eyes => (Vector2)transform.position + new Vector2(eyeOffset.x * Facing, eyeOffset.y);

        /// <summary>Which way the owner faces (+1 right, -1 left). Set by the enemy script.</summary>
        public Func<int> facing;

        public event Action<Awareness> StateChanged;

        int Facing => facing != null ? facing() : 1;
        float stateTimer;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            var player = PlayerController.Instance;
            CanSeePlayerNow = player != null && CanSee(player);

            if (CanSeePlayerNow)
            {
                Target = player.Center;
                float distance = Vector2.Distance(Eyes, player.Center);
                float closeness = Mathf.Lerp(2.5f, 0.6f, Mathf.Clamp01(distance / Mathf.Max(0.1f, viewDistance)));
                float stealth = player.IsSneaking ? 0.55f : 1f;
                float speed = State == Awareness.Alert || State == Awareness.Searching ? 3f : 1f;
                Detection = Mathf.Min(1f, Detection + dt / Mathf.Max(0.05f, detectionTime) * closeness * stealth * speed);
                if (Detection >= 1f)
                {
                    SetState(Awareness.Alert);
                    stateTimer = loseSightTime;
                }
                else if (State == Awareness.Unaware && Detection > 0.35f)
                {
                    SetState(Awareness.Suspicious);
                    stateTimer = suspicionTime;
                }
                return;
            }

            Detection = Mathf.Max(0f, Detection - dt * 0.35f);
            if (State == Awareness.Unaware) return;
            stateTimer -= dt;
            if (stateTimer > 0f) return;

            switch (State)
            {
                case Awareness.Alert:
                    SetState(Awareness.Searching);
                    stateTimer = searchTime;
                    break;
                case Awareness.Searching:
                case Awareness.Suspicious:
                    SetState(Awareness.Unaware);
                    Detection = 0f;
                    break;
            }
        }

        bool CanSee(PlayerController player)
        {
            if (player.PlayerHealth.IsDead) return false;
            var stealth = player.Stealth;
            Vector2 eyes = Eyes;
            Vector2 to = player.Center - eyes;
            float distance = to.magnitude;
            if (distance > viewDistance) return false;

            bool close = distance <= closeRange;
            if (stealth != null)
            {
                if (stealth.IsHidden && !(close && State == Awareness.Alert)) return false;
                if (stealth.IsDisguised && fooledByDisguise && !stealth.ActingSuspicious) return false;
            }
            if (!close)
            {
                float angle = Vector2.Angle(new Vector2(Facing, 0f), to);
                if (angle > viewAngle) return false;
            }
            return PhysicsUtil.HasLineOfSight(eyes, player.Center);
        }

        /// <summary>Called by Noise.Emit.</summary>
        public void HearNoise(Vector2 position, float radius)
        {
            float distance = Vector2.Distance(transform.position, position);
            if (distance > radius * hearingMultiplier) return;
            if (State == Awareness.Alert)
            {
                Target = position;
                return;
            }
            Target = position;
            Detection = Mathf.Max(Detection, 0.4f);
            SetState(Awareness.Suspicious);
            stateTimer = suspicionTime;
        }

        /// <summary>Instantly alerts this enemy (e.g. it was attacked).</summary>
        public void Alert(Vector2 at)
        {
            Target = at;
            Detection = 1f;
            SetState(Awareness.Alert);
            stateTimer = loseSightTime;
        }

        public void ResetAwareness()
        {
            Detection = 0f;
            SetState(Awareness.Unaware);
        }

        void SetState(Awareness state)
        {
            if (State == state) return;
            var previous = State;
            State = state;
            if (state == Awareness.Alert && previous != Awareness.Alert) AudioManager.PlaySfx(AudioIds.GuardAlert);
            else if (state == Awareness.Suspicious && previous == Awareness.Unaware) AudioManager.PlaySfx(AudioIds.GuardSuspicious);
            StateChanged?.Invoke(state);
        }

        void OnDrawGizmosSelected()
        {
            Vector2 eyes = Application.isPlaying ? Eyes : (Vector2)transform.position + eyeOffset;
            int f = Application.isPlaying ? Facing : 1;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Vector2 forward = new Vector2(f, 0f);
            Vector2 up = Quaternion.Euler(0f, 0f, viewAngle) * forward;
            Vector2 down = Quaternion.Euler(0f, 0f, -viewAngle) * forward;
            Gizmos.DrawLine(eyes, eyes + up * viewDistance);
            Gizmos.DrawLine(eyes, eyes + down * viewDistance);
            Gizmos.DrawWireSphere(eyes, closeRange);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
