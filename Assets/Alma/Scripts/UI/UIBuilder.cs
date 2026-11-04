using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Alma
{
    /// <summary>
    /// Helpers that build uGUI elements in code. The placeholder UI uses them, so no UI prefabs
    /// are needed yet. When you design the real UI, replace these panels with prefabs.
    /// </summary>
    public static class UIBuilder
    {
        static Font builtinFont;

        public static UISkin Skin => UISkin.Get();

        public static Font Font
        {
            get
            {
                if (Skin.font != null) return Skin.font;
                if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return builtinFont;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Anchors at one point. Anchor and pivot use 0..1 coordinates (0,0 = bottom-left).</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        public static Image CreatePanel(Transform parent, string name)
        {
            var image = CreateImage(parent, name, Skin.panelColor, Skin.panelSprite);
            image.raycastTarget = true;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            var rt = CreateRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color ?? Skin.textColor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, Vector2 size)
        {
            var image = CreateImage(parent, name, Color.white, Skin.buttonSprite);
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = size;
            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Skin.buttonColor;
            colors.highlightedColor = Skin.buttonHighlightColor;
            colors.selectedColor = Skin.buttonHighlightColor;
            colors.pressedColor = Skin.accentColor;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(() => AudioManager.PlaySfx(AudioIds.UiConfirm));

            var text = CreateText(image.transform, "Label", label, 34);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>An item slot: icon on top, label below. Selectable for gamepad navigation.</summary>
        public static Button CreateSlot(Transform parent, string name, Sprite icon, string label, Vector2 size, bool unlocked)
        {
            var bg = CreateImage(parent, name, Color.white, Skin.slotSprite != null ? Skin.slotSprite : Skin.buttonSprite);
            bg.raycastTarget = true;
            bg.rectTransform.sizeDelta = size;
            var layout = bg.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            var button = bg.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Skin.buttonColor;
            colors.highlightedColor = Skin.buttonHighlightColor;
            colors.selectedColor = Skin.accentColor;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.targetGraphic = bg;

            var iconImage = CreateImage(bg.transform, "Icon", unlocked ? Color.white : new Color(1f, 1f, 1f, 0.25f), icon);
            iconImage.preserveAspect = true;
            Place(iconImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(size.x - 40f, size.y - 70f));
            if (icon == null) iconImage.color = unlocked ? new Color(1f, 1f, 1f, 0.15f) : new Color(1f, 1f, 1f, 0.05f);

            var text = CreateText(bg.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(size.x - 10f, 48f));
            return button;
        }

        public static Slider CreateSlider(Transform parent, string name, string label, float value, UnityAction<float> onChanged)
        {
            var row = CreateRect(name, parent);
            row.sizeDelta = new Vector2(620f, 64f);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 620f;
            layout.preferredHeight = 64f;

            var labelText = CreateText(row, "Label", label, 30, TextAnchor.MiddleLeft);
            Place(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(220f, 60f));

            var sliderRt = CreateRect("Slider", row);
            Place(sliderRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(380f, 28f));
            var background = sliderRt.gameObject.AddComponent<Image>();
            background.color = Skin.buttonColor;

            var fillArea = CreateRect("Fill Area", sliderRt);
            Stretch(fillArea);
            var fill = CreateImage(fillArea, "Fill", Skin.accentColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = CreateRect("Handle Slide Area", sliderRt);
            Stretch(handleArea);
            var handle = CreateImage(handleArea, "Handle", Skin.textColor);
            handle.rectTransform.sizeDelta = new Vector2(22f, 40f);

            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            var colors = slider.colors;
            colors.normalColor = Skin.textColor;
            colors.highlightedColor = Skin.accentColor;
            colors.selectedColor = Skin.accentColor;
            slider.colors = colors;
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing, int padding = 0)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Wires up/down navigation between selectables in order (for gamepad menus).</summary>
        public static void ChainNavigation(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length]
                };
                if (items[i] is Slider)
                {
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;
                }
                items[i].navigation = nav;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => builtinFont = null;
    }
}
