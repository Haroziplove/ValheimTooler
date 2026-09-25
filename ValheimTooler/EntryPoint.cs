using System;
using System.Linq;
using System.Reflection;
using RapidGUI;
using UnityEngine;
using ValheimTooler.Utils;
using ValheimTooler.Core;
using ValheimTooler.UI;

namespace ValheimTooler
{
    public class EntryPoint : MonoBehaviour
    {
        public static readonly int s_boxSpacing = 8;
        private Rect _valheimToolerRect;
        public static Rect s_mainWindowRect;

        public static bool s_showMainWindow = true;
        public static bool s_passThroughInput = false;
        private bool _wasMainWindowShowed = false;
        public static bool s_showItemGiver = false;
        public static bool s_showRecipeManager = false;
        private static int s_toggleFrame = -1;
        private static bool s_toggleKeyHeld = false;
        private static int s_passThroughFrame = -1;
        private static bool s_passThroughKeyHeld = false;

        private WindowToolbar _windowToolbar = WindowToolbar.PLAYER;
        private readonly string[] _toolbarChoices = {
            "$vt_toolbar_player",
            "$vt_toolbar_entities",
            "$vt_toolbar_terrain_shaper",
            "$vt_toolbar_misc"
        };

        private string _version;

        public void Start()
        {
            _valheimToolerRect = new Rect(ConfigManager.s_mainWindowPosition.Value.x, ConfigManager.s_mainWindowPosition.Value.y, 800, 300);
            s_mainWindowRect = _valheimToolerRect;
            s_showMainWindow = ConfigManager.s_showAtStartup.Value;

            StartFeature(PlayerHacks.Start, "PlayerHacks");
            StartFeature(EntitiesItemsHacks.Start, "EntitiesItemsHacks");
            StartFeature(ItemGiver.Start, "ItemGiver");
            StartFeature(RecipeManager.Start, "RecipeManager");
            StartFeature(MiscHacks.Start, "MiscHacks");
            StartFeature(ESP.Start, "ESP");
            StartFeature(TerrainShaper.Start, "TerrainShaper");

            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private static void StartFeature(Action start, string name)
        {
            try
            {
                start();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ValheimTooler] Failed to start " + name + ": " + ex.Message);
            }
        }

        public void Update()
        {
            var shortcut = ConfigManager.s_toggleInterfaceKey.Value;
            if (shortcut.MainKey != KeyCode.None)
            {
                if (Input.GetKeyUp(shortcut.MainKey))
                {
                    s_toggleKeyHeld = false;
                }

                if (Input.GetKeyDown(shortcut.MainKey) && ToggleModifiersHeld(shortcut))
                {
                    s_toggleKeyHeld = true;
                    ToggleMainWindow();
                }
            }

            if (Input.GetKeyUp(KeyCode.End))
            {
                s_passThroughKeyHeld = false;
            }
            if (s_showMainWindow && Input.GetKeyDown(KeyCode.End) && !s_passThroughKeyHeld)
            {
                TogglePassThrough();
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                ComfortTable.Toggle();
            }

            ApplyMouseCapture();

            PlayerHacks.Update();
            EntitiesItemsHacks.Update();
            ItemGiver.Update();
            MiscHacks.Update();
            ESP.Update();
            TerrainShaper.Update();
        }

