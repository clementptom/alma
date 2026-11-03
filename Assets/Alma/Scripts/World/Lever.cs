using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Opens one or more gates. Pull it with INTERACT or hit it with an attack.
    /// It stays pulled once used.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class Lever : Interactable, IDamageable
    {
        public Gate[] targets = new Gate[0];
        [Tooltip("Optional save flag to set when pulled (for things in other scenes).")]
        public string flagToSet = "";
        [Tooltip("Rotated when pulled. Optional.")]
        public Transform handle;
        public float pulledAngle = -45f;
        public Animator animator;

        public bool IsPulled { get; private set; }

        PersistentId pid;
        CharacterAnimator anim;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            if (prompt == "Interact") prompt = "Pull";
        }

        void Start()
        {
            if (pid.IsFlagSet())
            {
                IsPulled = true;
                ApplyVisual();
            }
        }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !IsPulled;

        public override void Interact(PlayerController player) => Pull();

        public bool TakeDamage(DamageInfo info)
        {
            if (info.type != DamageType.Attack || IsPulled) return false;
            Pull();
            return true;
        }

        public void Pull()
        {
            if (IsPulled) return;
            IsPulled = true;
            pid.SetFlag();
            if (!string.IsNullOrEmpty(flagToSet) && GameManager.Instance != null) GameManager.Instance.SetFlag(flagToSet);
            AudioManager.PlaySfx(AudioIds.Lever);
            foreach (var gate in targets)
                if (gate != null) gate.Open();
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(0f, 0f, pulledAngle);
            anim.Play(IsPulled ? AnimStates.Active : AnimStates.Idle);
        }
    }
}
