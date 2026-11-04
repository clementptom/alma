using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A travel shrine (stag station, waystone, tram stop...). Walking near it for the first
    /// time unlocks it; interacting opens a list of every unlocked point to travel to,
    /// including ones in other scenes.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class FastTravelPoint : Interactable, ISpawnLocation
    {
        [Tooltip("Name shown in the travel list.")]
        public string pointName = "Travel Point";
        public Vector2 spawnOffset = new Vector2(1.2f, 0f);
        [Tooltip("The point unlocks when the player comes this close.")]
        public float unlockRadius = 3f;
        public Animator animator;

        PersistentId pid;
        CharacterAnimator anim;
        bool unlocked;

        public string SpawnId => (pid != null ? pid : GetComponent<PersistentId>()).Id;
        public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;
        public bool IsUnlocked => unlocked;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            if (prompt == "Interact") prompt = "Travel";
        }

        void Start()
        {
            var gm = GameManager.Instance;
            unlocked = gm != null && gm.Data != null && gm.Data.FindMarker(SpawnId) != null;
            anim.Play(unlocked ? AnimStates.Active : AnimStates.Idle);
        }

        void Update()
        {
            if (unlocked) return;
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (Vector2.Distance(player.Center, transform.position) <= unlockRadius) Unlock();
        }

        void Unlock()
        {
            if (unlocked) return;
            unlocked = true;
            var gm = GameManager.Instance;
            if (gm != null) gm.RegisterTravelPoint(this);
            AudioManager.PlaySfx(AudioIds.TravelActivate);
            EffectsManager.Spawn(EffectIds.Travel, transform.position);
            anim.Play(AnimStates.Active, true);
            GameEvents.RaiseToast("Travel point found: " + pointName);
        }

        public override void Interact(PlayerController player)
        {
            if (!unlocked) Unlock();
            var ui = UIManager.Instance;
            if (ui == null) return;
            ui.FastTravel.SetOrigin(SpawnId);
            ui.FastTravel.Open();
        }

        public void OnPlayerArrived(PlayerController player)
        {
            anim.Play(AnimStates.Active, true);
            EffectsManager.Spawn(EffectIds.Travel, SpawnPosition);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, unlockRadius);
        }
    }
}
