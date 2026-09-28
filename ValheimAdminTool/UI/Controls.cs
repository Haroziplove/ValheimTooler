using ValheimAdminTool.Configuration;
using ValheimAdminTool.Core;
using ValheimAdminTool.Utils;
using UnityEngine;

namespace ValheimAdminTool.UI
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

        public static void BeginSection(string titleCode, string helpCode = null)
        {
            GUILayout.Space(10);
            if (string.IsNullOrEmpty(helpCode))
            {
                GUILayout.Label(VTLocalization.instance.Localize(titleCode), StyleOrDefault("sectionTitle"));
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(VTLocalization.instance.Localize(titleCode), StyleOrDefault("sectionTitle"), GUILayout.ExpandWidth(false));
                HelpMark(helpCode);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        }

        public static void HelpMark(string textCode)
        {
            GUILayout.Label("?", StyleOrDefault("infoButton"), GUILayout.Width(20f), GUILayout.Height(20f));
            NoteHoverTooltip(VTLocalization.instance.Localize(textCode));
        }

        public static void EndSection()
        {
            GUILayout.EndVertical();
        }

        public static bool FeatureButton(string labelCode, bool on, FeatureMethod method, KeyboardShortcut? shortcut = null, bool usesRadius = false, bool showMethod = false)
        {
            GUILayout.BeginHorizontal();
            if (usesRadius)
            {
                RadiusMark(28f);
            }
            string tip = Tip(labelCode);
            bool clicked = GUILayout.Button(
                new GUIContent(Utils.ToggleButtonLabel(labelCode, on, shortcut), tip),
                StyleOrDefault(on ? "featureOn" : "featureOff"),
                GUILayout.MinHeight(28),
                GUILayout.ExpandWidth(true));
            NoteHoverTooltip(tip);
            NoteHoverAction(labelCode);
            if (showMethod || method == FeatureMethod.DevCommands)
            {
                GUILayout.Label(
                    MethodLabel(method),
                    StyleOrDefault(method == FeatureMethod.DevCommands ? "badgeDev" : "badgeDirect"),
                    GUILayout.Width(108),
                    GUILayout.MinHeight(28));
                NoteHoverAction(labelCode);
            }
            GUILayout.EndHorizontal();
            return clicked;
        }

        public static bool ActionButton(string labelCode, FeatureMethod method, KeyboardShortcut? shortcut = null, bool usesRadius = false)
        {
            GUILayout.BeginHorizontal();
            if (usesRadius)
            {
                RadiusMark(28f);
            }
            string tip = Tip(labelCode);
            bool clicked = GUILayout.Button(
                new GUIContent(Utils.LabelWithShortcut(labelCode, shortcut), tip),
                StyleOrDefault("actionButton"),
                GUILayout.MinHeight(28),
                GUILayout.ExpandWidth(true));
            NoteHoverTooltip(tip);
            NoteHoverAction(labelCode);
            if (method == FeatureMethod.DevCommands)
            {
                GUILayout.Label(
                    MethodLabel(method),
                    StyleOrDefault("badgeDev"),
                    GUILayout.Width(108),
                    GUILayout.MinHeight(28));
            }
            GUILayout.EndHorizontal();
            return clicked;
        }

        public static bool ActionButtonAt(Rect rect, string labelCode, FeatureMethod method, string tipCode = null)
        {
            string tip = Tip(string.IsNullOrEmpty(tipCode) ? labelCode : tipCode);
            bool showBadge = method == FeatureMethod.DevCommands;
            const float badgeWidth = 108f;
            const float gap = 4f;
            float buttonWidth = showBadge ? Mathf.Max(8f, rect.width - badgeWidth - gap) : rect.width;
            Rect buttonRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            bool clicked = GUI.Button(buttonRect, new GUIContent(Utils.LabelWithShortcut(labelCode, null), tip), StyleOrDefault("actionButton"));
            if (Event.current != null && Event.current.type == EventType.Repaint && buttonRect.Contains(Event.current.mousePosition))
            {
                SetHoverTooltip(tip);
                s_hoveredAction = labelCode;
            }

            if (showBadge)
            {
                Rect badgeRect = new Rect(rect.x + buttonWidth + gap, rect.y, badgeWidth, rect.height);
                GUI.Label(badgeRect, MethodLabel(method), StyleOrDefault("badgeDev"));
            }
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
            if (method == FeatureMethod.DevCommands)
            {
                GUILayout.Label(
                    MethodLabel(method),
                    StyleOrDefault("badgeDev"),
                    GUILayout.Width(108),
                    GUILayout.Height(28));
            }
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

        // The dialog is modal: while it is open, and until the click that closed it is released,
        // no mouse input may reach the game.
        public static bool ConfirmBlocksInput
        {
            get
            {
                if (s_confirmAction != null)
                {
                    return true;
                }

                if (s_confirmReleasePending && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                {
                    s_confirmReleasePending = false;
                }

                return s_confirmReleasePending;
            }
        }

        private static bool s_confirmReleasePending;

        private static void CloseConfirm()
        {
            s_confirmAction = null;
            s_confirmReleasePending = true;
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
                CloseConfirm();
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
                CloseConfirm();
                action?.Invoke();
            }
            if (GUILayout.Button(VTLocalization.instance.Localize("$vt_confirm_no"), GUILayout.Width(120), GUILayout.Height(28)))
            {
                CloseConfirm();
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

            if (s_fieldLabelStyle == null || s_fieldLabelSkin != GUI.skin)
            {
                s_fieldLabelSkin = GUI.skin;
                s_fieldLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = false,
                    stretchWidth = false,
                    clipping = TextClipping.Overflow
                };
            }
            GUILayout.Label(new GUIContent(text, tip), s_fieldLabelStyle, GUILayout.ExpandWidth(false));
            NoteHoverTooltip(tip);
        }

        private static GUIStyle s_fieldLabelStyle;
        private static GUISkin s_fieldLabelSkin;

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

        public static bool LabeledToggle(string labelCode, bool value, bool usesRadius = false)
        {
            string tip = Tip(labelCode);
            GUILayout.BeginHorizontal();
            if (usesRadius)
            {
                RadiusMark(18f);
            }
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
            if (EntryPoint.s_passThroughInput || Event.current == null || Event.current.type != EventType.Repaint || string.IsNullOrEmpty(tip))
            {
                return;
            }

            s_tooltipText = tip;
            // The real cursor position. GUIToScreenPoint drifts inside scroll views and under the
            // UI-scale matrix, which put item giver and recipe grid tooltips in odd places.
            s_tooltipScreenPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
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
            if (EntryPoint.s_passThroughInput || Event.current == null || string.IsNullOrEmpty(labelCode))
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

        public static void RadiusNote(string text, string hoverAction = null)
        {
            string tip = Tip("$vt_action_radius_mark");
            GUILayout.BeginHorizontal();
            RadiusMark(22f);
            GUILayout.Label(new GUIContent(text, tip), GUILayout.MinHeight(22f));
            NoteHoverTooltip(tip);
            if (hoverAction != null)
            {
                NoteHoverAction(hoverAction);
            }
            GUILayout.EndHorizontal();
        }

        private static void RadiusMark(float rowHeight)
        {
            if (s_radiusMarkStyle == null)
            {
                s_radiusMarkStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageOnly,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(GUI.skin.button.margin.left, 2, GUI.skin.button.margin.top, GUI.skin.button.margin.bottom)
                };
            }

            s_radiusMarkStyle.fixedHeight = rowHeight;
            string tip = Tip("$vt_action_radius_mark");
            GUILayout.Label(new GUIContent(RadiusCircle(), tip), s_radiusMarkStyle, GUILayout.Width(18f), GUILayout.Height(rowHeight));
            NoteHoverTooltip(tip);
        }

        private static Texture2D RadiusCircle()
        {
            if (s_radiusCircle != null)
            {
                return s_radiusCircle;
            }

            const int size = 16;
            s_radiusCircle = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            float center = (size - 1) * 0.5f;
            float radius = center - 2f;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color ring = new Color(0.45f, 0.9f, 1f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1.6f - Mathf.Abs(distance - radius));
                    Color pixel = ring;
                    pixel.a = alpha;
                    s_radiusCircle.SetPixel(x, y, alpha > 0.05f ? pixel : clear);
                }
            }

            s_radiusCircle.Apply();
            return s_radiusCircle;
        }

        private static Texture2D s_radiusCircle;
        private static GUIStyle s_radiusMarkStyle;
        private static GUIStyle s_closeButtonStyle;
        private static GUISkin s_closeButtonSkin;

        private static readonly int s_toggleHint = "Toggle".GetHashCode();

        // Off-screen grid cells skip drawing but must still take the control ID GUI.Toggle would take.
        // Otherwise the Layout pass and the click pass number controls differently, and any popup
        // placed after the grid loses its selection.
        public static void SkipToggle(Rect rect)
        {
            GUIUtility.GetControlID(s_toggleHint, FocusType.Passive, rect);
        }

        public static GUIStyle CloseButtonStyle()
        {
            if (s_closeButtonStyle == null || s_closeButtonSkin != GUI.skin)
            {
                s_closeButtonSkin = GUI.skin;
                s_closeButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }

            return s_closeButtonStyle;
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
