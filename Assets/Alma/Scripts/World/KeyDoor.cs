using UnityEngine;

namespace Alma
{
    /// <summary>A gate that opens with INTERACT if the player carries the right key item.</summary>
    [RequireComponent(typeof(Gate))]
    public class KeyDoor : Interactable
    {
        public string requiredItemId = "key_rusty";
        public string lockedMessage = "It's locked.";

        Gate gate;

        void Awake()
        {
            gate = GetComponent<Gate>();
            if (prompt == "Interact") prompt = "Open";
        }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !gate.IsOpen;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.HasKeyItem(requiredItemId))
            {
                gate.Open();
            }
            else
            {
                AudioManager.PlaySfx(AudioIds.DoorLocked);
                GameEvents.RaiseToast(lockedMessage);
            }
        }
    }
}
