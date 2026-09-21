using ValheimTooler.Configuration;
using ValheimTooler.Core;
using ValheimTooler.Utils;
using UnityEngine;

namespace ValheimTooler.UI
{
    public static class Controls
    {
        private const int TooltipWindowId = 19991;
        private static string s_tooltipText = "";
        private static Vector2 s_tooltipScreenPos;
        private static float s_tooltipWidth;
        private static float s_tooltipHeight;
        private static Rect s_tooltipRect;
        private static GUIStyle s_statusOk;
        private static GUIStyle s_statusWarn;
        private static string s_hoveredAction = "";
        private static string s_confirmTitle = "";
        private static string s_confirmMessage = "";
        private static System.Action s_confirmAction;
        private static Rect s_confirmRect;

        public static void BeginSection(string titleCode)
        {
            GUILayout.Space(10);
            GUILayout.Label(VTLocalization.instance.Localize(titleCode), StyleOrDefault("sectionTitle"));
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        }

        public static void EndSection()
        {
            GUILayout.EndVertical();
        }

        public static bool FeatureButton(string labelCode, bool on, FeatureMethod method, KeyboardShortcut? shortcut = null)
        {
            GUILayout.BeginHorizontal();
            string tip = Tip(labelCode);
            bool clicked = GUILayout.Button(
                new GUIContent(Utils.ToggleButtonLabel(labelCode, on, shortcut), tip),
                StyleOrDefault(on ? "featureOn" : "featureOff"),
                GUILayout.MinHeight(28),
                GUILayout.ExpandWidth(true));
            NoteHoverTooltip(tip);
            NoteHoverAction(labelCode);
            GUILayout.Label(
                MethodLabel(method),
                StyleOrDefault(method == FeatureMethod.DevCommands ? "badgeDev" : "badgeDirect"),
                GUILayout.Width(108),
                GUILayout.MinHeight(28));
            NoteHoverAction(labelCode);
            GUILayout.EndHorizontal();
            return clicked;
        }

        public static bool ActionButton(string labelCode, FeatureMethod method, KeyboardShortcut? shortcut = null)
        {
            GUILayout.BeginHorizontal();
            string tip = Tip(labelCode);
            bool clicked = GUILayout.Button(
                new GUIContent(Utils.LabelWithShortcut(labelCode, shortcut), tip),
                StyleOrDefault("actionButton"),
                GUILayout.MinHeight(28),
                GUILayout.ExpandWidth(true));
            NoteHoverTooltip(tip);
            NoteHoverAction(labelCode);
            GUILayout.Label(
                MethodLabel(method),
                StyleOrDefault(method == FeatureMethod.DevCommands ? "badgeDev" : "badgeDirect"),
                GUILayout.Width(108),
                GUILayout.MinHeight(28));
            GUILayout.EndHorizontal();
            return clicked;
        }

        public static bool ActionButtonAt(Rect rect, string labelCode, FeatureMethod method)
        {
            const float badgeWidth = 108f;
            const float gap = 4f;
            float buttonWidth = Mathf.Max(8f, rect.width - badgeWidth - gap);
            Rect buttonRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Rect badgeRect = new Rect(rect.x + buttonWidth + gap, rect.y, badgeWidth, rect.height);

            string tip = Tip(labelCode);
            bool clicked = GUI.Button(buttonRect, new GUIContent(Utils.LabelWithShortcut(labelCode, null), tip), StyleOrDefault("actionButton"));
            if (Event.current != null && Event.current.type == EventType.Repaint && buttonRect.Contains(Event.current.mousePosition))
            {
                SetHoverTooltip(tip);
                s_hoveredAction = labelCode;
            }

            GUI.Label(badgeRect, MethodLabel(method), StyleOrDefault(method == FeatureMethod.DevCommands ? "badgeDev" : "badgeDirect"));
            return clicked;
        }

        public static bool ActionButtonNarrow(string labelCode, FeatureMethod method, float width = 180f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            string tip = Tip(labelCode);
            bool clicked = GUILayout.Button(
                new GUIContent(Utils.LabelWithShortcut(labelCode, null), tip),
                GUI.skin.button,
                GUILayout.Width(width),
                GUILayout.Height(28));
            NoteHoverTooltip(tip);
            NoteHoverAction(labelCode);
            GUILayout.Label(
                MethodLabel(method),
                StyleOrDefault(method == FeatureMethod.DevCommands ? "badgeDev" : "badgeDirect"),
                GUILayout.Width(108),
                GUILayout.Height(28));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return clicked;
        }

        public static string HoveredAction => s_hoveredAction;

        public static void AskConfirm(string titleCode, string messageCode, System.Action onConfirm)
        {
            s_confirmTitle = VTLocalization.instance.Localize(titleCode);
            s_confirmMessage = VTLocalization.instance.Localize(messageCode);
            s_confirmAction = onConfirm;
        }

