using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Base class for things collected by touching them. Persistent pickups are
    /// remembered in the save file and never come back once taken.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public abstract class Pickup : MonoBehaviour
    {
        public float collectRadius = 0.9f;
        [Tooltip("Remember that this was collected (upgrades, keys, abilities).")]
        public bool persistent = true;
        public string collectSound = AudioIds.PickupItem;
        public string collectEffect = EffectIds.PickupSparkle;

        [Header("Idle Bob")]
        [Tooltip("Child that bobs up and down. Leave empty to skip.")]
        public Transform visual;
        public float bobAmplitude = 0.15f;
        public float bobSpeed = 2.5f;

        protected PersistentId pid;
        bool collected;
        Vector3 visualBase;
        float bobTime;

        protected virtual void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null) visualBase = visual.localPosition;
            bobTime = Random.value * 10f;
        }

        protected virtual void Start()
        {
            if (persistent && pid.IsFlagSet()) Destroy(gameObject);
        }

        protected virtual void Update()
        {
            if (visual != null && bobAmplitude > 0f)
            {
                bobTime += Time.deltaTime;
                visual.localPosition = visualBase + Vector3.up * Mathf.Sin(bobTime * bobSpeed) * bobAmplitude;
            }

            var player = PlayerController.Instance;
            if (collected || player == null || player.PlayerHealth.IsDead) return;
            if (Vector2.Distance(player.Center, transform.position) <= collectRadius && CanCollect(player)) Collect(player);
        }

        protected virtual bool CanCollect(PlayerController player)
        {
            var gm = GameManager.Instance;
            return gm == null || gm.State == GameState.Playing;
        }

        void Collect(PlayerController player)
        {
            collected = true;
            if (persistent) pid.SetFlag();
            AudioManager.PlaySfx(collectSound);
            EffectsManager.Spawn(collectEffect, transform.position);
            OnCollected(player);
            Destroy(gameObject);
        }

        protected abstract void OnCollected(PlayerController player);

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);
        }
    }
}
