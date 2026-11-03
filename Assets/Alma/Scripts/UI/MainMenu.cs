using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Title screen. Put this on any object in the main menu scene; it builds its own UI.
    /// Continue loads save slot 0, New Game starts fresh (asks before overwriting).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public int saveSlot = 0;
        public string titleMusic = AudioIds.MusicTitle;

        RectTransform mainPanel;
        RectTransform settingsPanel;
        RectTransform confirmPanel;
        Button continueButton;
        Button newGameButton;
        Button firstSettings;
        Button confirmNo;

        void Start()
        {
            BuildUI();
            AudioManager.PlayMusic(titleMusic);
            ShowMain();
        }

        void BuildUI()
        {
            var skin = UIBuilder.Skin;
            var canvas = UIBuilder.CreateCanvas("MainMenuCanvas", 50, transform);
            var root = canvas.transform;

            var bg = UIBuilder.CreateImage(root, "Background", skin.titleBackground != null ? Color.white : new Color(0.04f, 0.04f, 0.08f), skin.titleBackground);
            UIBuilder.Stretch(bg.rectTransform);
            bg.preserveAspect = false;

            if (skin.titleLogo != null)
            {
                var logo = UIBuilder.CreateImage(root, "Logo", Color.white, skin.titleLogo);
                logo.preserveAspect = true;
                UIBuilder.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1000f, 360f));
            }
            else
            {
                var title = UIBuilder.CreateText(root, "Title", skin.gameTitle, 160, TextAnchor.MiddleCenter, skin.accentColor);
                UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1400f, 240f));
            }

            var size = new Vector2(520f, 76f);

            // Main buttons
            mainPanel = UIBuilder.CreateRect("Main", root);
            UIBuilder.Place(mainPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(600f, 420f));
            UIBuilder.AddVerticalLayout(mainPanel.gameObject, 18f);
            continueButton = UIBuilder.CreateButton(mainPanel, "Continue", "Continue", OnContinue, size);
            newGameButton = UIBuilder.CreateButton(mainPanel, "NewGame", "New Game", OnNewGame, size);
            var settings = UIBuilder.CreateButton(mainPanel, "Settings", "Settings", ShowSettings, size);
            var quit = UIBuilder.CreateButton(mainPanel, "Quit", "Quit", OnQuit, size);
            bool hasSave = SaveSystem.Exists(saveSlot);
            continueButton.gameObject.SetActive(hasSave);
            if (hasSave) UIBuilder.ChainNavigation(continueButton, newGameButton, settings, quit);
            else UIBuilder.ChainNavigation(newGameButton, settings, quit);

            // Settings
            settingsPanel = UIBuilder.CreatePanel(root, "Settings").rectTransform;
            UIBuilder.Place(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(760f, 520f));
            UIBuilder.AddVerticalLayout(settingsPanel.gameObject, 20f, 40);
            var audio = AudioManager.Instance;
            var music = UIBuilder.CreateSlider(settingsPanel, "Music", "Music", audio != null ? audio.MusicVolume : 0.8f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
            var sfx = UIBuilder.CreateSlider(settingsPanel, "SFX", "Sound", audio != null ? audio.SfxVolume : 1f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v); });
            Text shakeLabel = null;
            var shake = UIBuilder.CreateButton(settingsPanel, "Shake", "", () =>
            {
                GameSettings.ScreenShake = !GameSettings.ScreenShake;
                shakeLabel.text = "Screen Shake: " + (GameSettings.ScreenShake ? "On" : "Off");
            }, size);
            shakeLabel = shake.GetComponentInChildren<Text>();
            shakeLabel.text = "Screen Shake: " + (GameSettings.ScreenShake ? "On" : "Off");
            Text fullscreenLabel = null;
            var fullscreen = UIBuilder.CreateButton(settingsPanel, "Fullscreen", "", () =>
            {
                GameSettings.Fullscreen = !GameSettings.Fullscreen;
                fullscreenLabel.text = "Fullscreen: " + (!Screen.fullScreen ? "On" : "Off");
            }, size);
            fullscreenLabel = fullscreen.GetComponentInChildren<Text>();
            fullscreenLabel.text = "Fullscreen: " + (Screen.fullScreen ? "On" : "Off");
            var back = UIBuilder.CreateButton(settingsPanel, "Back", "Back", ShowMain, size);
            UIBuilder.ChainNavigation(music, sfx, shake, fullscreen, back);
            firstSettings = back;

            // Overwrite confirmation
            confirmPanel = UIBuilder.CreatePanel(root, "Confirm").rectTransform;
            UIBuilder.Place(confirmPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(900f, 380f));
            UIBuilder.AddVerticalLayout(confirmPanel.gameObject, 20f, 40);
            var warning = UIBuilder.CreateText(confirmPanel, "Warning", "Start a new game?\nYour current save will be erased.", 36);
            warning.rectTransform.sizeDelta = new Vector2(800f, 120f);
            var yes = UIBuilder.CreateButton(confirmPanel, "Yes", "Yes, start over", StartNewGame, size);
            confirmNo = UIBuilder.CreateButton(confirmPanel, "No", "No", ShowMain, size);
            UIBuilder.ChainNavigation(confirmNo, yes);
        }

        void ShowMain()
        {
            mainPanel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
            confirmPanel.gameObject.SetActive(false);
            UIManager.Select(continueButton.gameObject.activeSelf ? continueButton.gameObject : newGameButton.gameObject);
        }

        void ShowSettings()
        {
            mainPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
            UIManager.Select(firstSettings.gameObject);
        }

        void OnContinue()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!gm.ContinueGame(saveSlot))
            {
                GameEvents.RaiseToast("Couldn't load the save file.");
                continueButton.gameObject.SetActive(false);
                ShowMain();
            }
            else
            {
                SetInteractable(false);
            }
        }

        void OnNewGame()
        {
            if (SaveSystem.Exists(saveSlot))
            {
                mainPanel.gameObject.SetActive(false);
                confirmPanel.gameObject.SetActive(true);
                UIManager.Select(confirmNo.gameObject);
                return;
            }
            StartNewGame();
        }

        void StartNewGame()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            SetInteractable(false);
            gm.NewGame(saveSlot);
        }

        void SetInteractable(bool interactable)
        {
            foreach (var b in GetComponentsInChildren<Selectable>(true)) b.interactable = interactable;
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
