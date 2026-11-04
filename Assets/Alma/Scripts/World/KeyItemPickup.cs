using UnityEngine;

namespace Alma
{
    /// <summary>A key or quest item. KeyDoor checks for these by itemId.</summary>
    public class KeyItemPickup : Pickup
    {
        [Tooltip("Id from Resources/ItemDatabase. Its name, description and icon are used when the fields below are empty.")]
        public string itemId = "key_rusty";
        public string displayName = "";
        [TextArea] public string description = "";
        public Sprite icon;
        public bool showPopup = false;

        protected override void OnCollected(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.AddKeyItem(itemId);
            string name = string.IsNullOrEmpty(displayName) ? ItemDatabase.ItemName(itemId) : displayName;
            string body = string.IsNullOrEmpty(description) ? ItemDatabase.ItemDescription(itemId) : description;
            Sprite sprite = icon != null ? icon : ItemDatabase.ItemIcon(itemId);
            if (showPopup) gm.ShowPopup(name, body, sprite);
            else GameEvents.RaiseToast("Obtained: " + name);
        }
    }
}
