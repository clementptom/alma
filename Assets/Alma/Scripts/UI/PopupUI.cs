using System;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Centered announcement ("New ability: Dash") that pauses the game until dismissed.</summary>
    public class PopupUI : UIPanel
    {
        public float minimumShowTime = 0.6f;

        Image icon;
        Text title;
        Text body;
        Text hint;
        Action onClosed;
        float shownAt;

        protected override void OnBuild()
        {
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            UIBuilder.Stretch(dim.rectTransform);

            var box = UIBuilder.CreatePanel(transform, "Box");
            UIBuilder.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 520f));

            icon = UIBuilder.CreateImage(box.transform, "Icon", Color.white);
            UIBuilder.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(160f, 160f));
            icon.preserveAspect = true;

            title = UIBuilder.CreateText(box.transform, "Title", "", 56, TextAnchor.MiddleCenter, UIBuilder.Skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(900f, 80f));

            body = UIBuilder.CreateText(box.transform, "Body", "", 34, TextAnchor.UpperCenter);
            UIBuilder.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(880f, 130f));

            hint = UIBuilder.CreateText(box.transform, "Hint", "", 26, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(880f, 40f));

            SetVisible(false);
        }

        public void Show(string titleText, string bodyText, Sprite iconSprite, Action closed)
        {
            title.text = titleText ?? "";
            body.text = bodyText ?? "";
            icon.sprite = iconSprite;
            icon.gameObject.SetActive(iconSprite != null);
            var input = InputReader.Instance;
            hint.text = $"Press {(input != null ? input.PromptFor("Jump") : "Jump")} to continue";
            onClosed = closed;
            shownAt = Time.unscaledTime;
            IsOpen = true;
            SetVisible(true);
        }

        void Update()
        {
            if (!IsOpen) return;
            var input = InputReader.Instance;
            if (input == null || Time.unscaledTime - shownAt < minimumShowTime) return;
            if (input.ConfirmPressed || input.PausePressed)
            {
                IsOpen = false;
                SetVisible(false);
                AudioManager.PlaySfx(AudioIds.UiConfirm);
                var callback = onClosed;
                onClosed = null;
                callback?.Invoke();
            }
        }
    }
}
