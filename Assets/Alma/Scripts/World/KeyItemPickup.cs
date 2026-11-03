using UnityEngine;

namespace Alma
{
    /// <summary>A key or quest item. KeyDoor checks for these by itemId.</summary>
    public class KeyItemPickup : Pickup
    {
        public string itemId = "key_rusty";
        public string displayName = "Rusty Key";
        [TextArea] public string description = "";
        public Sprite icon;
        public bool showPopup = false;

        protected override void OnCollected(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.AddKeyItem(itemId);
            if (showPopup) gm.ShowPopup(displayName, description, icon);
            else GameEvents.RaiseToast("Obtained: " + displayName);
        }
    }
}
