using UnityEngine;

namespace Alma
{
    /// <summary>A thrown stone: flies in an arc and makes a noise on its first impact.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ThrownStone : MonoBehaviour
    {
        public float lifetime = 4f;

        Rigidbody2D rb;
        float noiseRadius = 7.5f;
        GameObject thrower;
        bool landed;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            int layer = Layers.Index(Layers.Pickup);
            if (layer >= 0) gameObject.layer = layer; // collides with terrain only
        }

        public void Launch(Vector2 velocity, float radius, GameObject from)
        {
            noiseRadius = radius;
            thrower = from;
            rb.linearVelocity = velocity;
            Destroy(gameObject, lifetime);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (landed) return;
            landed = true;
            Vector2 at = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            Noise.Emit(at, noiseRadius, thrower);
            AudioManager.PlaySfx(AudioIds.StoneImpact);
            EffectsManager.Spawn(EffectIds.StoneImpact, at);
            Destroy(gameObject, 1.5f);
        }

        /// <summary>A placeholder stone made in code (used when no prefab is assigned).</summary>
        public static ThrownStone CreateDefault(Vector2 position)
        {
            var go = new GameObject("Stone");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Placeholders.Circle;
            sr.color = new Color(0.6f, 0.6f, 0.62f);
            sr.sortingOrder = 25;
            go.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.5f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            return go.AddComponent<ThrownStone>();
        }
    }
}
