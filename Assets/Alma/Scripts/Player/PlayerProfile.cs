using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Applies a CharacterProfile to the player: animations, placeholder colour and which
    /// abilities are available. The GameManager calls Apply() whenever a scene loads.
    /// </summary>
    public class PlayerProfile : MonoBehaviour
    {
        public CharacterProfile Current { get; private set; }

        public bool CanAttack => P.canAttack;
        public bool CanParry => P.canParry;
        public bool CanDodge => P.canDodge;
        public bool CanHeal => P.canHeal;
        public bool CanPickLocks => P.canPickLocks;
        public bool CanThrowStones => P.canThrowStones;
        public bool HasCompanion => P.hasCompanion;
        public float SpeedMultiplier => Mathf.Max(0.1f, P.speedMultiplier);

        CharacterProfile P => Current != null ? Current : CharacterProfile.Default;

        RuntimeAnimatorController originalAnimator;
        bool capturedOriginal;

        public void Apply(CharacterProfile profile)
        {
            Current = profile != null ? profile : CharacterProfile.Default;

            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                if (!capturedOriginal)
                {
                    originalAnimator = animator.runtimeAnimatorController;
                    capturedOriginal = true;
                }
                animator.runtimeAnimatorController = Current.animator != null ? Current.animator : originalAnimator;
            }

            // Tint the grey placeholder body so the two characters look different before art exists.
            var body = transform.Find("Visual/Body");
            if (body != null)
            {
                var sr = body.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.name.StartsWith("Square")) sr.color = Current.placeholderColor;
                else if (sr != null && sr.sprite != null && sr.sprite.name.StartsWith("Placeholder")) sr.color = Current.placeholderColor;
            }

            var energy = GetComponent<PlayerEnergy>();
            if (energy != null && !Current.canHeal) energy.StopHealing();
            GameEvents.RaiseProfileChanged(Current);
        }
    }
}
