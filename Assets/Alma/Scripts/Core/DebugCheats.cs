using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alma
{
    /// <summary>
    /// Testing shortcuts, active in the editor and development builds only.
    ///   F1  unlock every ability
    ///   F2  refill health
    ///   F3  +100 currency
    ///   F4  toggle god mode
    ///   F5  save now
    ///   F6  delete the debug save slot
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        void Update()
        {
            var keyboard = Keyboard.current;
            var gm = GameManager.Instance;
            if (keyboard == null || gm == null || gm.Data == null) return;
            var player = PlayerController.Instance;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                foreach (AbilityType a in Enum.GetValues(typeof(AbilityType))) gm.UnlockAbility(a, showPopup: false);
                GameEvents.RaiseToast("[Cheat] All abilities unlocked");
            }
            if (keyboard.f2Key.wasPressedThisFrame && player != null)
            {
                player.PlayerHealth.Health.HealFull();
                GameEvents.RaiseToast("[Cheat] Healed");
            }
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                gm.AddCurrency(100);
            }
            if (keyboard.f4Key.wasPressedThisFrame && player != null)
            {
                var h = player.PlayerHealth.Health;
                h.GodMode = !h.GodMode;
                GameEvents.RaiseToast("[Cheat] God mode " + (h.GodMode ? "ON" : "OFF"));
            }
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                gm.Save();
                GameEvents.RaiseToast("[Cheat] Saved");
            }
            if (keyboard.f6Key.wasPressedThisFrame)
            {
                SaveSystem.Delete(SaveSystem.DebugSlot);
                GameEvents.RaiseToast("[Cheat] Debug save deleted");
            }
        }
    }
}
