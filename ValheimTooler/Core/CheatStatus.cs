using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class CheatStatus
    {
        public static bool IsCharacterTagged()
        {
            if (Game.instance == null)
            {
                return false;
            }

            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile == null)
            {
                return false;
            }

            return profile.GetFieldValue<bool>("m_usedCheats");
        }

        public static bool IsDevCommandsEnabled()
        {
            if (Console.instance == null)
            {
                return false;
            }

            return Console.instance.IsCheatsEnabled();
        }

        public static bool IsOfficialNoCostCheat()
        {
            return Player.m_localPlayer != null && Player.m_localPlayer.NoCostCheat();
        }

        public static Inventory GetOpenContainerInventory()
        {
            if (InventoryGui.instance == null)
            {
                return null;
            }

            if (!InventoryGui.instance.IsContainerOpen())
            {
                return null;
            }

            Container container = InventoryGui.instance.GetFieldValue<Container>("m_currentContainer");
            return container != null ? container.GetInventory() : null;
        }

        public static int CountCheated(Inventory inventory)
        {
            if (inventory == null)
            {
                return 0;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item != null && item.m_cheated)
                {
                    count++;
                }
            }

            return count;
        }

        public static string DescribeCheatedItems(Inventory inventory, int maxNames)
        {
            if (inventory == null)
            {
                return "";
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
            {
                return "";
            }

            var names = new List<string>();
            int extra = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_cheated || item.m_shared == null)
                {
                    continue;
                }

                string name = item.m_shared.m_name;
                if (Localization.instance != null)
                {
                    name = Localization.instance.Localize(name);
                }

                if (names.Count < maxNames)
                {
                    names.Add(name);
                }
                else
                {
                    extra++;
                }
            }

            if (names.Count == 0)
            {
                return "";
            }

            var text = new StringBuilder(string.Join(", ", names.ToArray()));
            if (extra > 0)
            {
                text.Append(" +").Append(extra);
            }

            return text.ToString();
        }

        public static float DropCleanRadius
        {
            get
            {
                return ConfigManager.s_cleanDroppedRadius != null ? ConfigManager.s_cleanDroppedRadius.Value : 20f;
            }
            set
            {
                if (ConfigManager.s_cleanDroppedRadius != null)
                {
                    ConfigManager.s_cleanDroppedRadius.Value = value;
                }
            }
        }

        public static bool GetAchievementsBypass()
        {
            try
            {
                return PlayerProfile.s_bypassCheatChecks;
            }
            catch
            {
                return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey("bypasscheatchecks");
            }
        }

        public static void SetAchievementsBypass(bool enabled)
        {
            if (GetAchievementsBypass() == enabled)
            {
                return;
            }

            ZoneSystem zone = ZoneSystem.instance;
            if (zone != null)
            {
                if (enabled)
                {
                    zone.SetGlobalKey("bypasscheatchecks 1");
                    if (!GetAchievementsBypass())
                    {
                        zone.SetGlobalKey("bypasscheatchecks");
                    }
                    if (!GetAchievementsBypass())
                    {
                        zone.CallMethod("GlobalKeyAdd", "bypasscheatchecks 1", true);
                    }
                }
                else
                {
                    zone.RemoveGlobalKey("bypasscheatchecks");
                    zone.CallMethod("GlobalKeyRemove", "bypasscheatchecks", true);
                    zone.CallMethod("GlobalKeyRemove", "bypasscheatchecks 1", true);
                }
            }

            if (GetAchievementsBypass() != enabled && Console.instance != null)
            {
                Console.instance.TryRunCommand("yesiuseddevcommandsbutiwantmyachievementsanyway", true, true);
            }
        }

        public static void ClearCharacterFlag()
        {
            if (Game.instance == null)
            {
                return;
            }

            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile == null)
            {
                return;
            }

            profile.SetFieldValue("m_usedCheats", false);
            Game.instance.SavePlayerProfile(false, false);
        }

        public static void ForceGroundRefresh()
        {
            s_groundScanAt = 0f;
        }

        public static bool HasInventoryCheated()
        {
            if (Player.m_localPlayer == null)
            {
                return false;
            }

            return CountCheated(Player.m_localPlayer.GetInventory()) > 0;
        }

        public static bool HasNearbyCheatedDrops()
        {
            RefreshGroundCheats();
            return s_groundCount > 0;
        }

        public static void DrawHudIndicators()
        {
            if (ConfigManager.s_cheatMinimapIndicators == null || !ConfigManager.s_cheatMinimapIndicators.Value)
            {
                return;
            }

            if (Player.m_localPlayer == null)
            {
                return;
            }

            bool inventoryOpen = InventoryGui.IsVisible();
            bool inventoryCheated = HasInventoryCheated();
            bool containerCheated = inventoryOpen && HasContainerCheated();
            bool drops = HasNearbyCheatedDrops();

            if (inventoryOpen && (inventoryCheated || containerCheated))
            {
                RectTransform panel = InventoryPanel();
                if (panel != null)
                {
                    DrawIndicatorRow(ScreenRect(panel), inventoryCheated ? GetRedCircle() : null, containerCheated ? GetYellowCircle() : null);
                }
            }

            if (Hud.IsUserHidden() || Minimap.IsOpen() || Minimap.instance == null)
            {
                return;
            }

            GameObject smallRoot = Minimap.instance.m_smallRoot;
            if (smallRoot == null || !smallRoot.activeInHierarchy)
            {
                return;
            }

            RectTransform transform = smallRoot.transform as RectTransform;
            if (transform == null)
            {
                return;
            }

            bool minimapRed = inventoryCheated && !inventoryOpen;
            if (!minimapRed && !drops)
            {
                return;
            }

            DrawIndicatorRow(ScreenRect(transform), minimapRed ? GetRedCircle() : null, drops ? GetOrangeCircle() : null);
        }

        public static bool HasContainerCheated()
        {
            return CountCheated(GetOpenContainerInventory()) > 0;
        }

        private static RectTransform InventoryPanel()
        {
            if (InventoryGui.instance == null)
            {
                return null;
            }

            RectTransform player = InventoryGui.instance.m_player;
            if (player != null && player.gameObject.activeInHierarchy)
            {
                return player;
            }

            return InventoryGui.instance.m_inventoryRoot as RectTransform;
        }

        private static void DrawIndicatorRow(Rect anchor, Texture2D first, Texture2D second)
        {
            const float size = 14f;
            const float gap = 6f;
            const float lift = 10f;
            int shown = (first != null ? 1 : 0) + (second != null ? 1 : 0);
            if (shown == 0)
            {
                return;
            }

            float total = shown * size + (shown - 1) * gap;
            float x = anchor.x + (anchor.width - total) * 0.5f;
            float y = anchor.y - size - lift;
            if (first != null)
            {
                DrawCircle(new Rect(x, y, size, size), first);
                x += size + gap;
            }
            if (second != null)
            {
                DrawCircle(new Rect(x, y, size, size), second);
            }
        }

        private static void DrawCircle(Rect rect, Texture2D texture)
        {
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            }
        }

        private static Rect ScreenRect(RectTransform transform)
        {
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            float left = Mathf.Min(bottomLeft.x, topRight.x);
            float right = Mathf.Max(bottomLeft.x, topRight.x);
            float bottom = Mathf.Min(bottomLeft.y, topRight.y);
            float top = Mathf.Max(bottomLeft.y, topRight.y);
            return new Rect(left, Screen.height - top, right - left, top - bottom);
        }

        private static Texture2D GetRedCircle()
        {
            if (s_redCircle == null)
            {
                s_redCircle = MakeCircle(new Color(0.92f, 0.12f, 0.1f, 1f));
            }

            return s_redCircle;
        }

        private static Texture2D GetOrangeCircle()
        {
            if (s_orangeCircle == null)
            {
                s_orangeCircle = MakeCircle(new Color(1f, 0.45f, 0.12f, 1f));
            }

            return s_orangeCircle;
        }

        private static Texture2D GetYellowCircle()
        {
            if (s_yellowCircle == null)
            {
                s_yellowCircle = MakeCircle(new Color(0.95f, 0.82f, 0.12f, 1f));
            }

            return s_yellowCircle;
        }

        private static Texture2D MakeCircle(Color color)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            float center = (size - 1) * 0.5f;
            float radius = center - 1.5f;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.75f);
                    float ring = Mathf.Clamp01(1.4f - Mathf.Abs(distance - (radius - 1.2f)));
                    Color pixel = color;
                    pixel.r = Mathf.Lerp(0.12f, color.r, 0.65f + 0.35f * ring);
                    pixel.g = Mathf.Lerp(0.04f, color.g, 0.65f + 0.35f * ring);
                    pixel.b = Mathf.Lerp(0.04f, color.b, 0.65f + 0.35f * ring);
                    pixel.a = color.a * alpha;
                    texture.SetPixel(x, y, alpha > 0.01f ? pixel : clear);
                }
            }

            texture.Apply();
            return texture;
        }

        public static void Draw()
        {
            DrawCharacterFlag();

            DrawFlag("$vt_player_cheat_status_devcommands",
                IsDevCommandsEnabled()
                    ? VTLocalization.instance.Localize("$vt_cheat_on")
                    : VTLocalization.instance.Localize("$vt_cheat_off"),
                IsDevCommandsEnabled());

            DrawFlag("$vt_player_cheat_status_nocost",
                IsOfficialNoCostCheat()
                    ? VTLocalization.instance.Localize("$vt_cheat_on")
                    : VTLocalization.instance.Localize("$vt_cheat_off"),
                IsOfficialNoCostCheat());

            Inventory playerInv = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;
            DrawInventoryLine("$vt_player_cheat_status_inventory", playerInv, false);

            Inventory containerInv = GetOpenContainerInventory();
            DrawInventoryLine("$vt_player_cheat_status_container", containerInv, containerInv == null);

            DrawGroundLine();
        }

        private static void DrawCharacterFlag()
        {
            bool tagged = IsCharacterTagged();
            string clearTip = UI.Controls.Tip("$vt_player_cheat_status_clear_flag");
            GUILayout.BeginHorizontal();
            UI.Controls.HoverLabel("$vt_player_cheat_status_character");
            GUILayout.FlexibleSpace();
            GUI.enabled = tagged;
            if (GUILayout.Button(new GUIContent(VTLocalization.instance.Localize("$vt_player_cheat_status_clear_flag"), clearTip), GUILayout.Width(80), GUILayout.Height(28)))
            {
                ClearCharacterFlag();
            }
            GUI.enabled = true;
            UI.Controls.NoteHoverTooltip(clearTip);
            GUILayout.Label(
                tagged
                    ? VTLocalization.instance.Localize("$vt_player_cheat_status_tagged")
                    : VTLocalization.instance.Localize("$vt_player_cheat_status_clean"),
                UI.Controls.StatusStyle(tagged),
                GUILayout.Width(80),
                GUILayout.Height(28));
            GUILayout.EndHorizontal();
        }

        private static void DrawFlag(string labelCode, string value, bool warning)
        {
            GUILayout.BeginHorizontal();
            UI.Controls.HoverLabel(labelCode);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, UI.Controls.StatusStyle(warning), GUILayout.Width(140));
            GUILayout.EndHorizontal();
        }

        private static void DrawInventoryLine(string labelCode, Inventory inventory, bool closed)
        {
            GUILayout.Space(4);
            if (closed)
            {
                string tip = UI.Controls.Tip(labelCode);
                GUILayout.Label(new GUIContent(VTLocalization.instance.Localize(labelCode) + ": " + VTLocalization.instance.Localize("$vt_player_cheat_status_closed"), tip));
                UI.Controls.NoteHoverTooltip(tip);
                return;
            }

            int count = CountCheated(inventory);
            bool warning = count > 0;
            string summary = count == 0
                ? VTLocalization.instance.Localize("$vt_player_cheat_status_none")
                : count + " " + VTLocalization.instance.Localize("$vt_player_cheat_status_items");

            GUILayout.BeginHorizontal();
            UI.Controls.HoverLabel(labelCode);
            GUILayout.FlexibleSpace();
            GUILayout.Label(summary, UI.Controls.StatusStyle(warning), GUILayout.Width(140));
            GUILayout.EndHorizontal();

            string names = DescribeCheatedItems(inventory, 6);
            if (!string.IsNullOrEmpty(names))
            {
                GUILayout.Label(names, UI.Controls.StatusStyle(true));
            }
        }

        private static float s_groundScanAt;
        private static int s_groundCount;
        private static int s_cleanCount;
        private static float s_lastStatusRadius = -1f;
        private static float s_lastCleanRadius = -1f;
        private static string s_groundNames = "";
        private static Texture2D s_redCircle;
        private static Texture2D s_orangeCircle;
        private static Texture2D s_yellowCircle;

        public static void DrawCleanPreviewCount()
        {
            RefreshGroundCheats();
            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_clean_cheated_drops_count") + " " + s_cleanCount);
        }

        private static float NearbyStatusRadius()
        {
            if (ConfigManager.s_espRadiusEnabled != null && ConfigManager.s_espRadiusEnabled.Value)
            {
                return ConfigManager.s_espRadius.Value;
            }

            return 80f;
        }

        private static void DrawGroundLine()
        {
            RefreshGroundCheats();

            GUILayout.Space(4);
            string summary = s_groundCount == 0
                ? VTLocalization.instance.Localize("$vt_player_cheat_status_ground_none")
                : s_groundCount + " " + VTLocalization.instance.Localize("$vt_player_cheat_status_items");

            GUILayout.BeginHorizontal();
            UI.Controls.HoverLabel("$vt_player_cheat_status_ground");
            GUILayout.FlexibleSpace();
            GUILayout.Label(summary, UI.Controls.StatusStyle(s_groundCount > 0), GUILayout.Width(140));
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(s_groundNames))
            {
                GUILayout.Label(s_groundNames, UI.Controls.StatusStyle(true));
            }
        }

        private static void RefreshGroundCheats()
        {
            float statusRadius = NearbyStatusRadius();
            float cleanRadius = DropCleanRadius;
            if (Time.time < s_groundScanAt && Mathf.Approximately(statusRadius, s_lastStatusRadius) && Mathf.Approximately(cleanRadius, s_lastCleanRadius))
            {
                return;
            }

            s_groundScanAt = Time.time + 0.5f;
            s_lastStatusRadius = statusRadius;
            s_lastCleanRadius = cleanRadius;
            s_groundCount = 0;
            s_cleanCount = 0;
            s_groundNames = "";

            if (Player.m_localPlayer == null)
            {
                return;
            }

            ItemDrop[] drops = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
            if (drops == null)
            {
                return;
            }

            Vector3 origin = Player.m_localPlayer.transform.position;
            var names = new List<string>();
            int extra = 0;

            foreach (ItemDrop drop in drops)
            {
                if (drop == null || drop.m_itemData == null || !drop.m_itemData.m_cheated)
                {
                    continue;
                }

                float distance = global::Utils.DistanceXZ(origin, drop.transform.position);
                if (distance <= cleanRadius)
                {
                    s_cleanCount++;
                }

                if (distance > statusRadius)
                {
                    continue;
                }

                s_groundCount++;
                string name = drop.GetHoverName();
                if (Localization.instance != null)
                {
                    name = Localization.instance.Localize(name);
                }

                if (names.Count < 6)
                {
                    names.Add(name);
                }
                else
                {
                    extra++;
                }
            }

            if (names.Count > 0)
            {
                var text = new StringBuilder(string.Join(", ", names.ToArray()));
                if (extra > 0)
                {
                    text.Append(" +").Append(extra);
                }
                s_groundNames = text.ToString();
            }
        }
    }
}
