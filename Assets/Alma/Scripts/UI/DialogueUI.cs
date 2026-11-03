using System;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Text box with speaker name, portrait and typewriter effect. Advance with Jump/Attack/Interact.</summary>
    public class DialogueUI : UIPanel
    {
        public float charactersPerSecond = 45f;

        Image portrait;
        Text speakerText;
        Text bodyText;
        Text continueHint;

        DialogueLine[] lines;
        string defaultSpeaker;
        Action onComplete;
        int index;
        float visibleChars;
        int openedFrame;
        int lastBlipChar;

        protected override void OnBuild()
        {
            var box = UIBuilder.CreatePanel(transform, "Box");
            UIBuilder.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1500f, 300f));

            portrait = UIBuilder.CreateImage(box.transform, "Portrait", Color.white);
            UIBuilder.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(240f, 240f));
            portrait.preserveAspect = true;

            speakerText = UIBuilder.CreateText(box.transform, "Speaker", "", 36, TextAnchor.UpperLeft, UIBuilder.Skin.accentColor);
            UIBuilder.Place(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -24f), new Vector2(1150f, 48f));

            bodyText = UIBuilder.CreateText(box.transform, "Body", "", 34, TextAnchor.UpperLeft);
            UIBuilder.Place(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -80f), new Vector2(1150f, 190f));

            continueHint = UIBuilder.CreateText(box.transform, "Continue", "▼", 30, TextAnchor.LowerRight, UIBuilder.Skin.accentColor);
            UIBuilder.Place(continueHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(60f, 40f));

            SetVisible(false);
        }

        public void Show(DialogueLine[] dialogueLines, string fallbackSpeaker, Action completed)
        {
            lines = dialogueLines;
            defaultSpeaker = fallbackSpeaker;
            onComplete = completed;
            index = 0;
            openedFrame = Time.frameCount;
            IsOpen = true;
            SetVisible(true);
            ShowLine();
        }

        void ShowLine()
        {
            var line = lines[index];
            string speaker = string.IsNullOrEmpty(line.speaker) ? defaultSpeaker : line.speaker;
            speakerText.text = speaker ?? "";
            portrait.sprite = line.portrait;
            portrait.gameObject.SetActive(line.portrait != null);
            float textX = line.portrait != null ? 300f : 50f;
            speakerText.rectTransform.anchoredPosition = new Vector2(textX, -24f);
            bodyText.rectTransform.anchoredPosition = new Vector2(textX, -80f);
            bodyText.text = "";
            visibleChars = 0f;
            lastBlipChar = 0;
            continueHint.enabled = false;
        }

        void Update()
        {
            if (!IsOpen) return;
            string full = lines[index].text ?? "";

            if (visibleChars < full.Length)
            {
                visibleChars += charactersPerSecond * Time.unscaledDeltaTime;
                int count = Mathf.Min(full.Length, Mathf.FloorToInt(visibleChars));
                bodyText.text = full.Substring(0, count);
                if (count - lastBlipChar >= 3)
                {
                    lastBlipChar = count;
                    AudioManager.PlaySfx(AudioIds.UiDialogueBlip);
                }
                if (count >= full.Length) continueHint.enabled = true;
            }

            var input = InputReader.Instance;
            if (input == null || Time.frameCount == openedFrame || !input.ConfirmPressed) return;

            if (visibleChars < full.Length)
            {
                // First press completes the line.
                visibleChars = full.Length;
                bodyText.text = full;
                continueHint.enabled = true;
                return;
            }

            index++;
            if (index < lines.Length)
            {
                ShowLine();
            }
            else
            {
                Close();
            }
        }

        void Close()
        {
            IsOpen = false;
            SetVisible(false);
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