        public static bool HasConfirmDialog => s_confirmAction != null;

        public static bool IsPointerOverConfirm(Vector2 guiMouse)
        {
            return s_confirmAction != null && s_confirmRect.Contains(guiMouse);
        }

        public static void DrawConfirmDialog()
        {
            if (s_confirmAction == null)
            {
                return;
            }

            Event ev = Event.current;
            if (ev != null && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            {
                s_confirmAction = null;
                ev.Use();
                return;
            }

            float width = 440f;
            float height = 170f;
            s_confirmRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.ModalWindow(19992, s_confirmRect, DrawConfirmWindow, s_confirmTitle);
        }

        private static void DrawConfirmWindow(int id)
        {
            GUILayout.Space(8);
            GUILayout.Label(s_confirmMessage, StyleOrDefault("hint"));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(VTLocalization.instance.Localize("$vt_confirm_yes"), GUILayout.Width(120), GUILayout.Height(28)))
            {
                System.Action action = s_confirmAction;
                s_confirmAction = null;
                action?.Invoke();
            }
            if (GUILayout.Button(VTLocalization.instance.Localize("$vt_confirm_no"), GUILayout.Width(120), GUILayout.Height(28)))
            {
                s_confirmAction = null;
            }
            GUILayout.EndHorizontal();
        }

        public static FeatureMethod MethodPicker(FeatureMethod current, string labelCode = null)
        {
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(labelCode))
            {
                GUILayout.Label(VTLocalization.instance.Localize(labelCode), GUILayout.ExpandWidth(false));
            }
            GUILayout.Space(4);
            string directTip = Tip("$vt_method_direct");
            if (GUILayout.Button(new GUIContent(VTLocalization.instance.Localize("$vt_method_direct"), directTip),
                StyleOrDefault(current == FeatureMethod.Direct ? "methodPickOn" : "methodPick"),
                GUILayout.Width(108),
                GUILayout.Height(26)))
            {
                current = FeatureMethod.Direct;
            }
            NoteHoverTooltip(directTip);

            string devTip = Tip("$vt_method_devcommands");
            if (GUILayout.Button(new GUIContent(VTLocalization.instance.Localize("$vt_method_devcommands"), devTip),
                StyleOrDefault(current == FeatureMethod.DevCommands ? "methodPickDevOn" : "methodPick"),
                GUILayout.Width(132),
                GUILayout.Height(26)))
            {
                current = FeatureMethod.DevCommands;
            }
            NoteHoverTooltip(devTip);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return current;
        }

        public static void Hint(string labelCode)
        {
            GUILayout.Label(VTLocalization.instance.Localize(labelCode), StyleOrDefault("hint"));
        }

        public static void FieldLabel(string labelCode, bool colon = true)
        {
            string tip = Tip(labelCode);
            string text = VTLocalization.instance.Localize(labelCode);
            if (colon && !text.EndsWith(":") && !text.EndsWith(" :"))
            {
                text += " :";
            }

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.wordWrap = false;
            style.stretchWidth = false;
            style.clipping = TextClipping.Overflow;
            GUILayout.Label(new GUIContent(text, tip), style, GUILayout.ExpandWidth(false));
            NoteHoverTooltip(tip);
        }

        public static void HoverLabel(string labelCode, GUIStyle style = null, params GUILayoutOption[] options)
        {
            string tip = Tip(labelCode);
            GUIContent content = new GUIContent(VTLocalization.instance.Localize(labelCode), tip);
            if (style != null)
            {
                GUILayout.Label(content, style, options);
            }
            else
            {
                GUILayout.Label(content, options);
            }
            NoteHoverTooltip(tip);
        }

        public static void DrawInfoButton(float x, float y)
        {
            Rect rect = new Rect(x, y, 22f, 22f);
            GUIStyle style = StyleOrDefault("infoButton");
            GUI.Label(rect, "i", style);
            if (Event.current != null && Event.current.type == EventType.Repaint && rect.Contains(Event.current.mousePosition))
            {
                SetHoverTooltip(VTLocalization.instance.Localize("$vt_method_legend"));
            }
        }

        public static bool LabeledToggle(string labelCode, bool value)
        {
            string tip = Tip(labelCode);
            GUILayout.BeginHorizontal();
            bool next = GUILayout.Toggle(value, new GUIContent("", tip));
            NoteHoverTooltip(tip);
            GUILayout.Label(new GUIContent(VTLocalization.instance.Localize(labelCode), tip));
            NoteHoverTooltip(tip);
            GUILayout.EndHorizontal();
            return next;
        }

