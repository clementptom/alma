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
        Text currencyText;

        RectTransform promptRoot;
        Text promptText;
        Interactable promptTarget;

        RectTransform bossRoot;
        Text bossName;
        RectTransform bossFill;
        Boss boss;

        Text toastText;
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

            var currencyIcon = UIBuilder.CreateImage(transform, "CurrencyIcon", skin.currencyColor, skin.currencyIcon);
            if (skin.currencyIcon != null) currencyIcon.color = Color.white;
            UIBuilder.Place(currencyIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -126f), new Vector2(36f, 36f));
            currencyText = UIBuilder.CreateText(transform, "Currency", "0", 36, TextAnchor.MiddleLeft);
            UIBuilder.Place(currencyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -120f), new Vector2(300f, 48f));

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
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.Toast += OnToast;
            GameEvents.BossStarted += OnBossStarted;
            GameEvents.BossDefeated += OnBossDefeated;

            SetVisible(false);
        }

        void OnDestroy()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.Toast -= OnToast;
            GameEvents.BossStarted -= OnBossStarted;
            GameEvents.BossDefeated -= OnBossDefeated;
        }

        public void RefreshAll()
        {
            var player = PlayerController.Instance;
            if (player != null && player.PlayerHealth.Health != null)
                OnHealthChanged(player.PlayerHealth.Health.Current, player.PlayerHealth.Health.Max);
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
            promptText.text = $"[{key}]  {target.prompt}";
            promptRoot.gameObject.SetActive(true);
            PositionPrompt();
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
