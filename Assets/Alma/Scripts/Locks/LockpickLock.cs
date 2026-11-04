using UnityEngine;
using UnityEngine.Events;

namespace Alma
{
    /// <summary>
    /// A lock Corvin can pick (door, gate, chest, crypt grille). Interact to start the
    /// lockpicking minigame; on success it opens its Gate, reveals objects and fires onUnlocked.
    /// Breaking a pick makes noise. Some locks need better tools (e.g. Malaspina's picks).
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class LockpickLock : Interactable
    {
        /// <summary>Key item that makes every lock easier.</summary>
        public const string BetterPicksItem = "malaspina_picks";

        [Header("Difficulty")]
        [Range(1, 6)] public int pins = 3;
        [Tooltip("How fast the pick moves. 1 = normal.")]
        [Range(0.4f, 3f)] public float speed = 1f;
        [Tooltip("Width of the sweet spot as a fraction of the track.")]
        [Range(0.05f, 0.5f)] public float sweetSpot = 0.2f;
        [Tooltip("Key item needed to even try this lock (empty = any picks).")]
        public string requiredToolItem = "";
        public string tooHardMessage = "These pins are too fine for your picks.";

        [Header("When opened")]
        public Gate gate;
        public GameObject[] revealOnUnlock = new GameObject[0];
        public UnityEvent onUnlocked = new UnityEvent();
        public float failNoiseRadius = 6f;

        public bool IsUnlocked { get; private set; }

        PersistentId pid;
        const string UnlockedFlag = "unlocked";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (prompt == "Interact") prompt = "Pick lock";
        }

        void Start()
        {
            if (!pid.IsFlagSet(UnlockedFlag)) return;
            IsUnlocked = true;
            if (gate != null) gate.SetOpenImmediate(true);
            Reveal();
        }

        public override bool CanInteract(PlayerController player)
        {
            return base.CanInteract(player) && !IsUnlocked && (player.Profile == null || player.Profile.CanPickLocks);
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredToolItem) && (gm == null || !gm.HasKeyItem(requiredToolItem)))
            {
                AudioManager.PlaySfx(AudioIds.DoorLocked);
                GameEvents.RaiseToast(tooHardMessage);
                return;
            }
            if (UIManager.Instance != null) UIManager.Instance.Lockpick.Begin(this);
        }

        public void Unlock()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;
            pid.SetFlag(UnlockedFlag);
            AudioManager.PlaySfx(AudioIds.LockOpen);
            if (gate != null) gate.Open();
            Reveal();
            onUnlocked.Invoke();
        }

        public void OnPickBroken()
        {
            Noise.Emit(transform.position, failNoiseRadius, gameObject);
        }

        void Reveal()
        {
            foreach (var go in revealOnUnlock)
                if (go != null) go.SetActive(true);
        }
    }
}
