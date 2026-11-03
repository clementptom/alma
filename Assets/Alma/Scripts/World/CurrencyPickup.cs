using UnityEngine;

namespace Alma
{
    /// <summary>Money / shards / geo. Enemies drop these; they get pulled toward the player.</summary>
    public class CurrencyPickup : Pickup
    {
        public int value = 1;
        public float magnetRadius = 3.5f;
        public float magnetSpeed = 14f;
        public float scatterSpeed = 6f;
        [Tooltip("Dropped coins disappear after this long. 0 = never.")]
        public float despawnTime = 0f;

        Rigidbody2D rb;
        float magnetDelay;
        float age;
        bool magnetized;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            if (collectSound == AudioIds.PickupItem) collectSound = AudioIds.PickupCurrency;
            int layer = Layers.Index(Layers.Pickup);
            if (layer >= 0) gameObject.layer = layer;
        }

        /// <summary>Called when an enemy drops this: flings it out and makes it non-persistent.</summary>
        public void Scatter()
        {
            persistent = false;
            magnetDelay = 0.45f;
            despawnTime = despawnTime <= 0f ? 30f : despawnTime;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(Random.Range(-0.6f, 0.6f) * scatterSpeed, Random.Range(0.7f, 1f) * scatterSpeed);
            }
        }

        protected override void Update()
        {
            base.Update();
            age += Time.deltaTime;
            if (despawnTime > 0f && age > despawnTime)
            {
                Destroy(gameObject);
                return;
            }
            if (magnetDelay > 0f)
            {
                magnetDelay -= Time.deltaTime;
                return;
            }

            var player = PlayerController.Instance;
            if (player == null) return;
            Vector2 toPlayer = player.Center - (Vector2)transform.position;
            if (!magnetized && toPlayer.magnitude < magnetRadius) magnetized = true;
            if (!magnetized) return;

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = toPlayer.normalized * magnetSpeed;
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, player.Center, magnetSpeed * Time.deltaTime);
            }
        }

        protected override void OnCollected(PlayerController player)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCurrency(value);
        }
    }
}
