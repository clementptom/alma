using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Big centered text for chapter and place cards ("Emerita Augusta, Year of Our Lord 713").
    /// Drawn above the screen fader, so it can sit on a black screen.
    /// </summary>
    public class TitleCardUI : UIPanel
    {
        Text title;
        Text subtitle;

        protected override void OnBuild()
        {
            title = UIBuilder.CreateText(transform, "Title", "", 72, TextAnchor.MiddleCenter, UIBuilder.Skin.textColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1700f, 120f));
            subtitle = UIBuilder.CreateText(transform, "Subtitle", "", 40, TextAnchor.MiddleCenter, UIBuilder.Skin.accentColor);
            UIBuilder.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1700f, 80f));
            SetVisible(false);
        }

        /// <summary>Shows the card for <paramref name="duration"/> seconds (with a short fade in and out).</summary>
        public IEnumerator Show(string titleText, string subtitleText, float duration)
        {
            title.text = titleText ?? "";
            subtitle.text = subtitleText ?? "";
            IsOpen = true;
            SetVisible(true);
            float fade = Mathf.Min(0.6f, duration * 0.3f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(Mathf.Min(t, duration - t) / Mathf.Max(0.01f, fade));
                group.alpha = a;
                yield return null;
            }
            group.alpha = 0f;
            IsOpen = false;
            SetVisible(false);
        }
    }
}
