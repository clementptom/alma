namespace Alma
{
    /// <summary>
    /// Every sound and music ID the code plays. The AudioLibrary asset gets one entry per
    /// constant (Alma > Create Data Assets fills it in), and you drag your clips into it.
    /// These IDs match docs/ASSET_LIST.md.
    /// </summary>
    public static class AudioIds
    {
        // ---------------- Player ----------------
        public const string PlayerFootstep = "player_footstep";
        public const string PlayerJump = "player_jump";
        public const string PlayerDoubleJump = "player_double_jump";
        public const string PlayerLand = "player_land";
        public const string PlayerWallJump = "player_wall_jump";
        public const string PlayerDash = "player_dash";
        public const string PlayerAttack = "player_attack";
        public const string PlayerAttackHit = "player_attack_hit";
        public const string PlayerHurt = "player_hurt";
        public const string PlayerDeath = "player_death";
        public const string PlayerHeal = "player_heal";
        public const string PlayerHealCharge = "player_heal_charge";

        // ---------------- Enemies ----------------
        public const string EnemyHurt = "enemy_hurt";
        public const string EnemyDeath = "enemy_death";
        public const string EnemyAlert = "enemy_alert";
        public const string EnemyShoot = "enemy_shoot";
        public const string ProjectileHit = "projectile_hit";

        // ---------------- Boss ----------------
        public const string BossRoar = "boss_roar";
        public const string BossCharge = "boss_charge";
        public const string BossSlam = "boss_slam";
        public const string BossHurt = "boss_hurt";
        public const string BossDeath = "boss_death";
        public const string JingleVictory = "jingle_victory";

        // ---------------- World ----------------
        public const string PickupCurrency = "pickup_currency";
        public const string PickupItem = "pickup_item";
        public const string PickupAbility = "pickup_ability";
        public const string PickupHealthUpgrade = "pickup_health_upgrade";
        public const string PickupEnergyUpgrade = "pickup_energy_upgrade";
        public const string SaveStation = "save_station";
        public const string GateOpen = "gate_open";
        public const string GateClose = "gate_close";
        public const string Lever = "lever";
        public const string BreakableHit = "breakable_hit";
        public const string BreakableBreak = "breakable_break";
        public const string DoorLocked = "door_locked";
        public const string ShopBuy = "shop_buy";
        public const string TravelActivate = "travel_activate";
        public const string TravelDepart = "travel_depart";
        public const string PlatformCrumble = "platform_crumble";
        public const string BouncePad = "bounce_pad";

        // ---------------- UI ----------------
        public const string UiMove = "ui_move";
        public const string UiConfirm = "ui_confirm";
        public const string UiBack = "ui_back";
        public const string UiPause = "ui_pause";
        public const string UiMapOpen = "ui_map_open";
        public const string UiDialogueBlip = "ui_dialogue_blip";
        public const string UiError = "ui_error";

        // ---------------- Music (looping) ----------------
        public const string MusicTitle = "music_title";
        public const string MusicArea1 = "music_area1";
        public const string MusicArea2 = "music_area2";
        public const string MusicBoss = "music_boss";
        public const string MusicCredits = "music_credits";

        // ---------------- Ambience (looping) ----------------
        public const string AmbienceArea1 = "amb_area1";
        public const string AmbienceArea2 = "amb_area2";
    }
}
