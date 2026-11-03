using System;
using System.IO;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Reads and writes save slots as JSON in Application.persistentDataPath.
    /// Writes go to a temp file first and keep a .bak copy, so a crash mid-save
    /// can't destroy a slot.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Used when you press Play directly inside a level, so testing never touches real saves.</summary>
        public const int DebugSlot = -1;
        public const int SlotCount = 3;

        public static string PathFor(int slot)
        {
            string file = slot == DebugSlot ? "save_debug.json" : $"save_{slot}.json";
            return Path.Combine(Application.persistentDataPath, file);
        }

        public static bool Exists(int slot) => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + ".bak");

        public static bool Save(int slot, SaveData data)
        {
            if (data == null) return false;
            string path = PathFor(slot);
            string temp = path + ".tmp";
            string backup = path + ".bak";
            try
            {
                data.savedAtUtc = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(temp, json);
                if (File.Exists(path))
                {
                    File.Copy(path, backup, true);
                    File.Delete(path);
                }
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save slot {slot}: {e}");
                return false;
            }
        }

        public static SaveData Load(int slot)
        {
            string path = PathFor(slot);
            var data = TryRead(path) ?? TryRead(path + ".bak");
            if (data != null) data.Migrate();
            return data;
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
            {
                try
                {
                    if (File.Exists(p)) File.Delete(p);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] Could not delete {p}: {e.Message}");
                }
            }
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {path}: {e.Message}");
                return null;
            }
        }
    }
}
