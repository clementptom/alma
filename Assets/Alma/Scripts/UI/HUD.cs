using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Health pips, currency, interaction prompt, boss health bar and toast messages.
    /// </summary>
    public class HUD : UIPanel
    {
        RectTransform healthRow;
        readonly List<Image> pips = new List<Image>();
        RectTransform energyRoot;
        RectTransform energyFill;
        Image energyFillImage;
        Text currencyText;

        RectTransform promptRoot;
        Text promptText;
        Interactable promptTarget;

        RectTransform bossRoot;
        Text bossName;
        RectTransform bossFill;
        Boss boss;

        Text toastText;

        // The Last Lock: dread, stealth, Barro, enemy awareness, hints
        Image vignette;
        RectTransform dreadRoot;
        RectTransform dreadFill;
        Text statusText;
        Text barroText;
        RectTransform hintRoot;
        Text hintText;
        float hintTimer;
        RectTransform promptFill;
        readonly List<Text> alertLabels = new List<Text>();
        readonly Queue<string> toasts = new Queue<string>();
        float toastTimer;

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;

            healthRow = UIBuilder.CreateRect("Health", transform);
            UIBuilder.Place(healthRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -50f), new Vector2(800f, 60f));
            var layout = healthRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Energy meter
            var energyFrame = UIBuilder.CreateImage(transform, "Energy", new Color(0f, 0f, 0f, 0.6f), skin.energyFrame);
            energyRoot = energyFrame.rectTransform;
            UIBuilder.Place(energyRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -120f), new Vector2(320f, 20f));
            energyFillImage = UIBuilder.CreateImage(energyRoot, "Fill", skin.energyColor, skin.energyFill);
            energyFill = energyFillImage.rectTransform;
            energyFill.anchorMin = Vector2.zero;
            energyFill.anchorMax = new Vector2(0f, 1f);
            energyFill.offsetMin = new Vector2(2f, 2f);
            energyFill.offsetMax = new Vector2(-2f, -2f);

            var currencyIcon = UIBuilder.CreateImage(transform, "CurrencyIcon", skin.currencyColor, skin.currencyIcon);
            if (skin.currencyIcon != null) currencyIcon.color = Color.white;
            UIBuilder.Place(currencyIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -186f), new Vector2(36f, 36f));
            currencyText = UIBuilder.CreateText(transform, "Currency", "0", 36, TextAnchor.MiddleLeft);
            UIBuilder.Place(currencyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -180f), new Vector2(300f, 48f));

            // Interaction prompt (follows the world position of the interactable).
            promptRoot = UIBuilder.CreatePanel(transform, "Prompt").rectTransform;
            promptRoot.sizeDelta = new Vector2(260f, 54f);
            promptText = UIBuilder.CreateText(promptRoot, "Text", "", 28);
            UIBuilder.Stretch(promptText.rectTransform);
            promptRoot.gameObject.SetActive(false);

            // Boss bar
            bossRoot = UIBuilder.CreateRect("BossBar", transform);
            UIBuilder.Place(bossRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1100f, 70f));
            bossName = UIBuilder.CreateText(bossRoot, "Name", "", 30, TextAnchor.LowerCenter);
            UIBuilder.Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -28f), new Vector2(1100f, 40f));
            var frame = UIBuilder.CreateImage(bossRoot, "Frame", new Color(0f, 0f, 0f, 0.7f), skin.bossBarFrame);
            UIBuilder.Place(frame.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1100f, 26f));
            var fill = UIBuilder.CreateImage(frame.transform, "Fill", skin.bossBarColor, skin.bossBarFill);
            bossFill = fill.rectTransform;
            bossFill.anchorMin = Vector2.zero;
            bossFill.anchorMax = Vector2.one;
            bossFill.offsetMin = new Vector2(3f, 3f);
            bossFill.offsetMax = new Vector2(-3f, -3f);
            bossRoot.gameObject.SetActive(false);

            // Toasts
            toastText = UIBuilder.CreateText(transform, "Toast", "", 32, TextAnchor.UpperRight);
            UIBuilder.Place(toastText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(900f, 60f));
            toastText.color = new Color(1f, 1f, 1f, 0f);

            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.PlayerEnergyChanged += OnEnergyChanged;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            if (InputReader.Instance != null)
            {
                InputReader.Instance.DeviceChanged += RefreshPrompt;
                InputReader.Instance.BindingsChanged += RefreshPrompt;
            }
            GameEvents.Toast += OnToast;
            GameEvents.BossStarted += OnBossStarted;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.Hint += OnHint;

            BuildLastLockHud(skin);
            SetVisible(false);
        }

        void BuildLastLockHud(UISkin skin)
        {
            // Dread vignette: behind everything else on the HUD.
            vignette = UIBuilder.CreateImage(transform, "DreadVignette", new Color(0.02f, 0f, 0.04f, 0f), skin.dreadVignette != null ? skin.dreadVignette : Placeholders.Vignette);
            UIBuilder.Stretch(vignette.rectTransform);
            vignette.transform.SetAsFirstSibling();

            var frame = UIBuilder.CreateImage(transform, "Dread", new Color(0f, 0f, 0f, 0.55f), skin.dreadFrame);
            dreadRoot = frame.rectTransform;
            UIBuilder.Place(dreadRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -146f), new Vector2(240f, 14f));
            var fill = UIBuilder.CreateImage(dreadRoot, "Fill", skin.dreadColor, skin.dreadFill);
            dreadFill = fill.rectTransform;
            dreadFill.anchorMin = Vector2.zero;
            dreadFill.anchorMax = new Vector2(0f, 1f);
            dreadFill.offsetMin = new Vector2(2f, 2f);
            dreadFill.offsetMax = new Vector2(-2f, -2f);
            var dreadLabel = UIBuilder.CreateText(dreadRoot, "Label", "Dread", 18, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(dreadLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(120f, 24f));
            dreadRoot.gameObject.SetActive(false);

            statusText = UIBuilder.CreateText(transform, "StealthStatus", "", 24, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 1f, 0.9f));
            UIBuilder.Place(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -230f), new Vector2(500f, 34f));
            barroText = UIBuilder.CreateText(transform, "BarroStatus", "", 24, TextAnchor.MiddleLeft, new Color(0.95f, 0.8f, 0.6f, 0.9f));
            UIBuilder.Place(barroText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -262f), new Vector2(500f, 34f));

            hintRoot = UIBuilder.CreatePanel(transform, "Hint").rectTransform;
            UIBuilder.Place(hintRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1100f, 70f));
            hintText = UIBuilder.CreateText(hintRoot, "Text", "", 28);
            UIBuilder.Stretch(hintText.rectTransform, 12f);
            hintRoot.gameObject.SetActive(false);

            var progress = UIBuilder.CreateImage(promptRoot, "Progress", skin.accentColor);
            promptFill = progress.rectTransform;
            promptFill.anchorMin = Vector2.zero;
            promptFill.anchorMax = new Vector2(0f, 0f);
            promptFill.offsetMin = Vector2.zero;
            promptFill.offsetMax = new Vector2(0f, 5f);
        }

        void OnHint(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                hintTimer = 0f;
                hintRoot.gameObject.SetActive(false);
                return;
            }
            hintText.text = text;
            hintTimer = 7f;
            hintRoot.gameObject.SetActive(true);
        }

        /// <summary>Fill of the prompt bar for hold-to-interact objects (0..1).</summary>
        public void SetPromptProgress(float progress)
        {
            if (promptFill == null) return;
            promptFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 0f);
        }

        void UpdateLastLockHud()
        {
            var dread = DreadMeter.Instance;
            float value = dread != null ? dread.Value : 0f;
            var vc = vignette.color;
            vc.a = Mathf.MoveTowards(vc.a, Mathf.Clamp01(value / 100f) * 0.85f, Time.unscaledDeltaTime);
            vignette.color = vc;
            dreadRoot.gameObject.SetActive(value > 1f);
            dreadFill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);

            var player = PlayerController.Instance;
            string status = "";
            if (player != null && player.Stealth != null)
            {
                if (player.Stealth.IsDisguised) status = "Disguised";
                else if (player.Stealth.IsHidden) status = "Hidden";
                else if (player.Stealth.IsSneaking) status = "Sneaking";
            }
            statusText.text = status;

            var barro = BarroCompanion.Instance;
            barroText.text = barro == null ? "" : "Barro: " + BarroStatus(barro);

            if (hintTimer > 0f)
            {
                hintTimer -= Time.unscaledDeltaTime;
                if (hintTimer <= 0f) hintRoot.gameObject.SetActive(false);
            }

            UpdateAlerts();
        }

        static string BarroStatus(BarroCompanion barro)
        {
            switch (barro.Current)
            {
                case BarroCompanion.Mode.Guard: return "staying";
                case BarroCompanion.Mode.Tracking: return "tracking";
                case BarroCompanion.Mode.Fetching: return "fetching";
                case BarroCompanion.Mode.Attacking: return "attacking";
                case BarroCompanion.Mode.Downed: return "down!";
                case BarroCompanion.Mode.Scripted: return "";
                default: return "with you";
            }
        }

        void UpdateAlerts()
        {
            var cam = Camera.main;
            int used = 0;
            if (cam != null)
            {
                var parent = (RectTransform)transform;
                foreach (var p in StealthPerception.Active)
                {
                    if (p == null || (p.State == Awareness.Unaware && p.Detection < 0.05f)) continue;
                    var enemy = p.GetComponent<Enemy>();
                    if (enemy != null && enemy.IsDead) continue;
                    Vector3 screen = cam.WorldToScreenPoint((Vector2)p.transform.position + Vector2.up * 2.6f);
                    if (screen.z < 0f) continue;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local)) continue;

                    if (used >= alertLabels.Count)
                    {
                        var t = UIBuilder.CreateText(transform, "Alert", "", 44, TextAnchor.MiddleCenter);
                        t.rectTransform.sizeDelta = new Vector2(60f, 60f);
                        alertLabels.Add(t);
                    }
                    var label = alertLabels[used++];
                    label.gameObject.SetActive(true);
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    label.rectTransform.anchoredPosition = local;
                    bool alert = p.State == Awareness.Alert;
                    label.text = alert ? "!" : "?";
                    float k = alert ? 1f : Mathf.Clamp01(p.Detection + (p.State != Awareness.Unaware ? 0.4f : 0f));
                    label.color = alert ? new Color(1f, 0.25f, 0.2f, 1f) : new Color(1f, 0.9f, 0.3f, k);
                }
            }
            for (int i = used; i < alertLabels.Count; i++) alertLabels[i].gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.PlayerEnergyChanged -= OnEnergyChanged;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            if (InputReader.Instance != null)
            {
                InputReader.Instance.DeviceChanged -= RefreshPrompt;
                InputReader.Instance.BindingsChanged -= RefreshPrompt;
            }
            GameEvents.Toast -= OnToast;
            GameEvents.BossStarted -= OnBossStarted;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.Hint -= OnHint;
        }

        public void RefreshAll()
        {
            var player = PlayerController.Instance;
            if (player != null && player.PlayerHealth.Health != null)
                OnHealthChanged(player.PlayerHealth.Health.Current, player.PlayerHealth.Health.Max);
            if (player != null && player.Energy != null)
                OnEnergyChanged(player.Energy.Current, player.Energy.Max);
            var gm = GameManager.Instance;
            OnCurrencyChanged(gm != null ? gm.Currency : 0);
            if (boss == null) bossRoot.gameObject.SetActive(false);
        }

        void OnHealthChanged(int current, int max)
        {
            var skin = UIBuilder.Skin;
            while (pips.Count < max)
            {
                var pip = UIBuilder.CreateImage(healthRow, "Pip", Color.white);
                pip.rectTransform.sizeDelta = new Vector2(44f, 44f);
                pips.Add(pip);
            }
            for (int i = 0; i < pips.Count; i++)
            {
                bool exists = i < max;
                pips[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool full = i < current;
                Sprite sprite = full ? skin.healthFull : skin.healthEmpty;
                pips[i].sprite = sprite;
                pips[i].color = sprite != null ? Color.white : (full ? skin.healthFullColor : skin.healthEmptyColor);
            }
        }

        void OnEnergyChanged(int current, int max)
        {
            energyRoot.gameObject.SetActive(max > 0);
            if (max <= 0) return;
            // The bar's width grows with the maximum, so upgrades are visible.
            energyRoot.sizeDelta = new Vector2(Mathf.Clamp(max * 3.2f, 120f, 640f), energyRoot.sizeDelta.y);
            energyFill.anchorMax = new Vector2(Mathf.Clamp01((float)current / max), 1f);
            var player = PlayerController.Instance;
            bool ready = player != null && player.Energy != null && player.Energy.CanAffordHeal;
            var skin = UIBuilder.Skin;
            if (skin.energyFill == null) energyFillImage.color = ready ? skin.energyReadyColor : skin.energyColor;
        }

        void OnCurrencyChanged(int amount)
        {
            currencyText.text = amount.ToString();
        }

        public void SetPrompt(Interactable target)
        {
            promptTarget = target;
            if (target == null)
            {
                promptRoot.gameObject.SetActive(false);
                return;
            }
            var input = InputReader.Instance;
            string key = input != null ? input.PromptFor("Interact") : "E";
            promptText.text = target.holdDuration > 0f ? $"[Hold {key}]  {target.prompt}" : $"[{key}]  {target.prompt}";
            SetPromptProgress(0f);
            promptRoot.gameObject.SetActive(true);
            PositionPrompt();
        }

        void RefreshPrompt()
        {
            if (promptTarget != null) SetPrompt(promptTarget);
        }

        void PositionPrompt()
        {
            if (promptTarget == null || Camera.main == null) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(promptTarget.PromptWorldPosition);
            var parent = (RectTransform)promptRoot.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local))
            {
                promptRoot.anchorMin = promptRoot.anchorMax = new Vector2(0.5f, 0.5f);
                promptRoot.anchoredPosition = local;
            }
        }

        void OnBossStarted(Boss b)
        {
            boss = b;
            bossName.text = b.displayName;
            bossRoot.gameObject.SetActive(true);
        }

        void OnBossDefeated(Boss b)
        {
            if (b == boss) boss = null;
            bossRoot.gameObject.SetActive(false);
        }

        void OnToast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            toasts.Enqueue(message);
        }

        void LateUpdate()
        {
            if (promptTarget != null) PositionPrompt();
            UpdateLastLockHud();

            if (boss != null)
            {
                bossFill.anchorMax = new Vector2(Mathf.Clamp01(boss.Health.Fraction), 1f);
            }
            else if (bossRoot.gameObject.activeSelf)
            {
                bossRoot.gameObject.SetActive(false);
            }

            // Toast: fade in, hold, fade out.
            if (toastTimer <= 0f && toasts.Count > 0)
            {
                toastText.text = toasts.Dequeue();
                toastTimer = 2.5f;
            }
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(Mathf.Min(2.5f - toastTimer, toastTimer) * 4f);
                var c = toastText.color;
                c.a = a;
                toastText.color = c;
            }
        }
    }
}
