using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Physics layer names used by the game. The editor creates these
    /// automatically (see Editor/AlmaProjectSetup.cs).
    /// </summary>
    public static class Layers
    {
        public const string Ground = "Ground";
        public const string OneWayPlatform = "OneWayPlatform";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Breakable = "Breakable";
        public const string Pickup = "Pickup";
        public const string Companion = "Companion";

        public static readonly string[] All = { Ground, OneWayPlatform, Player, Enemy, Breakable, Pickup, Companion };

        /// <summary>Anything you can stand on.</summary>
        public static int GroundMask => LayerMask.GetMask(Ground, OneWayPlatform, Breakable);

        /// <summary>Solid walls (you can't wall-slide on one-way platforms).</summary>
        public static int SolidMask => LayerMask.GetMask(Ground, Breakable);

        public static int PlayerMask => LayerMask.GetMask(Player);

        public static int EnemyMask => LayerMask.GetMask(Enemy);

        public static int CompanionMask => LayerMask.GetMask(Companion);

        /// <summary>What the player's attack can hit: everything except terrain and the player.</summary>
        public static int PlayerAttackMask => ~LayerMask.GetMask(Player, Ground, OneWayPlatform, Pickup, Companion);

        public static int Index(string layerName) => LayerMask.NameToLayer(layerName);

        public static bool AllLayersExist()
        {
            foreach (var name in All)
            {
                if (LayerMask.NameToLayer(name) < 0) return false;
            }
            return true;
        }

        /// <summary>Configures which layers physically collide. Called once at startup.</summary>
        public static void ConfigureCollisionMatrix()
        {
            int player = Index(Player);
            int enemy = Index(Enemy);
            int pickup = Index(Pickup);
            int companion = Index(Companion);
            if (player < 0 || enemy < 0 || pickup < 0) return;

            // The player walks through enemies; damage comes from ContactDamage queries instead.
            Physics2D.IgnoreLayerCollision(player, enemy, true);
            Physics2D.IgnoreLayerCollision(enemy, enemy, true);
            // Pickups only collide with terrain.
            Physics2D.IgnoreLayerCollision(pickup, player, true);
            Physics2D.IgnoreLayerCollision(pickup, enemy, true);
            Physics2D.IgnoreLayerCollision(pickup, pickup, true);
            // Barro walks through the player, enemies and pickups.
            if (companion >= 0)
            {
                Physics2D.IgnoreLayerCollision(companion, player, true);
                Physics2D.IgnoreLayerCollision(companion, enemy, true);
                Physics2D.IgnoreLayerCollision(companion, pickup, true);
                Physics2D.IgnoreLayerCollision(companion, companion, true);
            }
        }

        public static void SetLayerRecursively(GameObject go, string layerName)
        {
            int layer = Index(layerName);
            if (layer < 0) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }
    }
}
