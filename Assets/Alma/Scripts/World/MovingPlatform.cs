using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Moves between waypoints (relative to its start position) and carries the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Tooltip("Points relative to the starting position. The platform starts at the first one.")]
        public Vector2[] waypoints = { Vector2.zero, new Vector2(5f, 0f) };
        public float speed = 3f;
        public float waitAtEachPoint = 0.5f;
        [Tooltip("Go back and forth (true) or loop from the last point to the first (false).")]
        public bool pingPong = true;

        public Vector2 Velocity { get; private set; }

        Rigidbody2D rb;
        Vector2 origin;
        int index;
        int direction = 1;
        float waitTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = transform.position;
            if (waypoints.Length > 0) rb.position = origin + waypoints[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (waypoints.Length < 2)
            {
                Velocity = Vector2.zero;
                return;
            }
            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                Velocity = Vector2.zero;
                return;
            }

            Vector2 target = origin + waypoints[index];
            Vector2 next = Vector2.MoveTowards(rb.position, target, speed * dt);
            Velocity = (next - rb.position) / dt;
            rb.MovePosition(next);

            if ((next - target).sqrMagnitude < 0.0001f)
            {
                waitTimer = waitAtEachPoint;
                AdvanceIndex();
            }
        }

        void AdvanceIndex()
        {
            if (pingPong)
            {
                if (index + direction >= waypoints.Length || index + direction < 0) direction = -direction;
                index += direction;
            }
            else
            {
                index = (index + 1) % waypoints.Length;
            }
        }

        void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            Vector2 o = Application.isPlaying ? origin : (Vector2)transform.position;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.DrawWireSphere(o + waypoints[i], 0.2f);
                if (i > 0) Gizmos.DrawLine(o + waypoints[i - 1], o + waypoints[i]);
            }
        }
    }
}
