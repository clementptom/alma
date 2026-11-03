using System;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Global event hub. Gameplay code raises events here and the UI and audio listen,
    /// so the systems don't need references to each other.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>(current, max)</summary>
        public static event Action<int, int> PlayerHealthChanged;
        public static event Action<int> CurrencyChanged;
        public static event Action<AbilityType> AbilityUnlocked;
        public static event Action<string> KeyItemAdded;
        public static event Action<string> FlagSet;
        public static event Action<Room> RoomEntered;
        public static event Action PlayerDied;
        public static event Action GameSaved;
        /// <summary>Short message shown in the corner of the HUD.</summary>
        public static event Action<string> Toast;
        public static event Action<Boss> BossStarted;
        public static event Action<Boss> BossDefeated;
        public static event Action<GameState> StateChanged;

        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaiseCurrencyChanged(int amount) => CurrencyChanged?.Invoke(amount);
        public static void RaiseAbilityUnlocked(AbilityType ability) => AbilityUnlocked?.Invoke(ability);
        public static void RaiseKeyItemAdded(string item) => KeyItemAdded?.Invoke(item);
        public static void RaiseFlagSet(string flag) => FlagSet?.Invoke(flag);
        public static void RaiseRoomEntered(Room room) => RoomEntered?.Invoke(room);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseGameSaved() => GameSaved?.Invoke();
        public static void RaiseToast(string message) => Toast?.Invoke(message);
        public static void RaiseBossStarted(Boss boss) => BossStarted?.Invoke(boss);
        public static void RaiseBossDefeated(Boss boss) => BossDefeated?.Invoke(boss);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);

        /// <summary>Clears all listeners so nothing leaks between play sessions when domain reload is off.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PlayerHealthChanged = null;
            CurrencyChanged = null;
            AbilityUnlocked = null;
            KeyItemAdded = null;
            FlagSet = null;
            RoomEntered = null;
            PlayerDied = null;
            GameSaved = null;
            Toast = null;
            BossStarted = null;
            BossDefeated = null;
            StateChanged = null;
        }
    }
}
