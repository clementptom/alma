using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// "Press JUMP repeatedly!" struggle prompt (digging out of an avalanche, breaking free).
    /// The bar fills with each press and slowly drains.
    /// </summary>
    public class MashUI : UIPanel
    {
        Text prompt;
        RectTransform fill;

        protected override void OnBuild()
        {
            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(800f, 170f));
            prompt = UIBuilder.CreateText(box, "Prompt", "", 34);
            UIBuilder.Place(prompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(760f, 60f));
            var bar = UIBuilder.CreateImage(box, "Bar", new Color(0f, 0f, 0f, 0.6f)).rectTransform;
            UIBuilder.Place(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(700f, 34f));
            fill = UIBuilder.CreateImage(bar, "Fill", UIBuilder.Skin.accentColor).rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            SetVisible(false);
        }

        /// <summary>Runs until the player has pressed confirm about <paramref name="presses"/> times.</summary>
        public IEnumerator Run(string text, int presses, string soundId)
        {
            var input = InputReader.Instance;
            string key = input != null ? input.PromptFor("Jump") : "Jump";
            prompt.text = string.IsNullOrEmpty(text) ? $"Press {key} repeatedly!" : text.Replace("{key}", key);
            IsOpen = true;
            SetVisible(true);
            float progress = 0f;
            float step = 1f / Mathf.Max(1, presses);
            int startFrame = Time.frameCount;
            while (progress < 1f)
            {
                if (input != null && input.ConfirmPressed && Time.frameCount != startFrame)
                {
                    progress += step;
                    if (!string.IsNullOrEmpty(soundId)) AudioManager.PlaySfx(soundId);
                    CameraController.Shake(0.05f, 0.08f);
                }
                progress = Mathf.Max(0f, progress - Time.unscaledDeltaTime * step * 0.8f);
                fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                yield return null;
            }
            IsOpen = false;
            SetVisible(false);
        }
    }
}
