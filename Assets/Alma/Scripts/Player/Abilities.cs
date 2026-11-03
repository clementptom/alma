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

    public static class AbilityInfo
    {
        public static string DisplayName(AbilityType ability)
        {
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
            switch (ability)
            {
                case AbilityType.Dash: return "Press DASH to dash forward. Works once in the air.";
                case AbilityType.WallJump: return "Hold toward a wall to slide down it, then press JUMP to leap off.";
                case AbilityType.DoubleJump: return "Press JUMP again in mid-air.";
                case AbilityType.Glide: return "Hold JUMP while falling to glide.";
                default: return "";
            }
        }
    }
}
