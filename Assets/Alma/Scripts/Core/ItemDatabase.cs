using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    public enum ItemCategory
    {
        KeyItem,
        Collectible
    }

    [Serializable]
    public class ItemDefinition
    {
        [Tooltip("Referenced by KeyItemPickup, KeyDoor and shops, e.g. \"key_arena\".")]
        public string id = "";
        public string displayName = "";
        [TextArea(2, 4)] public string description = "";
        public Sprite icon;
        public ItemCategory category = ItemCategory.KeyItem;
    }

    [Serializable]
    public class AbilityDefinition
    {
        public AbilityType ability;
        [Tooltip("Leave empty to use the built-in name.")]
        public string displayName = "";
        [TextArea(2, 4)] public string description = "";
        public Sprite icon;
    }

    /// <summary>
    /// Names, descriptions and icons for items and abilities, shown in the inventory, shops
    /// and popups. Lives at Resources/ItemDatabase.asset. Anything missing falls back to a
    /// readable default, so you only fill this in when the writing and art exist.
    /// </summary>
    [CreateAssetMenu(menuName = "Alma/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();
        public List<AbilityDefinition> abilities = new List<AbilityDefinition>();

        static ItemDatabase cached;

        public static ItemDatabase Get()
        {
            if (cached == null)
            {
                cached = Resources.Load<ItemDatabase>("ItemDatabase");
                if (cached == null) cached = CreateInstance<ItemDatabase>();
            }
            return cached;
        }

        public ItemDefinition FindItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return items.Find(i => i != null && i.id == id);
        }

        public AbilityDefinition FindAbility(AbilityType ability)
        {
            return abilities.Find(a => a != null && a.ability == ability);
        }

        public static string ItemName(string id)
        {
            var def = Get().FindItem(id);
            return def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : Prettify(id);
        }

        public static string ItemDescription(string id)
        {
            var def = Get().FindItem(id);
            return def != null ? def.description : "";
        }

        public static Sprite ItemIcon(string id)
        {
            var def = Get().FindItem(id);
            return def != null ? def.icon : null;
        }

        /// <summary>"key_arena" becomes "Key Arena".</summary>
        static string Prettify(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var words = id.Replace('-', '_').Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cached = null;
    }
}