        public static float LabeledSlider(string labelCode, float value, float min, float max, string valueText, string hoverAction = null)
        {
            string tip = Tip(labelCode);
            GUILayout.Label(new GUIContent(VTLocalization.instance.Localize(labelCode) + " (" + valueText + ")", tip), GUILayout.MinWidth(200));
            NoteHoverTooltip(tip);
            if (hoverAction != null)
            {
                NoteHoverAction(hoverAction);
            }
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            NoteHoverTooltip(tip);
            if (hoverAction != null)
            {
                NoteHoverAction(hoverAction);
            }
            return next;
        }

        public static GUIStyle StatusStyle(bool warning)
        {
            if (s_statusOk == null)
            {
                s_statusOk = new GUIStyle(StyleOrDefault("badgeDirect"))
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 13
                };
                s_statusWarn = new GUIStyle(StyleOrDefault("badgeDev"))
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 13
                };
            }

            return warning ? s_statusWarn : s_statusOk;
        }

        public static bool IsPointerOverTooltip(Vector2 guiMouse)
        {
            return !string.IsNullOrEmpty(s_tooltipText) && s_tooltipRect.Contains(guiMouse);
        }

        public static void PrepareTooltipPass()
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                s_tooltipText = "";
                s_hoveredAction = "";
            }
        }

        public static void ClearCoveredHover()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            s_tooltipText = "";
            s_hoveredAction = "";
        }

        public static void SetHoverTooltip(string tip)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || string.IsNullOrEmpty(tip))
            {
                return;
            }

            s_tooltipText = tip;
            s_tooltipScreenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        }

        public static void NoteHoverTooltip(string tip)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || string.IsNullOrEmpty(tip))
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                return;
            }

            SetHoverTooltip(tip);
        }

        private static string s_dragAction;

        public static void NoteHoverAction(string labelCode)
        {
            if (Event.current == null || string.IsNullOrEmpty(labelCode))
            {
                return;
            }

            Event ev = Event.current;
            if (ev.type == EventType.MouseUp)
            {
                s_dragAction = null;
            }

            Rect last = GUILayoutUtility.GetLastRect();
            if (last.Contains(ev.mousePosition))
            {
                s_hoveredAction = labelCode;
                if (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag || Input.GetMouseButton(0))
                {
                    s_dragAction = labelCode;
                }
            }
            else if (s_dragAction == labelCode && Input.GetMouseButton(0))
            {
                s_hoveredAction = labelCode;
            }
        }

        public static void DrawOverlayTooltip()
        {
            if (string.IsNullOrEmpty(s_tooltipText))
            {
                return;
            }

            GUIStyle style = StyleOrDefault("tooltip");
            GUIContent content = new GUIContent(s_tooltipText);
            s_tooltipWidth = Mathf.Min(380f, style.CalcSize(content).x + 20f);
            s_tooltipHeight = style.CalcHeight(content, s_tooltipWidth - 16f) + 14f;

            float x = s_tooltipScreenPos.x + 24f;
            float y = s_tooltipScreenPos.y + 8f;
            if (x + s_tooltipWidth > Screen.width)
            {
                x = s_tooltipScreenPos.x - s_tooltipWidth - 16f;
            }
            if (y + s_tooltipHeight > Screen.height)
            {
                y = s_tooltipScreenPos.y - s_tooltipHeight - 10f;
            }
            if (x < 4f)
            {
                x = 4f;
            }
            if (y < 4f)
            {
                y = 4f;
            }

            Rect rect = new Rect(x, y, s_tooltipWidth, s_tooltipHeight);
            s_tooltipRect = rect;
            GUI.Window(TooltipWindowId, rect, DrawTooltipWindow, GUIContent.none, style);
            GUI.BringWindowToFront(TooltipWindowId);
        }

        private static void DrawTooltipWindow(int id)
        {
            EntryPoint.HandleToggleHotkey();

            GUIStyle style = StyleOrDefault("tooltip");
            GUI.Label(new Rect(0, 0, s_tooltipWidth, s_tooltipHeight), s_tooltipText, style);
        }

        public static string Tip(string labelCode)
        {
            if (string.IsNullOrEmpty(labelCode))
            {
                return "";
            }

            string tip = VTLocalization.instance.Localize(labelCode + "_tip");
            if (string.IsNullOrEmpty(tip) || (tip.Length > 2 && tip[0] == '[' && tip[tip.Length - 1] == ']'))
            {
                return "";
            }

            return tip;
        }

        private static string MethodLabel(FeatureMethod method)
        {
            return VTLocalization.instance.Localize(method == FeatureMethod.DevCommands ? "$vt_method_devcommands" : "$vt_method_direct");
        }

        private static GUIStyle StyleOrDefault(string name)
        {
            GUIStyle style = InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.FindStyle(name) : null;
            return style != null ? style : GUI.skin.box;
        }
    }
}