        public void OnGUI()
        {
            GUI.skin = InterfaceMaker.CustomSkin;

            HandleToggleHotkey();
            HandlePassThroughHotkey();
            Controls.PrepareTooltipPass();

            Matrix4x4 previousMatrix = GUI.matrix;
            if (s_showMainWindow)
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ConfigManager.UiScale, ConfigManager.UiScale, 1f));
                if (ShouldProcessToolGui())
                {
                    _valheimToolerRect = GUILayout.Window(1001, _valheimToolerRect, ValheimToolerWindow, VTLocalization.instance.Localize($"$vt_main_title (v{_version})"), GUILayout.Height(10), GUILayout.MinWidth(860));
                    s_mainWindowRect = _valheimToolerRect;

                    if (s_showItemGiver)
                    {
                        ItemGiver.DisplayGUI();
                    }
                    if (s_showRecipeManager)
                    {
                        RecipeManager.DisplayGUI();
                    }

                    ConfigManager.s_mainWindowPosition.Value = _valheimToolerRect.position;
                }
                _wasMainWindowShowed = true;
                GUI.matrix = previousMatrix;
            }
            else
            {
                if (_wasMainWindowShowed)
                {
                    if (GameCamera.instance != null)
                    {
                        GameCamera.instance.SetFieldValue<bool>("m_mouseCapture", true);
                        GameCamera.instance.CallMethod("UpdateMouseCapture");
                    }
                    _wasMainWindowShowed = false;
                }
            }

            ESP.DisplayGUI();
            ComfortEsp.Draw();
            ComfortTable.Draw();
            EspRadiusMap.Draw();
            CheatStatus.DrawHudIndicators();
            if (!s_passThroughInput)
            {
                Controls.DrawOverlayTooltip();
                Controls.DrawConfirmDialog();
            }
        }

        public static bool IsToolInteractive()
        {
            return s_showMainWindow && !s_passThroughInput;
        }

        public static bool ShouldBlockCameraZoom()
        {
            if (!IsToolInteractive())
            {
                return false;
            }

            return RGUI.IsPopupOpen() || IsPointerOverTool() || Controls.HasConfirmDialog;
        }

        private static bool ShouldProcessToolGui()
        {
            if (!s_passThroughInput)
            {
                return true;
            }

            EventType type = Event.current.type;
            return type == EventType.Layout || type == EventType.Repaint;
        }

        private static void ApplyMouseCapture()
        {
            if (GameCamera.instance == null)
            {
                return;
            }

            GameCamera.instance.SetFieldValue<bool>("m_mouseCapture", !IsToolInteractive());
            GameCamera.instance.CallMethod("UpdateMouseCapture");
        }

        public static void HandleToggleHotkey()
        {
            Event ev = Event.current;
            var shortcut = ConfigManager.s_toggleInterfaceKey.Value;
            if (ev == null || shortcut.MainKey == KeyCode.None)
            {
                return;
            }

            if (ev.type == EventType.KeyUp && ev.keyCode == shortcut.MainKey)
            {
                s_toggleKeyHeld = false;
                return;
            }

            if (ev.type != EventType.KeyDown || ev.keyCode != shortcut.MainKey || s_toggleKeyHeld)
            {
                return;
            }

            if (!ToggleModifiersHeld(shortcut))
            {
                return;
            }

            s_toggleKeyHeld = true;
            ToggleMainWindow();
            GUIUtility.keyboardControl = 0;
            GUI.FocusControl(null);
            ev.Use();
        }

        private static bool ToggleModifiersHeld(ValheimTooler.Configuration.KeyboardShortcut shortcut)
        {
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        public static void ToggleMainWindow()
        {
            if (s_toggleFrame == Time.frameCount)
            {
                return;
            }

            s_toggleFrame = Time.frameCount;
            s_showMainWindow = !s_showMainWindow;
            GUIUtility.keyboardControl = 0;
            GUI.FocusControl(null);
            ApplyMouseCapture();
        }

        public static void HandlePassThroughHotkey()
        {
            Event ev = Event.current;
            if (ev == null)
            {
                return;
            }

            if (ev.type == EventType.KeyUp && ev.keyCode == KeyCode.End)
            {
                s_passThroughKeyHeld = false;
                return;
            }

            if (!s_showMainWindow || ev.type != EventType.KeyDown || ev.keyCode != KeyCode.End || s_passThroughKeyHeld)
            {
                return;
            }

            TogglePassThrough();
            ev.Use();
        }

        private static void TogglePassThrough()
        {
            if (s_passThroughFrame == Time.frameCount || !s_showMainWindow)
            {
                return;
            }

            s_passThroughFrame = Time.frameCount;
            s_passThroughKeyHeld = true;
            s_passThroughInput = !s_passThroughInput;
            GUIUtility.keyboardControl = 0;
            GUI.FocusControl(null);
            ApplyMouseCapture();
        }

        void ValheimToolerWindow(int windowID)
        {
            HandleToggleHotkey();
            HandlePassThroughHotkey();

            GUILayout.Space(4);

            GUIStyle toolbarStyle = InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.FindStyle("toolbar") : null;
            _windowToolbar = (WindowToolbar)GUILayout.Toolbar(
                (int)_windowToolbar,
                _toolbarChoices.Select(choice => VTLocalization.instance.Localize(choice)).ToArray(),
                toolbarStyle != null ? toolbarStyle : GUI.skin.button,
                GUILayout.Height(32));

            switch (_windowToolbar)
            {
                case WindowToolbar.PLAYER:
                    PlayerHacks.DisplayGUI();
                    break;
                case WindowToolbar.ENTITIES_ITEMS:
                    EntitiesItemsHacks.DisplayGUI();
                    break;
                case WindowToolbar.TERRAIN_SHAPER:
                    TerrainShaper.DisplayGUI();
                    break;
                case WindowToolbar.MISC:
                    MiscHacks.DisplayGUI();
                    break;
            }

            Controls.DrawInfoButton(8f, 7f);
            DrawZoomButtons();
            DrawActionRadius();
            GUI.DragWindow(new Rect(34, 0, Mathf.Max(0f, s_mainWindowRect.width - 100f), 32));
        }

        private static void DrawZoomButtons()
        {
            GUIStyle style = InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.button : GUI.skin.button;
            Rect minus = new Rect(s_mainWindowRect.width - 64f, 6f, 26f, 22f);
            Rect plus = new Rect(s_mainWindowRect.width - 34f, 6f, 26f, 22f);
            string zoomOut = Controls.Tip("$vt_ui_zoom_out");
            string zoomIn = Controls.Tip("$vt_ui_zoom_in");
            if (GUI.Button(minus, new GUIContent("-", zoomOut), style))
            {
                ConfigManager.s_uiScale.Value = Mathf.Clamp(ConfigManager.UiScale - 0.1f, 0.6f, 1.8f);
            }
            if (GUI.Button(plus, new GUIContent("+", zoomIn), style))
            {
                ConfigManager.s_uiScale.Value = Mathf.Clamp(ConfigManager.UiScale + 0.1f, 0.6f, 1.8f);
            }
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                if (minus.Contains(Event.current.mousePosition))
                {
                    Controls.SetHoverTooltip(zoomOut);
                }
                else if (plus.Contains(Event.current.mousePosition))
                {
                    Controls.SetHoverTooltip(zoomIn);
                }
            }
        }

        private static void DrawActionRadius()
        {
            GUILayout.Space(8);
            float radius = ConfigManager.ActionRadius;
            string label = VTLocalization.instance.Localize("$vt_action_radius") + " " + radius.ToString("0.0") + "m";
            GUILayout.Label(new GUIContent(label, Controls.Tip("$vt_action_radius")));
            Controls.NoteHoverTooltip(Controls.Tip("$vt_action_radius"));
            float next = GUILayout.HorizontalSlider(radius, 1f, 80f);
            Controls.NoteHoverAction("$vt_action_radius");
            Controls.NoteHoverTooltip(Controls.Tip("$vt_action_radius"));
            if (!Mathf.Approximately(next, radius))
            {
                ConfigManager.s_actionRadius.Value = next;
            }
        }

        public static bool IsPointerOverTool()
        {
            if (!IsToolInteractive())
            {
                return false;
            }

            Vector2 screenMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (s_showMainWindow && ComfortTable.Visible && ComfortTable.ScreenRect.Contains(screenMouse))
            {
                return true;
            }

            float scale = ConfigManager.UiScale;
            Vector2 mouse = new Vector2(Input.mousePosition.x / scale, (Screen.height - Input.mousePosition.y) / scale);
            if (s_mainWindowRect.Contains(mouse))
            {
                return true;
            }

            if (s_showItemGiver && ItemGiver.WindowRect.Contains(mouse))
            {
                return true;
            }

            if (s_showRecipeManager && RecipeManager.WindowRect.Contains(mouse))
            {
                return true;
            }

            if (Controls.IsPointerOverTooltip(mouse) || Controls.IsPointerOverConfirm(mouse))
            {
                return true;
            }

            return RGUI.IsPointerOverPopup(mouse);
        }
    }
}
