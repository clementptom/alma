using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Checkpoint (bench, shrine, campfire...). Resting here heals the player fully,
    /// saves the game, and makes this the respawn point after death.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class SaveStation : Interactable, ISpawnLocation
    {
        public Vector2 spawnOffset = Vector2.zero;
        public Animator animator;

        PersistentId pid;
        CharacterAnimator anim;

        public string SpawnId => pid != null ? pid.Id : GetComponent<PersistentId>().Id;
        public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            if (prompt == "Interact") prompt = "Rest";
        }

        void Start()
        {
            anim.Play(AnimStates.Idle);
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            player.PlayerHealth.Health.HealFull();
            gm.SaveAtStation(this);
            AudioManager.PlaySfx(AudioIds.SaveStation);
            AudioManager.PlaySfx(AudioIds.PlayerHeal);
            EffectsManager.Spawn(EffectIds.SaveStation, transform.position);
            anim.Play(AnimStates.Active, true);
            GameEvents.RaiseToast("Game saved");
        }

        public void OnPlayerArrived(PlayerController player)
        {
            anim.Play(AnimStates.Active);
        }
    }
}
