using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>Small wrappers around Physics2D queries with explicit trigger handling.</summary>
    public static class PhysicsUtil
    {
        static readonly RaycastHit2D[] castBuffer = new RaycastHit2D[8];
        static readonly List<Collider2D> overlapBuffer = new List<Collider2D>(16);

        public static ContactFilter2D Filter(int mask, bool includeTriggers)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = includeTriggers;
            return filter;
        }

        /// <summary>
        /// Box cast that returns the closest hit whose collider is not <paramref name="ignore"/>
        /// and whose normal passes <paramref name="minNormalDot"/> against -direction.
        /// </summary>
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, Vector2 direction, float distance, int mask,
            Collider2D ignore = null, float minNormalDot = -1f)
        {
            int count = Physics2D.BoxCast(origin, size, 0f, direction, Filter(mask, false), castBuffer, distance);
            RaycastHit2D best = default;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = castBuffer[i];
                if (hit.collider == null || hit.collider == ignore) continue;
                if (Vector2.Dot(hit.normal, -direction) < minNormalDot) continue;
                if (hit.distance < bestDistance)
                {
                    best = hit;
                    bestDistance = hit.distance;
                }
            }
            return best;
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int mask)
        {
            int count = Physics2D.Raycast(origin, direction, Filter(mask, false), castBuffer, distance);
            RaycastHit2D best = default;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (castBuffer[i].collider != null && castBuffer[i].distance < bestDistance)
                {
                    best = castBuffer[i];
                    bestDistance = best.distance;
                }
            }
            return best;
        }

        /// <summary>Overlap box query. The returned list is shared: copy it if you need to keep it.</summary>
        public static List<Collider2D> OverlapBox(Vector2 center, Vector2 size, int mask, bool includeTriggers = true)
        {
            overlapBuffer.Clear();
            Physics2D.OverlapBox(center, size, 0f, Filter(mask, includeTriggers), overlapBuffer);
            return overlapBuffer;
        }

        /// <summary>Overlap query using a collider's exact shape. The returned list is shared.</summary>
        public static List<Collider2D> OverlapCollider(Collider2D collider, int mask, bool includeTriggers = true)
        {
            overlapBuffer.Clear();
            if (collider == null || !collider.enabled) return overlapBuffer;
            Physics2D.OverlapCollider(collider, Filter(mask, includeTriggers), overlapBuffer);
            return overlapBuffer;
        }

        /// <summary>Overlap circle query. The returned list is shared.</summary>
        public static List<Collider2D> OverlapCircle(Vector2 center, float radius, int mask, bool includeTriggers = true)
        {
            overlapBuffer.Clear();
            Physics2D.OverlapCircle(center, radius, Filter(mask, includeTriggers), overlapBuffer);
            return overlapBuffer;
        }

        public static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var hit = Raycast(from, delta.normalized, delta.magnitude, Layers.SolidMask);
            return hit.collider == null;
        }
    }
}
