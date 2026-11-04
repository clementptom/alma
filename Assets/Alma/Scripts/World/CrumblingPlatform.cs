using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A platform that shakes and falls apart shortly after the player stands on it, then
    /// comes back after a few seconds. Put it on the Ground layer (or make it a OneWayPlatform).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CrumblingPlatform : MonoBehaviour
    {
        public float crumbleDelay = 0.6f;
        public float respawnTime = 3f;
        public float shakeAmount = 0.06f;
        [Tooltip("Child that shakes. Defaults to the child named Visual.")]
        public Transform visual;

        Collider2D[] colliders;
        Renderer[] renderers;
        Vector3 visualBase;
        bool busy;

        void Awake()
        {
            colliders = GetComponents<Collider2D>();
            renderers = GetComponentsInChildren<Renderer>(true);
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null) visualBase = visual.localPosition;
        }

        void FixedUpdate()
        {
            if (busy) return;
            var player = PlayerController.Instance;
            if (player == null || player.GroundCollider == null) return;
            if (player.GroundCollider.transform == transform || player.GroundCollider.transform.IsChildOf(transform))
                StartCoroutine(Crumble());
        }

        IEnumerator Crumble()
        {
            busy = true;
            AudioManager.PlaySfx(AudioIds.PlatformCrumble);

            float t = 0f;
            while (t < crumbleDelay)
            {
                t += Time.deltaTime;
                if (visual != null) visual.localPosition = visualBase + (Vector3)(Random.insideUnitCircle * shakeAmount);
                yield return null;
            }
            if (visual != null) visual.localPosition = visualBase;

            var bounds = colliders.Length > 0 ? colliders[0].bounds : new Bounds(transform.position, Vector3.one);
            EffectsManager.Spawn(EffectIds.BreakableDebris, bounds.center);
            SetSolid(false);

            yield return new WaitForSeconds(respawnTime);

            // Don't reappear inside the player.
            while (PlayerOverlaps(bounds)) yield return new WaitForSeconds(0.2f);
            SetSolid(true);
            busy = false;
        }

        static bool PlayerOverlaps(Bounds bounds)
        {
            var player = PlayerController.Instance;
            return player != null && player.Collider != null && player.Collider.bounds.Intersects(bounds);
        }

        void SetSolid(bool solid)
        {
            foreach (var c in colliders) c.enabled = solid;
            foreach (var r in renderers) r.enabled = solid;
        }
    }
}
