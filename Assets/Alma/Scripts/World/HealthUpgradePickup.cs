using UnityEngine;

namespace Alma
{
    /// <summary>Raises maximum health by one (or more) and refills it.</summary>
    public class HealthUpgradePickup : Pickup
    {
        public int amount = 1;
        public Sprite icon;

        protected override void Awake()
        {
            base.Awake();
            if (collectSound == AudioIds.PickupItem) collectSound = AudioIds.PickupHealthUpgrade;
        }

        protected override void OnCollected(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.AddMaxHealth(amount);
            gm.ShowPopup("Vitality Up", $"Maximum health increased by {amount}.", icon);
        }
    }
}
