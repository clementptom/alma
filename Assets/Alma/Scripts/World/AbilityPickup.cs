using UnityEngine;

namespace Alma
{
    /// <summary>Grants a movement ability and shows the "new ability" popup.</summary>
    public class AbilityPickup : Pickup
    {
        public AbilityType ability = AbilityType.Dash;
        [Tooltip("Leave empty to use the default name from AbilityInfo.")]
        public string titleOverride = "";
        [TextArea] public string descriptionOverride = "";
        public Sprite icon;

        protected override void Awake()
        {
            base.Awake();
            if (collectSound == AudioIds.PickupItem) collectSound = AudioIds.PickupAbility;
            if (collectEffect == EffectIds.PickupSparkle) collectEffect = EffectIds.AbilityGet;
        }

        protected override void Start()
        {
            base.Start();
            // Abilities are remembered by the ability itself too (e.g. unlocked by a cheat).
            var gm = GameManager.Instance;
            if (gm != null && gm.HasAbility(ability) && persistent) Destroy(gameObject);
        }

        protected override void OnCollected(PlayerController player)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            string title = string.IsNullOrEmpty(titleOverride) ? AbilityInfo.DisplayName(ability) : titleOverride;
            string description = string.IsNullOrEmpty(descriptionOverride) ? AbilityInfo.Description(ability) : descriptionOverride;
            gm.UnlockAbility(ability, title, description, icon);
        }
    }
}
