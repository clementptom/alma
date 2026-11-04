using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>Pause menu: resume, volume sliders, screen shake toggle, quit to title.</summary>
    public class PauseMenu : UIPanel
    {
        Button resume;
        Slider music;
        Slider sfx;
        Text shakeLabel;

        protected override void OnBuild()
        {
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.6f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var box = UIBuilder.CreatePanel(transform, "Box");
            UIBuilder.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 880f));
            UIBuilder.AddVerticalLayout(box.gameObject, 18f, 36);

            var title = UIBuilder.CreateText(box.transform, "Title", "Paused", 60, TextAnchor.MiddleCenter, UIBuilder.Skin.accentColor);
            title.rectTransform.sizeDelta = new Vector2(600f, 90f);

            var audio = AudioManager.Instance;
            var size = new Vector2(520f, 72f);
            resume = UIBuilder.CreateButton(box.transform, "Resume", "Resume", OnResume, size);
            var inventory = UIBuilder.CreateButton(box.transform, "Inventory", "Inventory", () => OpenOverlay(UIManager.Instance.Inventory), size);
            var controls = UIBuilder.CreateButton(box.transform, "Controls", "Controls", () => OpenOverlay(UIManager.Instance.Controls), size);
            music = UIBuilder.CreateSlider(box.transform, "Music", "Music", audio != null ? audio.MusicVolume : 0.8f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
            sfx = UIBuilder.CreateSlider(box.transform, "SFX", "Sound", audio != null ? audio.SfxVolume : 1f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v); });
            var shake = UIBuilder.CreateButton(box.transform, "Shake", "", ToggleShake, size);
            shakeLabel = shake.GetComponentInChildren<Text>();
            var quit = UIBuilder.CreateButton(box.transform, "Quit", "Quit to Title", OnQuit, size);

            UIBuilder.ChainNavigation(resume, inventory, controls, music, sfx, shake, quit);
            RefreshShakeLabel();
            SetVisible(false);
        }

        public void Show()
        {
            IsOpen = true;
            SetVisible(true);
            RefreshShakeLabel();
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                music.SetValueWithoutNotify(audio.MusicVolume);
                sfx.SetValueWithoutNotify(audio.SfxVolume);
            }
            UIManager.Select(resume.gameObject);
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SetVisible(false);
            UIManager.Select(null);
        }

        static void OpenOverlay(OverlayPanel panel)
        {
            if (GameManager.Instance != null) GameManager.Instance.OpenOverlay(panel);
        }

        void OnResume()
        {
            if (GameManager.Instance != null) GameManager.Instance.Resume();
        }

        void OnQuit()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
        }

        void ToggleShake()
        {
            GameSettings.ScreenShake = !GameSettings.ScreenShake;
            RefreshShakeLabel();
        }

        void RefreshShakeLabel()
        {
            if (shakeLabel != null) shakeLabel.text = "Screen Shake: " + (GameSettings.ScreenShake ? "On" : "Off");
        }
    }
}
