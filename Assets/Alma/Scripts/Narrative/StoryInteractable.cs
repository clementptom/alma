using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Plays a StorySequence when the player interacts (read the letter, knock on a door,
    /// sit by the fire). Set a holdDuration for "fix the roof" style tasks.
    /// </summary>
    public class StoryInteractable : Interactable
    {
        public StorySequence sequence;
        [Tooltip("Can only be used once this flag is set (empty = always).")]
        public string requiredFlag = "";
        [Tooltip("Hide this interaction after it's been used once.")]
        public bool singleUse = true;
        [Tooltip("Optional flag set when used (e.g. roof_1). Lets other sequences wait for it.")]
        public string setsFlag = "";

        public override bool CanInteract(PlayerController player)
        {
            if (!base.CanInteract(player)) return false;
            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredFlag) && (gm == null || !gm.HasFlag(requiredFlag))) return false;
            if (singleUse && !string.IsNullOrEmpty(setsFlag) && gm != null && gm.HasFlag(setsFlag)) return false;
            if (singleUse && sequence != null && sequence.HasPlayed) return false;
            return true;
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm != null && !string.IsNullOrEmpty(setsFlag)) gm.SetFlag(setsFlag);
            if (sequence != null) sequence.Play();
        }
    }
}
