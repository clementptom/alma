namespace Alma
{
    /// <summary>
    /// Unlockable movement abilities: the main way the world gates progress.
    /// To add one, add an entry here, describe it in AbilityInfo, and use
    /// GameManager.Instance.HasAbility(...) wherever it matters.
    /// </summary>
    public enum AbilityType
    {
        Dash,
        WallJump,
        DoubleJump,
        Glide
    }

    /// <summary>
    /// Names, descriptions and icons for abilities. Values set in Resources/ItemDatabase win;
    /// the built-in text below is the fallback.
    /// </summary>
    public static class AbilityInfo
    {
        public static string DisplayName(AbilityType ability)
        {
            var def = ItemDatabase.Get().FindAbility(ability);
            if (def != null && !string.IsNullOrEmpty(def.displayName)) return def.displayName;
            switch (ability)
            {
                case AbilityType.Dash: return "Dash";
                case AbilityType.WallJump: return "Wall Jump";
                case AbilityType.DoubleJump: return "Double Jump";
                case AbilityType.Glide: return "Glide";
                default: return ability.ToString();
            }
        }

        public static string Description(AbilityType ability)
        {
            var def = ItemDatabase.Get().FindAbility(ability);
            if (def != null && !string.IsNullOrEmpty(def.description)) return def.description;
            switch (ability)
            {
                case AbilityType.Dash: return "Press DODGE in mid-air to dash forward.";
                case AbilityType.WallJump: return "Hold toward a wall to slide down it, then press JUMP to leap off.";
                case AbilityType.DoubleJump: return "Press JUMP again in mid-air.";
                case AbilityType.Glide: return "Hold JUMP while falling to glide.";
                default: return "";
            }
        }

        public static UnityEngine.Sprite Icon(AbilityType ability)
        {
            var def = ItemDatabase.Get().FindAbility(ability);
            return def != null ? def.icon : null;
        }
    }
}
