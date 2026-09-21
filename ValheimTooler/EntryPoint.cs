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
        private bool _wasMainWindowShowed = false;
        public static bool s_showItemGiver = false;
        public static bool s_showRecipeManager = false;
        private static int s_toggleFrame = -1;
        private static bool s_toggleKeyHeld = false;

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

            if (s_showMainWindow && GameCamera.instance != null)
            {
                GameCamera.instance.SetFieldValue<bool>("m_mouseCapture", false);
                GameCamera.instance.CallMethod("UpdateMouseCapture");
            }

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
            Controls.PrepareTooltipPass();

            if (s_showMainWindow)
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
                _wasMainWindowShowed = true;

                ConfigManager.s_mainWindowPosition.Value = _valheimToolerRect.position;
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
            EspRadiusMap.Draw();
            CheatStatus.DrawMinimapIndicators();
            Controls.DrawOverlayTooltip();
            Controls.DrawConfirmDialog();
        }

        public static bool ShouldBlockCameraZoom()
        {
            if (!s_showMainWindow)
            {
                return false;
            }

            return RGUI.IsPopupOpen() || IsPointerOverTool() || Controls.HasConfirmDialog;
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

            if (s_showMainWindow && GameCamera.instance != null)
            {
                GameCamera.instance.SetFieldValue<bool>("m_mouseCapture", false);
                GameCamera.instance.CallMethod("UpdateMouseCapture");
            }
        }

        void ValheimToolerWindow(int windowID)
        {
            HandleToggleHotkey();

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
            GUI.DragWindow(new Rect(34, 0, 10000, 32));
        }

        public static bool IsPointerOverTool()
        {
            if (!s_showMainWindow)
            {
                return false;
            }

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
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
