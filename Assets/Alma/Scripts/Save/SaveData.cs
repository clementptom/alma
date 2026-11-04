using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>A room the player has visited, stored so the map can draw it.</summary>
    [Serializable]
    public class RoomRecord
    {
        public string id;
        public string scene;
        public string displayName;
        public float x, y, width, height;

        public Rect Rect => new Rect(x, y, width, height);
    }

    /// <summary>A point of interest shown on the map: save stations and travel points.</summary>
    [Serializable]
    public class MapMarkerRecord
    {
        public const string SaveStation = "save";
        public const string Travel = "travel";

        public string id;
        public string kind;
        public string scene;
        public string label;
        public float x, y;

        public Vector2 Position => new Vector2(x, y);
    }

    /// <summary>
    /// All progress for one save slot. Serialized to JSON with JsonUtility, so it only
    /// uses fields, lists and serializable classes (no dictionaries or hash sets).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAtUtc = "";
        public float playTimeSeconds;

        [Header("Where the player respawns")]
        public string respawnScene = "";
        public string respawnPointId = "";
        [Tooltip("Name of the room where the game was last saved (shown on the save slot).")]
        public string locationName = "";

        [Header("Player")]
        public int maxHealth = 5;
        public int maxEnergy = 99;
        public int energy;
        public int currency;
        public List<string> abilities = new List<string>();
        public List<string> keyItems = new List<string>();

        [Header("World state")]
        /// <summary>Generic flags: collected pickups, opened gates, defeated bosses, seen cutscenes...</summary>
        public List<string> flags = new List<string>();
        public List<RoomRecord> rooms = new List<RoomRecord>();
        public List<MapMarkerRecord> markers = new List<MapMarkerRecord>();

        [NonSerialized] HashSet<string> flagLookup;

        public bool HasFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            EnsureLookup();
            return flagLookup.Contains(flag);
        }

        /// <summary>Returns true if the flag was newly added.</summary>
        public bool AddFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            EnsureLookup();
            if (!flagLookup.Add(flag)) return false;
            flags.Add(flag);
            return true;
        }

        public void RemoveFlag(string flag)
        {
            EnsureLookup();
            if (flagLookup.Remove(flag)) flags.Remove(flag);
        }

        public bool HasAbility(AbilityType ability) => abilities.Contains(ability.ToString());

        public bool AddAbility(AbilityType ability)
        {
            string key = ability.ToString();
            if (abilities.Contains(key)) return false;
            abilities.Add(key);
            return true;
        }

        public bool HasKeyItem(string item) => !string.IsNullOrEmpty(item) && keyItems.Contains(item);

        public RoomRecord FindRoom(string roomId) => rooms.Find(r => r.id == roomId);

        public MapMarkerRecord FindMarker(string markerId) => markers.Find(m => m.id == markerId);

        void EnsureLookup()
        {
            if (flagLookup == null) flagLookup = new HashSet<string>(flags);
        }

        /// <summary>Upgrades old save files when the format changes. Bump CurrentVersion when you add migrations.</summary>
        public void Migrate()
        {
            if (abilities == null) abilities = new List<string>();
            if (keyItems == null) keyItems = new List<string>();
            if (flags == null) flags = new List<string>();
            if (rooms == null) rooms = new List<RoomRecord>();
            if (markers == null) markers = new List<MapMarkerRecord>();
            if (locationName == null) locationName = "";
            flagLookup = null;
            version = CurrentVersion;
        }
    }
}
