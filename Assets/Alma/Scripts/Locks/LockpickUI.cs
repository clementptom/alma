using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// The lockpicking minigame. One pin at a time: a marker slides back and forth along the
    /// lock; press JUMP / ATTACK / INTERACT while it's in the gold zone to set the pin.
    /// Missing strains the pick; too many misses and it breaks (noisy). The world keeps
    /// running while you pick, so watch for patrols. Esc / B backs out.
    /// </summary>
    public class LockpickUI : UIPanel
    {
        LockpickLock target;
        RectTransform track;
        RectTransform zone;
        RectTransform marker;
        RectTransform pinRow;
        Text info;
        readonly List<Image> pinIcons = new List<Image>();

        int pinIndex;
        float position;
        int direction = 1;
        float zoneCenter;
        float zoneWidth;
        float speed;
        int misses;
        int allowedMisses;
        int openedFrame;
        float shakeTimer;
        PlayerController player;

        const float TrackWidth = 760f;

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;
            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(900f, 280f));

            var title = UIBuilder.CreateText(box, "Title", "Pick the lock", 36, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(800f, 50f));

            pinRow = UIBuilder.CreateRect("Pins", box);
            UIBuilder.Place(pinRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(600f, 40f));
            var layout = pinRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;

            var trackImage = UIBuilder.CreateImage(box, "Track", new Color(0f, 0f, 0f, 0.6f));
            track = trackImage.rectTransform;
            UIBuilder.Place(track, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(TrackWidth, 30f));
            zone = UIBuilder.CreateImage(track, "SweetSpot", skin.accentColor).rectTransform;
            zone.anchorMin = zone.anchorMax = new Vector2(0f, 0.5f);
            zone.pivot = new Vector2(0.5f, 0.5f);
            marker = UIBuilder.CreateImage(track, "Pick", Color.white).rectTransform;
            marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(8f, 54f);

            info = UIBuilder.CreateText(box, "Info", "", 24, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
            UIBuilder.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(860f, 40f));

            SetVisible(false);
        }

        public void Begin(LockpickLock lockToPick)
        {
            var gm = GameManager.Instance;
            player = PlayerController.Instance;
            if (gm == null || player == null || IsOpen) return;

            target = lockToPick;
            bool betterPicks = gm.HasKeyItem(LockpickLock.BetterPicksItem);
            allowedMisses = betterPicks ? 3 : 2;
            misses = 0;
            pinIndex = 0;

            foreach (var icon in pinIcons) Destroy(icon.gameObject);
            pinIcons.Clear();
            for (int i = 0; i < target.pins; i++)
            {
                var icon = UIBuilder.CreateImage(pinRow, "Pin", new Color(1f, 1f, 1f, 0.25f));
                icon.rectTransform.sizeDelta = new Vector2(24f, 36f);
                pinIcons.Add(icon);
            }

            gm.SetState(GameState.Lockpicking);
            player.Freeze();
            player.PlayerHealth.Health.Damaged += OnPlayerHurt;
            openedFrame = Time.frameCount;
            IsOpen = true;
            SetVisible(true);
            NextPin(betterPicks);
        }

        void NextPin(bool betterPicks)
        {
            zoneWidth = target.sweetSpot * (betterPicks ? 1.5f : 1f) * Mathf.Max(0.45f, 1f - 0.1f * pinIndex);
            zoneCenter = Random.Range(zoneWidth * 0.5f + 0.05f, 1f - zoneWidth * 0.5f - 0.05f);
            speed = 0.9f * target.speed * (1f + 0.12f * pinIndex);
            position = 0f;
            direction = 1;
            zone.sizeDelta = new Vector2(zoneWidth * TrackWidth, 30f);
            zone.anchoredPosition = new Vector2(zoneCenter * TrackWidth, 0f);
            UpdateInfo();
        }

        void UpdateInfo()
        {
            var input = InputReader.Instance;
            string key = input != null ? input.PromptFor("Interact") : "E";
            int left = allowedMisses - misses;
            info.text = $"Press {key} in the gold zone.   Pick strain: {new string('|', misses)}{new string('.', Mathf.Max(0, left))}   Esc / B: stop";
        }

        void Update()
        {
            if (!IsOpen || target == null) return;
            float dt = Time.unscaledDeltaTime;
            position += direction * speed * dt;
            if (position >= 1f)
            {
                position = 1f;
                direction = -1;
            }
            else if (position <= 0f)
            {
                position = 0f;
                direction = 1;
            }
            Vector2 shake = shakeTimer > 0f ? Random.insideUnitCircle * 6f : Vector2.zero;
            if (shakeTimer > 0f) shakeTimer -= dt;
            marker.anchoredPosition = new Vector2(position * TrackWidth, 0f) + shake;

            var input = InputReader.Instance;
            if (input == null || Time.frameCount == openedFrame) return;
            if (input.CancelPressed || input.PausePressed)
            {
                End(false, false);
                return;
            }
            if (!input.ConfirmPressed) return;

            if (Mathf.Abs(position - zoneCenter) <= zoneWidth * 0.5f)
            {
                AudioManager.PlaySfx(AudioIds.LockpickPin);
                pinIcons[pinIndex].color = UIBuilder.Skin.accentColor;
                pinIndex++;
                if (pinIndex >= target.pins)
                {
                    End(true, false);
                    return;
                }
                var gm = GameManager.Instance;
                NextPin(gm != null && gm.HasKeyItem(LockpickLock.BetterPicksItem));
            }
            else
            {
                misses++;
                shakeTimer = 0.2f;
                if (misses >= allowedMisses)
                {
                    AudioManager.PlaySfx(AudioIds.LockpickBreak);
                    GameEvents.RaiseToast("The pick snapped.");
                    End(false, true);
                    return;
                }
                AudioManager.PlaySfx(AudioIds.LockpickSlip);
                UpdateInfo();
            }
        }

        void OnPlayerHurt(DamageInfo info)
        {
            if (IsOpen) End(false, false);
        }

        void End(bool success, bool broken)
        {
            IsOpen = false;
            SetVisible(false);
            if (player != null)
            {
                player.PlayerHealth.Health.Damaged -= OnPlayerHurt;
                player.Unfreeze();
            }
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Lockpicking) gm.SetState(GameState.Playing);
            if (InputReader.Instance != null) InputReader.Instance.BlockMenuInput(2);
            var lockTarget = target;
            target = null;
            if (lockTarget == null) return;
            if (success) lockTarget.Unlock();
            else if (broken) lockTarget.OnPickBroken();
        }
    }
}
