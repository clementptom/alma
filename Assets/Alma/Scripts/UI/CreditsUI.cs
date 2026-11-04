using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Scrolling end credits. Hold JUMP to speed up, press PAUSE to skip.</summary>
    public class CreditsUI : UIPanel
    {
        public float scrollSpeed = 70f;
        public float fastMultiplier = 5f;

        public const string PlaceholderCredits =
            "ALMA\n\n\n" +
            "A game by\n[Your name]\n\n\n" +
            "Story and Design\n[Your name]\n\n" +
            "Art\n[Artists and tools]\n\n" +
            "Music and Sound\n[Composers, libraries and tools]\n\n" +
            "Made with Unity\n\n\n\n" +
            "Thank you for playing";

        RectTransform scroller;
        Text body;
        Coroutine routine;

        protected override void OnBuild()
        {
            var black = UIBuilder.CreateImage(transform, "Black", Color.black);
            UIBuilder.Stretch(black.rectTransform);
            black.raycastTarget = true;

            var mask = UIBuilder.CreateRect("Viewport", transform);
            UIBuilder.Stretch(mask);
            mask.gameObject.AddComponent<RectMask2D>();

            body = UIBuilder.CreateText(mask, "Text", "", 44, TextAnchor.UpperCenter);
            scroller = body.rectTransform;
            scroller.anchorMin = scroller.anchorMax = new Vector2(0.5f, 0f);
            scroller.pivot = new Vector2(0.5f, 1f);
            scroller.sizeDelta = new Vector2(1400f, 100f);
            body.lineSpacing = 1.2f;

            SetVisible(false);
        }

        public void Play(string text, Action onFinished)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(string.IsNullOrWhiteSpace(text) ? PlaceholderCredits : text, onFinished));
        }

        IEnumerator Run(string text, Action onFinished)
        {
            IsOpen = true;
            SetVisible(true);
            body.text = text;
            yield return null; // let the layout compute the text height
            float height = body.preferredHeight;
            scroller.sizeDelta = new Vector2(scroller.sizeDelta.x, height);
            float viewHeight = ((RectTransform)scroller.parent).rect.height;
            float y = 0f;                        // top of the text starts at the bottom edge
            float endY = viewHeight + height;    // until it leaves the top

            var input = InputReader.Instance;
            while (y < endY)
            {
                if (input != null && input.PausePressed) break;
                float speed = scrollSpeed * (input != null && input.ConfirmHeld ? fastMultiplier : 1f);
                y += speed * Time.unscaledDeltaTime;
                scroller.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            IsOpen = false;
            SetVisible(false);
            routine = null;
            onFinished?.Invoke();
        }
    }
}
