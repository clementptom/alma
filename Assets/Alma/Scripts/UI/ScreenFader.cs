using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Full-screen black overlay for scene transitions. Uses unscaled time.</summary>
    public class ScreenFader : UIPanel
    {
        Image image;

        public float Alpha => group != null ? group.alpha : 0f;

        protected override void OnBuild()
        {
            image = UIBuilder.CreateImage(transform, "Black", Color.black);
            UIBuilder.Stretch(image.rectTransform);
            image.raycastTarget = true;
            SetAlpha(0f);
        }

        public void SetAlpha(float alpha)
        {
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.5f;
            group.interactable = false;
        }

        public IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(target);
        }
    }
}
