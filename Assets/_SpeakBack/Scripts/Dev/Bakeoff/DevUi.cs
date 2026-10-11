using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uprise.SpeakBack.UI;

namespace Uprise.SpeakBack.Dev.Bakeoff
{
    /// <summary>
    /// Tiny code-built uGUI kit for dev screens: big, plain and readable, using the Master §31 palette.
    /// Built at runtime so the dev scene needs no hand-authored layout. Never used by release code.
    /// </summary>
    internal static class DevUi
    {
        public static readonly Color Background = Hex(0x0F151D);
        public static readonly Color Surface = Hex(0x1A2330);
        public static readonly Color SurfaceRaised = Hex(0x243041);
        public static readonly Color Text = Hex(0xEEF3F8);
        public static readonly Color Steel = Hex(0x8C99AB);
        public static readonly Color Accent = Hex(0x22D3EE);
        public static readonly Color Danger = Hex(0xFF4D5E);

        public static RectTransform Screen(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = Background;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 20;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static RectTransform Row(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, float height, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color ?? Text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        public static Button Button(Transform parent, string text, Action onClick, float height = 120f, Color? color = null)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color ?? SurfaceRaised;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() => onClick());

            var label = Label(go.transform, text, 40, height, color.HasValue ? Background : Text);
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);
            return button;
        }

        public static void SetText(Button button, string text) => button.GetComponentInChildren<TextMeshProUGUI>().text = text;

        public static TMP_InputField Input(Transform parent, string placeholder)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 110;
            go.GetComponent<Image>().color = Surface;
            var input = go.GetComponent<TMP_InputField>();
            input.pointSize = 44;
            input.textComponent.color = Text;
            input.characterLimit = 24;
            input.lineType = TMP_InputField.LineType.SingleLine;
            var hint = (TMP_Text)input.placeholder;
            hint.text = placeholder;
            hint.color = Steel;
            return input;
        }

        /// <summary>A button that cycles through <paramref name="options"/> on each tap; index -1 shows <paramref name="prompt"/>.</summary>
        public static Func<int> Cycle(Transform parent, string prompt, IReadOnlyList<string> options, int initial, out Action reset)
        {
            var index = initial;
            Button button = null;
            void Refresh() => SetText(button, index < 0 ? prompt + " (tap to choose)" : $"{prompt}: {options[index]}");
            button = Button(parent, string.Empty, () =>
            {
                index = (index + 1) % options.Count;
                Refresh();
            });
            Refresh();
            reset = () =>
            {
                index = initial;
                Refresh();
            };
            return () => index;
        }

        public static HoldRecordButton HoldButton(Transform parent, out TextMeshProUGUI label)
        {
            var go = new GameObject("HoldRecordButton", typeof(RectTransform), typeof(Image), typeof(HoldRecordButton), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Accent;
            go.GetComponent<LayoutElement>().preferredHeight = 260;
            label = Label(go.transform, "Hold to record", 52, 260, Background);
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);
            return go.GetComponent<HoldRecordButton>();
        }

        public static Image Meter(Transform parent, out Image fill)
        {
            var go = new GameObject("Meter", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Surface;
            go.GetComponent<LayoutElement>().preferredHeight = 36;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            fill = fillGo.GetComponent<Image>();
            fill.color = Accent;
            var rect = (RectTransform)fillGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }

        public static void SetMeter(Image fill, float level)
        {
            ((RectTransform)fill.transform).anchorMax = new Vector2(Mathf.Clamp01(level), 1f);
        }

        public static void Spacer(Transform parent)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleHeight = 1;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16, 0);
            rect.offsetMax = new Vector2(-16, 0);
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
