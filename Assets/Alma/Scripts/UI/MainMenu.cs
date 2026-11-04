using System;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Title screen with three save slots. Put this on any object in the main menu scene;
    /// it builds its own UI.
    ///   Continue  - loads the most recently saved slot
    ///   Play      - pick a slot: empty slots start a new game, filled ones continue (or erase)
    ///   Settings  - volume, screen shake, fullscreen, controls
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public string titleMusic = AudioIds.MusicTitle;

        RectTransform mainPanel;
        RectTransform slotsPanel;
        RectTransform settingsPanel;
        RectTransform confirmPanel;
        Button continueButton;
        Button playButton;
        Button settingsButton;
        Button quitButton;
        Button firstSettings;
        readonly Button[] slotButtons = new Button[SaveSystem.SlotCount];
        readonly Button[] eraseButtons = new Button[SaveSystem.SlotCount];
        Button slotsBack;
        Text confirmText;
        Button confirmYes;
        Button confirmNo;
        Action confirmAction;
        Action cancelAction;

        void Start()
        {
            BuildUI();
            AudioManager.PlayMusic(titleMusic);
            ShowMain();
        }

        // ================================================================== UI

        void BuildUI()
        {
            var skin = UIBuilder.Skin;
            var canvas = UIBuilder.CreateCanvas("MainMenuCanvas", 50, transform);
            var root = canvas.transform;

            var bg = UIBuilder.CreateImage(root, "Background", skin.titleBackground != null ? Color.white : new Color(0.04f, 0.04f, 0.08f), skin.titleBackground);
            UIBuilder.Stretch(bg.rectTransform);

            if (skin.titleLogo != null)
            {
                var logo = UIBuilder.CreateImage(root, "Logo", Color.white, skin.titleLogo);
                logo.preserveAspect = true;
                UIBuilder.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1000f, 340f));
            }
            else
            {
                var title = UIBuilder.CreateText(root, "Title", skin.gameTitle, 120, TextAnchor.MiddleCenter, skin.accentColor);
                UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 240f));
            }

            var size = new Vector2(520f, 76f);

            // Main buttons
            mainPanel = UIBuilder.CreateRect("Main", root);
            UIBuilder.Place(mainPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(600f, 420f));
            UIBuilder.AddVerticalLayout(mainPanel.gameObject, 18f);
            continueButton = UIBuilder.CreateButton(mainPanel, "Continue", "Continue", OnContinue, size);
            playButton = UIBuilder.CreateButton(mainPanel, "Play", "Play", ShowSlots, size);
            settingsButton = UIBuilder.CreateButton(mainPanel, "Settings", "Settings", ShowSettings, size);
            quitButton = UIBuilder.CreateButton(mainPanel, "Quit", "Quit", OnQuit, size);

            // Save slots
            slotsPanel = UIBuilder.CreatePanel(root, "Slots").rectTransform;
            UIBuilder.Place(slotsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1200f, 640f));
            var slotsTitle = UIBuilder.CreateText(slotsPanel, "Title", "Choose a save slot", 44, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(slotsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1000f, 60f));
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                int slot = i;
                float y = -110f - i * 140f;
                slotButtons[i] = UIBuilder.CreateButton(slotsPanel, "Slot " + i, "", () => OnSlot(slot), new Vector2(880f, 120f));
                UIBuilder.Place((RectTransform)slotButtons[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, y), new Vector2(880f, 120f));
                var label = slotButtons[i].GetComponentInChildren<Text>();
                label.fontSize = 28;
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(30f, 0f);
                eraseButtons[i] = UIBuilder.CreateButton(slotsPanel, "Erase " + i, "Erase", () => OnErase(slot), new Vector2(220f, 120f));
                UIBuilder.Place((RectTransform)eraseButtons[i].transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, y), new Vector2(220f, 120f));
            }
            slotsBack = UIBuilder.CreateButton(slotsPanel, "Back", "Back", ShowMain, new Vector2(320f, 70f));
            UIBuilder.Place((RectTransform)slotsBack.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(320f, 70f));

            // Settings
            settingsPanel = UIBuilder.CreatePanel(root, "Settings").rectTransform;
            UIBuilder.Place(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(760f, 620f));
            UIBuilder.AddVerticalLayout(settingsPanel.gameObject, 18f, 36);
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
            var controls = UIBuilder.CreateButton(settingsPanel, "Controls", "Controls", OpenControls, size);
            var back = UIBuilder.CreateButton(settingsPanel, "Back", "Back", ShowMain, size);
            UIBuilder.ChainNavigation(music, sfx, shake, fullscreen, controls, back);
            firstSettings = back;

            // Confirmation
            confirmPanel = UIBuilder.CreatePanel(root, "Confirm").rectTransform;
            UIBuilder.Place(confirmPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(900f, 380f));
            UIBuilder.AddVerticalLayout(confirmPanel.gameObject, 20f, 40);
            confirmText = UIBuilder.CreateText(confirmPanel, "Message", "", 36);
            confirmText.rectTransform.sizeDelta = new Vector2(800f, 120f);
            confirmYes = UIBuilder.CreateButton(confirmPanel, "Yes", "Yes", () => confirmAction?.Invoke(), size);
            confirmNo = UIBuilder.CreateButton(confirmPanel, "No", "No", () => cancelAction?.Invoke(), size);
            UIBuilder.ChainNavigation(confirmNo, confirmYes);
        }

        void ShowOnly(RectTransform panel)
        {
            mainPanel.gameObject.SetActive(panel == mainPanel);
            slotsPanel.gameObject.SetActive(panel == slotsPanel);
            settingsPanel.gameObject.SetActive(panel == settingsPanel);
            confirmPanel.gameObject.SetActive(panel == confirmPanel);
        }

        void ShowMain()
        {
            ShowOnly(mainPanel);
            bool any = SaveSystem.LatestSlot() >= 0;
            continueButton.gameObject.SetActive(any);
            if (any) UIBuilder.ChainNavigation(continueButton, playButton, settingsButton, quitButton);
            else UIBuilder.ChainNavigation(playButton, settingsButton, quitButton);
            UIManager.Select(any ? continueButton.gameObject : playButton.gameObject);
        }

        void ShowSlots()
        {
            ShowOnly(slotsPanel);
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                var data = SaveSystem.Load(i);
                slotButtons[i].GetComponentInChildren<Text>().text = Describe(i, data);
                eraseButtons[i].gameObject.SetActive(data != null);
            }
            WireSlotNavigation();
            UIManager.Select(slotButtons[0].gameObject);
        }

        void WireSlotNavigation()
        {
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                var up = i > 0 ? slotButtons[i - 1] : slotsBack;
                var down = i < SaveSystem.SlotCount - 1 ? slotButtons[i + 1] : slotsBack;
                bool canErase = eraseButtons[i].gameObject.activeSelf;
                slotButtons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = up,
                    selectOnDown = down,
                    selectOnRight = canErase ? eraseButtons[i] : null
                };
                eraseButtons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = slotButtons[i] };
            }
            slotsBack.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = slotButtons[SaveSystem.SlotCount - 1],
                selectOnDown = slotButtons[0]
            };
        }

        static string Describe(int slot, SaveData data)
        {
            if (data == null) return $"Slot {slot + 1}      Empty - New Game";
            var time = TimeSpan.FromSeconds(data.playTimeSeconds);
            string place = string.IsNullOrEmpty(data.locationName) ? data.respawnScene : data.locationName;
            int abilities = data.abilities.Count;
            int totalAbilities = Enum.GetValues(typeof(AbilityType)).Length;
            string done = data.HasFlag(GameEnding.CompletedFlag) ? "     Completed" : "";
            return $"Slot {slot + 1}      {place}{done}\n" +
                   $"Time {(int)time.TotalHours}:{time.Minutes:00}      Health {data.maxHealth}      Abilities {abilities}/{totalAbilities}      Currency {data.currency}";
        }

        void ShowSettings()
        {
            ShowOnly(settingsPanel);
            UIManager.Select(firstSettings.gameObject);
        }

        void OpenControls()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;
            settingsPanel.gameObject.SetActive(false);
            ui.Controls.OpenStandalone(ShowSettings);
        }

        void Confirm(string message, Action onYes, Action onNo)
        {
            ShowOnly(confirmPanel);
            confirmText.text = message;
            confirmAction = onYes;
            cancelAction = onNo;
            UIManager.Select(confirmNo.gameObject);
        }

        // ============================================================= Actions

        void OnContinue()
        {
            int slot = SaveSystem.LatestSlot();
            if (slot < 0)
            {
                ShowMain();
                return;
            }
            Load(slot);
        }

        void OnSlot(int slot)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (SaveSystem.Exists(slot))
            {
                Load(slot);
            }
            else
            {
                SetInteractable(false);
                gm.NewGame(slot);
            }
        }

        void Load(int slot)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            SetInteractable(false);
            if (!gm.ContinueGame(slot))
            {
                SetInteractable(true);
                Confirm($"Slot {slot + 1} couldn't be loaded. Erase it?", () => { SaveSystem.Delete(slot); ShowSlots(); }, ShowSlots);
            }
        }

        void OnErase(int slot)
        {
            Confirm($"Erase slot {slot + 1}?\nThis can't be undone.", () =>
            {
                SaveSystem.Delete(slot);
                ShowSlots();
            }, ShowSlots);
        }

        void SetInteractable(bool interactable)
        {
            foreach (var b in GetComponentsInChildren<Selectable>(true)) b.interactable = interactable;
        }

        void Update()
        {
            // Back out of sub-menus with Esc / B.
            var input = InputReader.Instance;
            var ui = UIManager.Instance;
            if (input == null || !input.CancelPressed) return;
            if (ui != null && ui.Controls.IsOpen) return;
            if (slotsPanel.gameObject.activeSelf || settingsPanel.gameObject.activeSelf)
            {
                AudioManager.PlaySfx(AudioIds.UiBack);
                ShowMain();
            }
            else if (confirmPanel.gameObject.activeSelf)
            {
                AudioManager.PlaySfx(AudioIds.UiBack);
                cancelAction?.Invoke();
            }
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
