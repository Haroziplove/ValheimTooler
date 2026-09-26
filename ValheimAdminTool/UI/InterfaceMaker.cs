using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.UI
{
    class InterfaceMaker
    {
        private static GUISkin s_customSkin;
        private static Font s_font;
        private static GUISkin s_oldSkin;
        private static readonly List<Texture2D> s_keptTextures = new List<Texture2D>();

        public static GUISkin CustomSkin
        {
            get
            {
                if (s_customSkin == null)
                {
                    try
                    {
                        s_customSkin = CreateSkin();
                    }
                    catch (Exception ex)
                    {
                        ZLog.Log("Could not load custom GUISkin - " + ex.Message);
                        s_customSkin = GUI.skin;
                    }
                }
                return s_customSkin;
            }
        }

        public static GUISkin OldSkin
        {
            get { return s_oldSkin; }
        }

        private static GUISkin CreateSkin()
        {
            s_oldSkin = GUI.skin;
            GUISkin guiskin = UnityEngine.Object.Instantiate(GUI.skin);
            UnityEngine.Object.DontDestroyOnLoad(guiskin);

            s_font = LoadFont();
            guiskin.font = s_font;

            Color windowBg = new Color(0.08f, 0.09f, 0.11f, 1f);
            Color windowBorder = new Color(0.82f, 0.90f, 0.95f, 1f);
            Color boxBg = new Color(0.14f, 0.16f, 0.19f, 1f);
            Color boxBorder = new Color(0.42f, 0.48f, 0.54f, 1f);
            Color buttonBg = new Color(0.24f, 0.26f, 0.31f, 1f);
            Color buttonHover = new Color(0.34f, 0.37f, 0.43f, 1f);
            Color buttonActive = new Color(0.10f, 0.45f, 0.40f, 1f);
            Color fieldBg = new Color(0.18f, 0.20f, 0.24f, 1f);
            Color fieldBorder = new Color(0.72f, 0.78f, 0.84f, 1f);
            Color sliderBg = new Color(0.10f, 0.11f, 0.13f, 1f);
            Color textMain = Color.white;
            Color textMuted = new Color(0.88f, 0.90f, 0.93f, 1f);
            Color directBg = new Color(0.08f, 0.42f, 0.46f, 1f);
            Color devBg = new Color(0.56f, 0.34f, 0.08f, 1f);
            Color idleBg = new Color(0.22f, 0.24f, 0.28f, 1f);
            Color onBg = new Color(0.08f, 0.40f, 0.36f, 1f);
            Color offBg = new Color(0.24f, 0.26f, 0.31f, 1f);

            Texture2D windowTex = BorderedTex(windowBg, windowBorder, 64, 2);
            Texture2D boxTex = BorderedTex(boxBg, boxBorder, 64, 1);
            Texture2D buttonTex = ColorTex(buttonBg);
            Texture2D buttonHoverTex = ColorTex(buttonHover);
            Texture2D buttonActiveTex = ColorTex(buttonActive);
            Texture2D fieldTex = BorderedTex(fieldBg, fieldBorder, 64, 2);
            Texture2D sliderTex = ColorTex(sliderBg);
            Texture2D toggleOffTex = ColorTex(idleBg);
            Texture2D toggleOnTex = ColorTex(buttonActive);
            Texture2D thumbTex = ColorTex(new Color(0.55f, 0.58f, 0.64f, 1f));

            ApplySolidBackground(guiskin.label, null, textMain);
            guiskin.label.font = s_font;
            guiskin.label.fontSize = 14;
            guiskin.label.fontStyle = FontStyle.Bold;
            guiskin.label.wordWrap = true;
            guiskin.label.normal.textColor = textMain;
            guiskin.label.hover.textColor = textMain;

            ApplySolidBackground(guiskin.box, boxTex, textMain);
            guiskin.box.font = s_font;
            guiskin.box.fontSize = 14;
            guiskin.box.fontStyle = FontStyle.Bold;
            guiskin.box.alignment = TextAnchor.UpperLeft;
            guiskin.box.padding = new RectOffset(10, 10, 10, 10);
            guiskin.box.border = new RectOffset(1, 1, 1, 1);

            ApplySolidBackground(guiskin.window, windowTex, textMain);
            guiskin.window.border = new RectOffset(2, 2, 2, 2);
            guiskin.window.padding = new RectOffset(14, 14, 36, 14);
            guiskin.window.font = s_font;
            guiskin.window.fontSize = 16;
            guiskin.window.fontStyle = FontStyle.Bold;
            guiskin.window.alignment = TextAnchor.UpperCenter;

            ApplyButton(guiskin.button, buttonTex, buttonHoverTex, buttonActiveTex, textMain);
            guiskin.button.font = s_font;
            guiskin.button.fontSize = 14;
            guiskin.button.fontStyle = FontStyle.Bold;
            guiskin.button.wordWrap = false;
            guiskin.button.padding = new RectOffset(10, 10, 7, 7);
            guiskin.button.border = new RectOffset(0, 0, 0, 0);

            ApplySolidBackground(guiskin.horizontalSliderThumb, thumbTex, textMain);
            guiskin.horizontalSliderThumb.hover.background = buttonHoverTex;
            ApplySolidBackground(guiskin.horizontalSlider, sliderTex, textMain);
            guiskin.horizontalSlider.border = new RectOffset(0, 0, 0, 0);

            ApplyToggle(guiskin.toggle, toggleOffTex, toggleOnTex);
            guiskin.toggle.normal.textColor = textMain;
            guiskin.toggle.onNormal.textColor = textMain;
            guiskin.toggle.border = new RectOffset(0, 0, 0, 0);
            guiskin.toggle.overflow = new RectOffset(0, 0, 0, 0);
            guiskin.toggle.imagePosition = ImagePosition.ImageOnly;
            guiskin.toggle.padding = new RectOffset(0, 0, 0, 0);
            guiskin.toggle.fixedWidth = 18;
            guiskin.toggle.fixedHeight = 18;

            ApplyTextField(guiskin.textField, fieldTex, textMain);
            guiskin.textField.font = s_font;
            guiskin.textField.fontSize = 14;
            guiskin.textField.fontStyle = FontStyle.Bold;
            guiskin.textField.alignment = TextAnchor.MiddleLeft;
            guiskin.textField.fixedHeight = 0;
            guiskin.textField.border = new RectOffset(2, 2, 2, 2);
            guiskin.settings.cursorColor = textMain;

            ApplySolidBackground(guiskin.verticalScrollbar, sliderTex, textMain);
            ApplySolidBackground(guiskin.verticalScrollbarThumb, thumbTex, textMain);

            GUIStyle popupStyle = new GUIStyle(guiskin.box)
            {
                name = "popup",
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplySolidBackground(popupStyle, windowTex, textMain);

            GUIStyle flatButtonStyle = new GUIStyle(guiskin.button)
            {
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
                name = "flatButton",
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplyButton(flatButtonStyle, buttonTex, buttonHoverTex, buttonActiveTex, textMain);

            GUIStyle badgeDirect = LabelBox("badgeDirect", directBg, textMain);
            GUIStyle badgeDev = LabelBox("badgeDev", devBg, textMain);
            GUIStyle methodIdle = LabelBox("methodIdle", idleBg, textMain);
            GUIStyle featureOn = ButtonBox("featureOn", onBg, textMain, buttonHoverTex, buttonActiveTex);
            GUIStyle featureOff = ButtonBox("featureOff", offBg, textMain, buttonHoverTex, buttonActiveTex);
            GUIStyle hint = new GUIStyle(guiskin.label)
            {
                name = "hint",
                wordWrap = true,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            hint.normal.textColor = textMuted;
            hint.hover.textColor = textMuted;
            hint.padding = new RectOffset(2, 2, 2, 8);

            GUIStyle sectionTitle = new GUIStyle(guiskin.label)
            {
                name = "sectionTitle",
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                padding = new RectOffset(2, 2, 2, 6)
            };
            sectionTitle.normal.textColor = windowBorder;
            sectionTitle.hover.textColor = windowBorder;

            GUIStyle tooltip = new GUIStyle(guiskin.box)
            {
                name = "tooltip",
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8)
            };
            ApplySolidBackground(tooltip, BorderedTex(new Color(0.06f, 0.07f, 0.09f, 1f), windowBorder, 64, 2), Color.white);
            tooltip.border = new RectOffset(2, 2, 2, 2);

            GUIStyle dropdown = new GUIStyle(guiskin.textField)
            {
                name = "dropdown",
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            ApplySolidBackground(dropdown, BorderedTex(new Color(0.24f, 0.27f, 0.32f, 1f), windowBorder, 64, 2), textMain);
            dropdown.border = new RectOffset(2, 2, 2, 2);
            dropdown.padding = new RectOffset(8, 8, 6, 6);

            GUIStyle searchField = new GUIStyle(guiskin.textField)
            {
                name = "searchField",
                alignment = TextAnchor.MiddleLeft
            };
            ApplySolidBackground(searchField, BorderedTex(new Color(0.28f, 0.31f, 0.36f, 1f), windowBorder, 64, 2), textMain);
            searchField.border = new RectOffset(2, 2, 2, 2);
            searchField.padding = new RectOffset(8, 8, 7, 7);

            GUIStyle toolbar = new GUIStyle(guiskin.button)
            {
                name = "toolbar",
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 6, 6),
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplySolidBackground(toolbar, ColorTex(offBg), textMain);
            toolbar.hover.background = buttonHoverTex;
            toolbar.active.background = buttonActiveTex;
            toolbar.onNormal.background = ColorTex(onBg);
            toolbar.onHover.background = ColorTex(new Color(0.12f, 0.50f, 0.44f, 1f));
            toolbar.onActive.background = ColorTex(onBg);
            toolbar.onNormal.textColor = Color.white;
            toolbar.onHover.textColor = Color.white;
            toolbar.onActive.textColor = Color.white;

            GUIStyle actionButton = new GUIStyle(guiskin.button)
            {
                name = "actionButton",
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                padding = new RectOffset(10, 10, 7, 7),
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplyButton(actionButton, buttonTex, buttonHoverTex, buttonActiveTex, textMain);

            GUIStyle methodPick = ButtonBox("methodPick", idleBg, textMain, buttonHoverTex, buttonActiveTex);
            methodPick.alignment = TextAnchor.MiddleCenter;
            methodPick.border = new RectOffset(0, 0, 0, 0);

            GUIStyle methodPickOn = ButtonBox("methodPickOn", new Color(0.12f, 0.52f, 0.48f, 1f), textMain, buttonHoverTex, buttonActiveTex);
            methodPickOn.alignment = TextAnchor.MiddleCenter;
            methodPickOn.border = new RectOffset(0, 0, 0, 0);

            GUIStyle methodPickDevOn = ButtonBox("methodPickDevOn", new Color(0.62f, 0.40f, 0.10f, 1f), textMain, buttonHoverTex, buttonActiveTex);
            methodPickDevOn.alignment = TextAnchor.MiddleCenter;
            methodPickDevOn.border = new RectOffset(0, 0, 0, 0);

            GUIStyle itemCell = new GUIStyle(flatButtonStyle)
            {
                name = "itemCell",
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                padding = new RectOffset(6, 6, 6, 6),
                overflow = new RectOffset(0, 0, 0, 0),
                stretchWidth = false,
                stretchHeight = false
            };
            ApplyButton(itemCell, buttonTex, buttonHoverTex, buttonActiveTex, textMain);
            itemCell.onNormal.background = ColorTex(onBg);
            itemCell.onHover.background = ColorTex(new Color(0.12f, 0.50f, 0.44f, 1f));
            itemCell.onActive.background = ColorTex(onBg);
            itemCell.onNormal.textColor = Color.white;
            itemCell.onHover.textColor = Color.white;
            itemCell.onActive.textColor = Color.white;

            Texture2D infoCircle = CircleTex(new Color(0.10f, 0.42f, 0.46f, 1f), windowBorder);
            GUIStyle infoButton = new GUIStyle(guiskin.label)
            {
                name = "infoButton",
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0)
            };
            infoButton.normal.background = infoCircle;
            infoButton.hover.background = infoCircle;
            infoButton.normal.textColor = Color.white;
            infoButton.hover.textColor = Color.white;

            guiskin.customStyles = new[]
            {
                popupStyle,
                flatButtonStyle,
                badgeDirect,
                badgeDev,
                methodIdle,
                featureOn,
                featureOff,
                hint,
                sectionTitle,
                tooltip,
                dropdown,
                searchField,
                toolbar,
                actionButton,
                methodPick,
                methodPickOn,
                methodPickDevOn,
                itemCell,
                infoButton
            };

            return guiskin;
        }

        private static void ApplyButton(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active, Color text)
        {
            ApplySolidBackground(style, normal, text);
            style.hover.background = hover;
            style.hover.textColor = text;
            style.active.background = active;
            style.active.textColor = text;
            style.onHover.background = hover;
            style.onHover.textColor = text;
            style.onActive.background = active;
            style.onActive.textColor = Color.white;
        }

        private static void ApplyToggle(GUIStyle style, Texture2D off, Texture2D on)
        {
            ApplySolidBackground(style, off, Color.white);
            style.onNormal.background = on;
            style.hover.background = off;
            style.onHover.background = on;
            style.active.background = on;
            style.onActive.background = off;
        }

        private static void ApplyTextField(GUIStyle style, Texture2D background, Color text)
        {
            ApplySolidBackground(style, background, text);
            style.padding = new RectOffset(8, 8, 6, 6);
            style.border = new RectOffset(2, 2, 2, 2);
        }

        private static void ApplySolidBackground(GUIStyle style, Texture2D background, Color text)
        {
            style.normal.background = background;
            style.hover.background = background;
            style.active.background = background;
            style.focused.background = background;
            style.onNormal.background = background;
            style.onHover.background = background;
            style.onActive.background = background;
            style.onFocused.background = background;
            style.normal.textColor = text;
            style.hover.textColor = text;
            style.active.textColor = text;
            style.focused.textColor = text;
            style.onNormal.textColor = text;
            style.onHover.textColor = text;
            style.onActive.textColor = text;
            style.onFocused.textColor = text;
        }

        private static GUIStyle LabelBox(string name, Color background, Color text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                name = name,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplySolidBackground(style, ColorTex(background), text);
            return style;
        }

        private static GUIStyle ButtonBox(string name, Color background, Color text, Texture2D hover, Texture2D active)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                name = name,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                padding = new RectOffset(10, 10, 7, 7),
                border = new RectOffset(0, 0, 0, 0)
            };
            Texture2D tex = ColorTex(background);
            ApplySolidBackground(style, tex, text);
            style.hover.background = hover;
            style.active.background = active;
            return style;
        }

        private static Texture2D ColorTex(Color color)
        {
            return MakeTex(64, color, color, 0);
        }

        private static Texture2D BorderedTex(Color fill, Color border, int size, int thickness)
        {
            return MakeTex(size, fill, border, thickness);
        }

        private static Texture2D MakeTex(int size, Color fill, Color border, int thickness)
        {
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = thickness > 0 && (x < thickness || y < thickness || x >= size - thickness || y >= size - thickness);
                    pixels[y * size + x] = edge ? border : fill;
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply();
            s_keptTextures.Add(texture);
            return texture;
        }

        private static Texture2D CircleTex(Color fill, Color border)
        {
            const int size = 32;
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.75f);
                    Color color = distance > radius - 2f ? border : fill;
                    color.a *= alpha;
                    pixels[y * size + x] = color;
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply();
            s_keptTextures.Add(texture);
            return texture;
        }

        private static Font LoadFont()
        {
            Font gameBold = (Resources.FindObjectsOfTypeAll(typeof(Font)) as Font[]).FirstOrDefault(f => f.name.Equals("AveriaSerifLibre-Bold"));
            if (gameBold != null)
            {
                return gameBold;
            }

            try
            {
                Font osFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Bold", "Arial Bold", "Segoe UI", "Arial" }, 16);
                if (osFont != null)
                {
                    osFont.hideFlags = HideFlags.HideAndDontSave;
                    return osFont;
                }
            }
            catch
            {
            }

            ZLog.Log("Error while loading font!");
            return GUI.skin.font;
        }
    }
}
