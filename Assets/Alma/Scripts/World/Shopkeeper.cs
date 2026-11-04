using System;
using UnityEngine;

namespace Alma
{
    public enum ShopItemType
    {
        KeyItem,
        MaxHealth,
        MaxEnergy,
        Ability
    }

    [Serializable]
    public class ShopEntry
    {
        [Tooltip("Leave empty to use the name from the Item Database (key items) or a default.")]
        public string displayName = "";
        [TextArea(2, 4)] public string description = "";
        public Sprite icon;
        public ShopItemType type = ShopItemType.KeyItem;
        [Tooltip("Key items: the id from Resources/ItemDatabase.")]
        public string itemId = "";
        [Tooltip("Abilities: which one is sold.")]
        public AbilityType ability = AbilityType.Dash;
        [Tooltip("Max Health / Max Energy: how much is added.")]
        public int amount = 1;
        public int price = 50;
        [Tooltip("Can only be bought once (remembered in the save).")]
        public bool unique = true;
    }

    /// <summary>
    /// A merchant. Talking to them plays an optional greeting, then opens the shop, where
    /// currency buys key items, health and energy upgrades, or abilities.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class Shopkeeper : Interactable
    {
        public string shopName = "Merchant";
        public ShopEntry[] stock = new ShopEntry[0];

        [Header("Greeting")]
        public DialogueLine[] greeting = new DialogueLine[0];
        [Tooltip("Play the greeting every visit, not just the first.")]
        public bool greetEveryVisit = false;

        [Header("Visuals")]
        public Transform visual;
        public Animator animator;

        PersistentId pid;
        CharacterAnimator anim;

        const string GreetedFlag = "greeted";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (visual == null) visual = transform.Find("Visual");
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
            if (prompt == "Interact") prompt = "Shop";
        }

        void Start()
        {
            anim.Play(AnimStates.Idle);
        }

        public override void Interact(PlayerController player)
        {
            if (visual != null)
            {
                Vector3 s = visual.localScale;
                s.x = Mathf.Abs(s.x) * (player.Center.x >= transform.position.x ? 1f : -1f);
                visual.localScale = s;
            }
            player.SetFacing(transform.position.x >= player.Center.x ? 1 : -1);

            bool greet = greeting != null && greeting.Length > 0 && (greetEveryVisit || !pid.IsFlagSet(GreetedFlag));
            if (greet)
            {
                DialogueRunner.Play(greeting, shopName, () =>
                {
                    pid.SetFlag(GreetedFlag);
                    OpenShop();
                });
            }
            else
            {
                OpenShop();
            }
        }

        void OpenShop()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;
            ui.Shop.SetShop(this);
            ui.Shop.Open();
        }

        // ------------------------------------------------------------- Stock

        public string EntryName(int index)
        {
            var e = stock[index];
            if (!string.IsNullOrEmpty(e.displayName)) return e.displayName;
            switch (e.type)
            {
                case ShopItemType.KeyItem: return ItemDatabase.ItemName(e.itemId);
                case ShopItemType.MaxHealth: return "Vitality Shard";
                case ShopItemType.MaxEnergy: return "Energy Vessel";
                case ShopItemType.Ability: return AbilityInfo.DisplayName(e.ability);
                default: return "Item";
            }
        }

        public string EntryDescription(int index)
        {
            var e = stock[index];
            if (!string.IsNullOrEmpty(e.description)) return e.description;
            switch (e.type)
            {
                case ShopItemType.KeyItem: return ItemDatabase.ItemDescription(e.itemId);
                case ShopItemType.MaxHealth: return $"Raises maximum health by {e.amount}.";
                case ShopItemType.MaxEnergy: return $"Raises maximum energy by {e.amount}.";
                case ShopItemType.Ability: return AbilityInfo.Description(e.ability);
                default: return "";
            }
        }

        public Sprite EntryIcon(int index)
        {
            var e = stock[index];
            if (e.icon != null) return e.icon;
            if (e.type == ShopItemType.KeyItem) return ItemDatabase.ItemIcon(e.itemId);
            if (e.type == ShopItemType.Ability) return AbilityInfo.Icon(e.ability);
            return null;
        }

        string SoldKey(int index)
        {
            var e = stock[index];
            switch (e.type)
            {
                case ShopItemType.KeyItem: return "sold:" + e.itemId;
                case ShopItemType.Ability: return "sold:ability:" + e.ability;
                default: return "sold:" + index;
            }
        }

        public bool IsSoldOut(int index)
        {
            var e = stock[index];
            var gm = GameManager.Instance;
            if (e.type == ShopItemType.Ability && gm != null && gm.HasAbility(e.ability)) return true;
            if (e.type == ShopItemType.KeyItem && e.unique && gm != null && gm.HasKeyItem(e.itemId)) return true;
            return e.unique && pid.IsFlagSet(SoldKey(index));
        }

        /// <summary>Tries to buy an entry. The message explains the result for the shop screen.</summary>
        public bool TryBuy(int index, out string message)
        {
            var gm = GameManager.Instance;
            if (gm == null || index < 0 || index >= stock.Length)
            {
                message = "";
                return false;
            }
            if (IsSoldOut(index))
            {
                message = "Sold out.";
                return false;
            }
            var e = stock[index];
            if (!gm.TrySpendCurrency(e.price))
            {
                message = "Not enough currency.";
                return false;
            }

            switch (e.type)
            {
                case ShopItemType.KeyItem: gm.AddKeyItem(e.itemId); break;
                case ShopItemType.MaxHealth: gm.AddMaxHealth(e.amount); break;
                case ShopItemType.MaxEnergy: gm.AddMaxEnergy(e.amount); break;
                case ShopItemType.Ability: gm.UnlockAbility(e.ability, showPopup: false); break;
            }
            if (e.unique) pid.SetFlag(SoldKey(index));
            message = "Bought " + EntryName(index) + ".";
            return true;
        }
    }
}
