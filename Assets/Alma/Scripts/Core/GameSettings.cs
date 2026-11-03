using UnityEngine;

namespace Alma
{
    /// <summary>Player options stored in PlayerPrefs (volume lives in AudioManager).</summary>
    public static class GameSettings
    {
        const string ScreenShakeKey = "alma.screenShake";

        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(ScreenShakeKey, 1) == 1;
            set => PlayerPrefs.SetInt(ScreenShakeKey, value ? 1 : 0);
        }

        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }
    }
}
