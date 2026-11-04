using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Key rebinding screen: one row per action with a keyboard and a gamepad column.
    /// Select a cell and press the new key/button (Esc cancels). Conflicts swap automatically.
    /// Opens from the pause menu and from Settings on the title screen.
    /// </summary>
    public class ControlsMenu : OverlayPanel
    {
        class Row
        {
            public string action;
            public string part;
            public Button keyboard;
            public Button gamepad;
            public Text keyboardLabel;
            public Text gamepadLabel;
        }

        const float RowHeight = 60f;

        readonly List<Row> rows = new List<Row>();
        Text hint;
        Button resetButton;
        Button backButton;
        bool waiting;

        protected override void OnBuild()
        {
            var skin = UIBuilder.Skin;
            var dim = UIBuilder.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.75f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var box = UIBuilder.CreatePanel(transform, "Box").rectTransform;
            UIBuilder.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 1000f));

            var title = UIBuilder.CreateText(box, "Title", "Controls", 56, TextAnchor.MiddleCenter, skin.accentColor);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(800f, 70f));

            Header(box, "Action", 80f, TextAnchor.MiddleLeft);
            Header(box, "Keyboard", 640f, TextAnchor.MiddleCenter);
            Header(box, "Gamepad", 960f, TextAnchor.MiddleCenter);

            int index = 0;
            foreach (var part in InputReader.MoveParts) AddRow(box, index++, "Move", part, "Move " + part);
            foreach (var action in InputReader.RebindableActions) AddRow(box, index++, action, null, action);

            hint = UIBuilder.CreateText(box, "Hint", "", 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f));
            UIBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1100f, 40f));

            var size = new Vector2(360f, 70f);
            resetButton = UIBuilder.CreateButton(box, "Reset", "Reset to Defaults", OnReset, size);
            UIBuilder.Place((RectTransform)resetButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 34f), size);
            backButton = UIBuilder.CreateButton(box, "Back", "Back", RequestClose, size);
            UIBuilder.Place((RectTransform)backButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 34f), size);

            WireNavigation();
            SetVisible(false);
        }

        static void Header(RectTransform box, string text, float x, TextAnchor anchor)
        {
            var t = UIBuilder.CreateText(box, "Header " + text, text, 30, anchor, UIBuilder.Skin.accentColor);
            UIBuilder.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(anchor == TextAnchor.MiddleLeft ? 0f : 0.5f, 0.5f),
                new Vector2(x, -120f), new Vector2(anchor == TextAnchor.MiddleLeft ? 400f : 280f, 44f));
        }

        void AddRow(RectTransform box, int index, string action, string part, string label)
        {
            float y = -175f - index * RowHeight;
            var name = UIBuilder.CreateText(box, "Label " + label, label, 30, TextAnchor.MiddleLeft);
            UIBuilder.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(80f, y), new Vector2(420f, RowHeight));

            var row = new Row { action = action, part = part };
            var cell = new Vector2(280f, RowHeight - 10f);

            row.keyboard = UIBuilder.CreateButton(box, "Keyboard " + label, "", () => Rebind(row, InputReader.KeyboardGroup), cell);
            UIBuilder.Place((RectTransform)row.keyboard.transform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(640f, y), cell);
            row.keyboardLabel = row.keyboard.GetComponentInChildren<Text>();
            row.keyboardLabel.fontSize = 28;

            if (part == null)
            {
                row.gamepad = UIBuilder.CreateButton(box, "Gamepad " + label, "", () => Rebind(row, InputReader.GamepadGroup), cell);
                UIBuilder.Place((RectTransform)row.gamepad.transform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(960f, y), cell);
                row.gamepadLabel = row.gamepad.GetComponentInChildren<Text>();
                row.gamepadLabel.fontSize = 28;
            }
            else
            {
                // Movement on a gamepad is always the stick and D-pad.
                var fixedText = UIBuilder.CreateText(box, "Gamepad " + label, index == 0 ? "Left Stick / D-Pad" : "", 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f));
                UIBuilder.Place(fixedText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(960f, y), cell);
            }
            rows.Add(row);
        }

        void WireNavigation()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var up = i > 0 ? rows[i - 1].keyboard : (Selectable)backButton;
                var down = i < rows.Count - 1 ? rows[i + 1].keyboard : (Selectable)resetButton;
                r.keyboard.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = up,
                    selectOnDown = down,
                    selectOnRight = r.gamepad != null ? r.gamepad : null
                };
                if (r.gamepad != null)
                {
                    r.gamepad.navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnUp = FindGamepad(i, -1) ?? (Selectable)r.keyboard,
                        selectOnDown = FindGamepad(i, 1) ?? (Selectable)backButton,
                        selectOnLeft = r.keyboard
                    };
                }
            }
            resetButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = rows[rows.Count - 1].keyboard,
                selectOnRight = backButton,
                selectOnDown = rows[0].keyboard
            };
            backButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = rows[rows.Count - 1].gamepad != null ? rows[rows.Count - 1].gamepad : rows[rows.Count - 1].keyboard,
                selectOnLeft = resetButton,
                selectOnDown = rows[0].keyboard
            };
        }

        Selectable FindGamepad(int from, int step)
        {
            for (int i = from + step; i >= 0 && i < rows.Count; i += step)
                if (rows[i].gamepad != null) return rows[i].gamepad;
            return null;
        }

        protected override void OnOpened()
        {
            Refresh();
            hint.text = "Select a control and press the new key or button. Esc cancels.";
            if (InputReader.Instance != null) InputReader.Instance.BindingsChanged += Refresh;
            UIManager.Select(rows[0].keyboard.gameObject);
        }

        protected override void OnClosed()
        {
            var input = InputReader.Instance;
            if (input == null) return;
            input.BindingsChanged -= Refresh;
            input.CancelRebind();
            waiting = false;
            SetNavigation(true);
        }

        void Refresh()
        {
            var input = InputReader.Instance;
            if (input == null) return;
            foreach (var r in rows)
            {
                r.keyboardLabel.text = input.GetBindingName(r.action, InputReader.KeyboardGroup, r.part);
                if (r.gamepadLabel != null) r.gamepadLabel.text = input.GetBindingName(r.action, InputReader.GamepadGroup, r.part);
            }
        }

        void Rebind(Row row, string group)
        {
            var input = InputReader.Instance;
            if (input == null || waiting) return;
            bool keyboard = group == InputReader.KeyboardGroup;
            var label = keyboard ? row.keyboardLabel : row.gamepadLabel;
            var button = keyboard ? row.keyboard : row.gamepad;

            waiting = true;
            label.text = keyboard ? "Press a key..." : "Press a button...";
            hint.text = "Waiting for input. Esc cancels.";
            SetNavigation(false);

            bool started = input.StartRebind(row.action, group, row.part, ok =>
            {
                waiting = false;
                Refresh();
                hint.text = ok ? "Saved." : "Cancelled.";
                AudioManager.PlaySfx(ok ? AudioIds.UiConfirm : AudioIds.UiBack);
                StartCoroutine(RestoreNavigation(button));
            });
            if (!started)
            {
                waiting = false;
                Refresh();
                SetNavigation(true);
            }
        }

        IEnumerator RestoreNavigation(Button reselect)
        {
            // Wait a moment so the key that was just bound doesn't also press a menu button.
            yield return new WaitForSecondsRealtime(0.2f);
            SetNavigation(true);
            if (IsOpen && reselect != null) UIManager.Select(reselect.gameObject);
        }

        static void SetNavigation(bool enabled)
        {
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = enabled;
        }

        void OnReset()
        {
            var input = InputReader.Instance;
            if (input == null || waiting) return;
            input.ResetBindings();
            Refresh();
            hint.text = "Controls reset to defaults.";
        }
    }
}
