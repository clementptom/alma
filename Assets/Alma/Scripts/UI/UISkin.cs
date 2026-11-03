using UnityEngine;

namespace Alma
{
    /// <summary>
    /// All UI art in one place. Lives at Resources/UISkin.asset. Every field is optional:
    /// empty sprites draw as flat colored boxes.
    /// </summary>
    [CreateAssetMenu(menuName = "Alma/UI Skin", fileName = "UISkin")]
    public class UISkin : ScriptableObject
    {
        [Header("Text")]
        [Tooltip("Leave empty to use Unity's built-in font.")]
        public Font font;
        public Color textColor = new Color(0.95f, 0.93f, 0.88f);
        public Color accentColor = new Color(0.98f, 0.78f, 0.35f);

        [Header("Panels and Buttons (9-sliced sprites work best)")]
        public Sprite panelSprite;
        public Color panelColor = new Color(0.05f, 0.05f, 0.09f, 0.88f);
        public Sprite buttonSprite;
        public Color buttonColor = new Color(0.16f, 0.15f, 0.22f, 1f);
        public Color buttonHighlightColor = new Color(0.32f, 0.28f, 0.42f, 1f);

        [Header("HUD")]
        public Sprite healthFull;
        public Sprite healthEmpty;
        public Color healthFullColor = new Color(0.92f, 0.28f, 0.3f);
        public Color healthEmptyColor = new Color(0.25f, 0.2f, 0.25f, 0.8f);
        public Sprite currencyIcon;
        public Color currencyColor = new Color(0.98f, 0.82f, 0.3f);
        public Sprite bossBarFrame;
        public Sprite bossBarFill;
        public Color bossBarColor = new Color(0.8f, 0.15f, 0.2f);

        [Header("Map")]
        public Color mapRoomColor = new Color(0.35f, 0.45f, 0.6f, 0.9f);
        public Color mapCurrentRoomColor = new Color(0.98f, 0.78f, 0.35f, 0.95f);
        public Sprite mapPlayerIcon;

        [Header("Title Screen")]
        public Sprite titleBackground;
        [Tooltip("Logo image. If empty, the game title is drawn as text.")]
        public Sprite titleLogo;
        public string gameTitle = "ALMA";

        static UISkin cached;

        public static UISkin Get()
        {
            if (cached == null)
            {
                cached = Resources.Load<UISkin>("UISkin");
                if (cached == null) cached = CreateInstance<UISkin>();
            }
            return cached;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cached = null;
    }
}
