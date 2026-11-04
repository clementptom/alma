using UnityEngine;

namespace Alma
{
    /// <summary>Raises maximum energy (more heals per full meter) and refills it.</summary>
    public class EnergyUpgradePickup : Pickup
    {
        public int amount = 33;
        public Sprite icon;

        protected override void Awake()
        {
            base.Awake();
            if (collectSound == AudioIds.PickupItem) collectSound = AudioIds.PickupEnergyUpgrade;
        }

        protected override void OnCollected(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.AddMaxEnergy(amount);
            gm.ShowPopup("Energy Vessel", $"Maximum energy increased by {amount}.", icon);
        }
    }
}
