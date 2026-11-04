using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A wall, floor or object destroyed by attacks. Great for secret passages.
    /// Put it on the "Breakable" layer so it's solid and can be hit.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class Breakable : MonoBehaviour, IDamageable
    {
        public int hitsToBreak = 3;
        [Tooltip("Stay broken forever once broken.")]
        public bool remember = true;
        [Tooltip("Enabled when this breaks (e.g. a hidden pickup).")]
        public GameObject[] revealOnBreak = new GameObject[0];
        public float shakeOnHit = 0.06f;

        int hits;
        PersistentId pid;
        Vector3 basePosition;
        Coroutine wobble;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            basePosition = transform.localPosition;
            int layer = Layers.Index(Layers.Breakable);
            if (layer >= 0) gameObject.layer = layer;
        }

        void Start()
        {
            if (remember && pid.IsFlagSet())
            {
                Reveal();
                Destroy(gameObject);
            }
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (info.type != DamageType.Attack) return false;
            hits++;
            if (hits >= hitsToBreak)
            {
                Break();
            }
            else
            {
                AudioManager.PlaySfx(AudioIds.BreakableHit);
                CameraController.Shake(shakeOnHit, 0.1f);
                if (wobble != null) StopCoroutine(wobble);
                wobble = StartCoroutine(Wobble());
            }
            return true;
        }

        public void Break()
        {
            if (remember) pid.SetFlag();
            AudioManager.PlaySfx(AudioIds.BreakableBreak);
            var col = GetComponent<Collider2D>();
            EffectsManager.Spawn(EffectIds.BreakableDebris, col != null ? (Vector2)col.bounds.center : (Vector2)transform.position);
            CameraController.Shake(0.2f, 0.25f);
            Reveal();
            Destroy(gameObject);
        }

        void Reveal()
        {
            foreach (var go in revealOnBreak)
                if (go != null) go.SetActive(true);
        }

        IEnumerator Wobble()
        {
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                transform.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * 0.06f);
                yield return null;
            }
            transform.localPosition = basePosition;
        }
    }
}
