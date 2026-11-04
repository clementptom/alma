using System;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Sound that enemies can hear. Running footsteps, landings, rolls, fights, Barro's bark,
    /// failed lockpicks and thrown stones all call Noise.Emit(position, radius).
    /// Every StealthPerception within the radius turns to investigate.
    /// </summary>
    public static class Noise
    {
        /// <summary>(position, radius) for debug drawing and effects.</summary>
        public static event Action<Vector2, float> Emitted;

        public static void Emit(Vector2 position, float radius, GameObject source = null)
        {
            if (radius <= 0f) return;
            var listeners = StealthPerception.Active;
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var listener = listeners[i];
                if (listener == null) continue;
                if (source != null && listener.gameObject == source) continue;
                listener.HearNoise(position, radius);
            }
            Emitted?.Invoke(position, radius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Emitted = null;
    }
}
