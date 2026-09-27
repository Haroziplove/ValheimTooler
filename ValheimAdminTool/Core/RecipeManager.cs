using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ValheimAdminTool.Core.Extensions;
using ValheimAdminTool.UI;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.Core
{
    public enum RecipeBiome
    {
        Meadows,
        BlackForest,
        Swamp,
        Mountains,
        Plains,
        Mistlands,
        Ashlands,
        Ocean,
        DeepNorth,
        Special
    }

    public static class RecipeManager
    {
        private const int WindowId = 1003;
        private const int GridColumns = 5;
        private const float GridHeight = 300f;

        private static Rect s_windowRect;
        private static Vector2 s_scroll;
        private static Texture2D s_placeholderIcon;
        private static int s_tabIdx;
        private static string s_searchTerms = "";
        private static string s_previousSearch = null;
        private static int s_previousTab = -1;
        private static int s_lastRecipeCount = -1;
        private static int s_lastKnownCount = -1;
        private static int s_learnSeq;
        private static bool s_learnBiomeItems = true;

        private static readonly List<RecipeEntry> s_catalog = new List<RecipeEntry>();
        private static readonly Dictionary<string, RecipeEntry> s_catalogByKey = new Dictionary<string, RecipeEntry>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_selected = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_suppressed = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> s_learnedSeq = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly List<string> s_vendorOrder = new List<string>();
        private static readonly Dictionary<string, string> s_vendorLabel = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> s_vendorByKey;
        private static bool s_vendorsReady;
        private static List<RecipeEntry> s_visible = new List<RecipeEntry>();

        private static readonly RecipeBiome[] s_biomeOrder =
        {
            RecipeBiome.Meadows,
            RecipeBiome.BlackForest,
            RecipeBiome.Swamp,
            RecipeBiome.Mountains,
            RecipeBiome.Plains,
            RecipeBiome.Mistlands,
            RecipeBiome.Ashlands,
            RecipeBiome.Ocean,
            RecipeBiome.DeepNorth,
            RecipeBiome.Special
        };

        public static Rect WindowRect => s_windowRect;

        public static void Start()
        {
            s_windowRect = new Rect(ConfigManager.s_recipeManagerWindowPosition.Value.x, ConfigManager.s_recipeManagerWindowPosition.Value.y, 520, 580);
        }

        public static void DisplayGUI()
        {
            s_windowRect = GUILayout.Window(WindowId, s_windowRect, DrawWindow, VTLocalization.instance.Localize("$vt_recipe_window_title"), GUILayout.MinWidth(520));
            ConfigManager.s_recipeManagerWindowPosition.Value = s_windowRect.position;
        }

        public static void DisplaySection()
        {
            Controls.BeginSection("$vt_recipe_title");
            Controls.Hint("$vt_recipe_hint");

            if (Controls.ActionButton("$vt_recipe_forget_items", FeatureMethod.Direct))
            {
                Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_items_confirm", ForgetAllItems);
            }
            if (Controls.ActionButton("$vt_recipe_discover_items", FeatureMethod.Direct))
            {
                DiscoverAllItems();
            }
            if (Controls.ActionButton("$vt_recipe_forget_recipes", FeatureMethod.Direct))
            {
                Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_recipes_confirm", ForgetAllRecipes);
            }
            if (Controls.ActionButton("$vt_recipe_learn_all", FeatureMethod.Direct))
            {
                LearnAllRecipes();
            }
            if (Controls.ActionButton("$vt_recipe_forget_stations", FeatureMethod.Direct))
            {
                Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_stations_confirm", ForgetAllStations);
            }
            if (Controls.ActionButton("$vt_recipe_learn_stations", FeatureMethod.Direct))
            {
                LearnAllStations();
            }

            GUILayout.Space(10);
            if (Controls.ActionButton(EntryPoint.s_showRecipeManager ? "$vt_recipe_window_hide" : "$vt_recipe_window_show", FeatureMethod.Direct))
            {
                EntryPoint.s_showRecipeManager = !EntryPoint.s_showRecipeManager;
            }

            GUILayout.Space(10);
            GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_biome_title"), InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.FindStyle("sectionTitle") : GUI.skin.label);
            if (Controls.FeatureButton("$vt_recipe_biome_with_items", s_learnBiomeItems, FeatureMethod.Direct))
            {
                s_learnBiomeItems = !s_learnBiomeItems;
            }

            const float rowHeight = 30f;
            const float gap = 8f;
            for (int i = 0; i < s_biomeOrder.Length; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                if (Controls.ActionButtonAt(left, BiomeLabel(s_biomeOrder[i]), FeatureMethod.Direct, "$vt_recipe_biome_learn"))
                {
                    LearnBiome(s_biomeOrder[i]);
                }
                if (i + 1 < s_biomeOrder.Length && Controls.ActionButtonAt(right, BiomeLabel(s_biomeOrder[i + 1]), FeatureMethod.Direct, "$vt_recipe_biome_learn"))
                {
                    LearnBiome(s_biomeOrder[i + 1]);
                }
                GUILayout.Space(4);
            }

            DrawVendorButtons(true);
            Controls.EndSection();
        }

        private static void DrawWindow(int windowID)
        {
            EntryPoint.HandleToggleHotkey();

            if (Event.current != null && new Rect(0f, 0f, s_windowRect.width, s_windowRect.height).Contains(Event.current.mousePosition))
            {
                Controls.ClearCoveredHover();
            }

            EnsureCatalog();
            RebuildVisible();

            GUILayout.Space(10);
            string[] tabs =
            {
                VTLocalization.instance.Localize("$vt_recipe_tab_unlocked"),
                VTLocalization.instance.Localize("$vt_recipe_tab_locked"),
                VTLocalization.instance.Localize("$vt_recipe_tab_recent")
            };
            int nextTab = GUILayout.Toolbar(s_tabIdx, tabs, InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.GetStyle("toolbar") : GUI.skin.button, GUILayout.Height(28));
            if (nextTab != s_tabIdx)
            {
                s_tabIdx = nextTab;
                s_selected.Clear();
                s_previousSearch = null;
            }

            s_searchTerms = GUILayout.TextField(s_searchTerms ?? "", GUILayout.MinHeight(26));
            RebuildVisible();

            GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_biome_select"));
            const float rowHeight = 28f;
            const float gap = 6f;
            for (int i = 0; i < s_biomeOrder.Length; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                string selectTip = Controls.Tip("$vt_recipe_biome_select");
                if (GUI.Button(left, new GUIContent(VTLocalization.instance.Localize(BiomeLabel(s_biomeOrder[i])), selectTip)))
                {
                    SelectVisibleBiome(s_biomeOrder[i]);
                }
                if (Event.current != null && Event.current.type == EventType.Repaint && left.Contains(Event.current.mousePosition))
                {
                    Controls.SetHoverTooltip(selectTip);
                }
                if (i + 1 < s_biomeOrder.Length && GUI.Button(right, new GUIContent(VTLocalization.instance.Localize(BiomeLabel(s_biomeOrder[i + 1])), selectTip)))
                {
                    SelectVisibleBiome(s_biomeOrder[i + 1]);
                }
                if (i + 1 < s_biomeOrder.Length && Event.current != null && Event.current.type == EventType.Repaint && right.Contains(Event.current.mousePosition))
                {
                    Controls.SetHoverTooltip(selectTip);
                }
                GUILayout.Space(2);
            }

            if (s_vendorOrder.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_vendor_title"));
                DrawVendorButtons(false);
            }

            GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_selected") + " " + s_selected.Count + " / " + s_visible.Count);

            if (s_visible.Count > 0)
            {
                DrawRecipeGrid();
            }
            else
            {
                GUILayout.Label(VTLocalization.instance.Localize(EmptyLabel()), GUILayout.Height(GridHeight));
            }

            GUILayout.BeginHorizontal();
            if (WindowAction("$vt_recipe_select_visible"))
            {
                for (int i = 0; i < s_visible.Count; i++)
                {
                    s_selected.Add(s_visible[i].key);
                }
            }
            if (WindowAction("$vt_recipe_clear_selection"))
            {
                s_selected.Clear();
            }
            GUILayout.EndHorizontal();

            if (s_tabIdx == 1)
            {
                if (Controls.ActionButton("$vt_recipe_learn_selected", FeatureMethod.Direct))
                {
                    LearnSelected();
                }
            }
            else if (Controls.ActionButton("$vt_recipe_forget_selected", FeatureMethod.Direct))
            {
                if (s_selected.Count > 0)
                {
                    Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_selected_confirm", ForgetSelected);
                }
            }

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0)
            };
            if (GUI.Button(new Rect(s_windowRect.width - 36, 6, 26, 26), "X", closeStyle))
            {
                EntryPoint.s_showRecipeManager = false;
            }

            GUI.DragWindow(new Rect(0, 0, s_windowRect.width - 40, 32));
        }

        private static bool WindowAction(string labelCode)
        {
            string tip = Controls.Tip(labelCode);
            bool clicked = GUILayout.Button(new GUIContent(VTLocalization.instance.Localize(labelCode), tip), InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.FindStyle("actionButton") : GUI.skin.button, GUILayout.MinHeight(26), GUILayout.ExpandWidth(true));
            Controls.NoteHoverTooltip(tip);
            return clicked;
        }

        private static void DrawRecipeGrid()
        {
            int count = s_visible.Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)GridColumns));
            Rect view = GUILayoutUtility.GetRect(1f, GridHeight, GUILayout.ExpandWidth(true), GUILayout.Height(GridHeight));
            float cell = view.width > 32f ? Mathf.Floor((view.width - 16f) / GridColumns) : 72f;
            Rect content = new Rect(0f, 0f, GridColumns * cell, rows * cell);
            s_scroll = GUI.BeginScrollView(view, s_scroll, content, false, true);

            GUIStyle cellStyle = InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.GetStyle("itemCell") : GUI.skin.button;
            Rect visible = new Rect(s_scroll.x, s_scroll.y, view.width, view.height);
            Vector2 mouse = Event.current.mousePosition;

            for (int i = 0; i < count; i++)
            {
                RecipeEntry entry = s_visible[i];
                int col = i % GridColumns;
                int row = i / GridColumns;
                Rect cellRect = new Rect(col * cell, row * cell, cell, cell);
                bool on = s_selected.Contains(entry.key);
                bool next = GUI.Toggle(cellRect, on, new GUIContent(entry.icon), cellStyle);
                if (next != on)
                {
                    if (next)
                    {
                        s_selected.Add(entry.key);
                    }
                    else
                    {
                        s_selected.Remove(entry.key);
                    }
                }

                if (Event.current.type == EventType.Repaint && cellRect.Contains(mouse) && visible.Contains(mouse))
                {
                    Controls.SetHoverTooltip(entry.displayName);
                }
            }

            GUI.EndScrollView();
        }

        public static bool IsSuppressed(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (s_suppressed.Contains(key))
            {
                return true;
            }

            if (key.StartsWith("$"))
            {
                return s_suppressed.Contains(key.Substring(1));
            }

            return s_suppressed.Contains("$" + key);
        }

        public static string KeyFromRecipe(Recipe recipe)
        {
            if (recipe == null || recipe.m_item == null || recipe.m_item.m_itemData == null || recipe.m_item.m_itemData.m_shared == null)
            {
                return null;
            }

            return recipe.m_item.m_itemData.m_shared.m_name;
        }

        public static string KeyFromPiece(Piece piece)
        {
            return piece != null ? piece.m_name : null;
        }

        public static void NoteLearned(string key)
        {
            if (string.IsNullOrEmpty(key) || s_suppressed.Contains(key))
            {
                return;
            }

            s_learnSeq++;
            s_learnedSeq[key] = s_learnSeq;
            s_lastKnownCount = -1;
        }

        private static HashSet<string> KnownRecipes(Player player)
        {
            return player != null ? player.GetFieldValue<HashSet<string>>("m_knownRecipes") : null;
        }

        private static HashSet<string> KnownMaterials(Player player)
        {
            return player != null ? player.GetFieldValue<HashSet<string>>("m_knownMaterial") : null;
        }

        private static Dictionary<string, int> KnownStations(Player player)
        {
            return player != null ? player.GetFieldValue<Dictionary<string, int>>("m_knownStations") : null;
        }

        private static void ForgetAllItems()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> materials = KnownMaterials(player);
            if (materials == null)
            {
                return;
            }

            int count = materials.Count;
            materials.Clear();
            PersistKnowledge(player);
            Notify(player, count);
        }

        private static void DiscoverAllItems()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> materials = KnownMaterials(player);
            if (materials == null || ObjectDB.instance == null || ObjectDB.instance.m_items == null)
            {
                return;
            }

            int added = 0;
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }

                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    continue;
                }

                string name = drop.m_itemData.m_shared.m_name;
                if (!string.IsNullOrEmpty(name) && materials.Add(name))
                {
                    added++;
                }
            }

            PersistKnowledge(player, false);
            Notify(player, added);
        }

        private static void ForgetAllRecipes()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> recipes = KnownRecipes(player);
            if (recipes == null)
            {
                return;
            }

            EnsureCatalog();
            HashSet<string> materials = KnownMaterials(player);
            SuppressQualified(recipes, materials);
            int count = recipes.Count;
            recipes.Clear();

            s_selected.Clear();
            s_lastKnownCount = -1;
            PersistKnowledge(player);
            recipes.Clear();
            Notify(player, count);
        }

        private static void SuppressKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            s_suppressed.Add(key);
            s_learnedSeq.Remove(key);
            if (key.StartsWith("$"))
            {
                string bare = key.Substring(1);
                s_suppressed.Add(bare);
                s_learnedSeq.Remove(bare);
            }
            else
            {
                s_suppressed.Add("$" + key);
                s_learnedSeq.Remove("$" + key);
            }
        }

        private static void SuppressQualified(HashSet<string> recipes, HashSet<string> materials)
        {
            foreach (string key in recipes)
            {
                SuppressKey(key);
            }

            for (int i = 0; i < s_catalog.Count; i++)
            {
                if (IngredientsKnown(s_catalog[i], materials))
                {
                    SuppressKey(s_catalog[i].key);
                }
            }
        }

        private static bool IngredientsKnown(RecipeEntry entry, HashSet<string> materials)
        {
            if (entry.materials == null)
            {
                return true;
            }

            for (int i = 0; i < entry.materials.Count; i++)
            {
                string name = entry.materials[i];
                if (string.IsNullOrEmpty(name) || SameMaterial(name, entry.key))
                {
                    continue;
                }

                if (materials == null || !MaterialKnown(materials, name))
                {
                    return false;
                }
            }

            return true;
        }

        public static void AllowRecipesForNewMaterial(string material)
        {
            if (string.IsNullOrEmpty(material))
            {
                return;
            }

            HashSet<string> known = KnownMaterials(Player.m_localPlayer);
            if (known != null && MaterialKnown(known, material))
            {
                return;
            }

            EnsureCatalog();
            for (int i = 0; i < s_catalog.Count; i++)
            {
                RecipeEntry entry = s_catalog[i];
                if (entry.manualOnly || entry.materials == null)
                {
                    continue;
                }

                for (int n = 0; n < entry.materials.Count; n++)
                {
                    if (SameMaterial(entry.materials[n], material))
                    {
                        ReleaseSuppressed(entry.key);
                        break;
                    }
                }
            }
        }

        private static void ReleaseSuppressed(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            s_suppressed.Remove(key);
            if (key.StartsWith("$"))
            {
                s_suppressed.Remove(key.Substring(1));
            }
            else
            {
                s_suppressed.Remove("$" + key);
            }
        }

        private static bool MaterialKnown(HashSet<string> materials, string name)
        {
            if (materials.Contains(name))
            {
                return true;
            }

            if (name.StartsWith("$"))
            {
                return materials.Contains(name.Substring(1));
            }

            return materials.Contains("$" + name);
        }

        private static bool SameMaterial(string a, string b)
        {
            return string.Equals(BareName(a), BareName(b), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string BareName(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            return key.StartsWith("$") ? key.Substring(1) : key;
        }

        private static void LearnAllRecipes()
        {
            EnsureCatalog();
            List<string> keys = new List<string>(s_catalog.Count);
            for (int i = 0; i < s_catalog.Count; i++)
            {
                keys.Add(s_catalog[i].key);
            }
            Notify(Player.m_localPlayer, LearnKeys(keys));
        }

        private static void DrawVendorButtons(bool learn)
        {
            EnsureCatalog();
            if (s_vendorOrder.Count == 0)
            {
                return;
            }

            const float rowHeight = 30f;
            const float gap = 8f;
            string tipCode = learn ? "$vt_recipe_vendor_learn" : "$vt_recipe_vendor_select";
            for (int i = 0; i < s_vendorOrder.Count; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                string leftId = s_vendorOrder[i];
                if (learn)
                {
                    if (Controls.ActionButtonAt(left, VendorLabel(leftId), FeatureMethod.Direct, tipCode))
                    {
                        LearnVendor(leftId);
                    }
                }
                else if (GUI.Button(left, new GUIContent(VendorLabel(leftId), Controls.Tip(tipCode))))
                {
                    SelectVisibleVendor(leftId);
                }

                if (i + 1 >= s_vendorOrder.Count)
                {
                    GUILayout.Space(4);
                    continue;
                }

                string rightId = s_vendorOrder[i + 1];
                if (learn)
                {
                    if (Controls.ActionButtonAt(right, VendorLabel(rightId), FeatureMethod.Direct, tipCode))
                    {
                        LearnVendor(rightId);
                    }
                }
                else if (GUI.Button(right, new GUIContent(VendorLabel(rightId), Controls.Tip(tipCode))))
                {
                    SelectVisibleVendor(rightId);
                }

                GUILayout.Space(4);
            }
        }

        private static void LearnVendor(string vendorId)
        {
            EnsureCatalog();
            List<string> keys = new List<string>();
            for (int i = 0; i < s_catalog.Count; i++)
            {
                if (s_catalog[i].vendor == vendorId && !s_catalog[i].manualOnly)
                {
                    keys.Add(s_catalog[i].key);
                }
            }

            if (s_learnBiomeItems)
            {
                DiscoverVendorItems(vendorId);
            }

            Notify(Player.m_localPlayer, LearnKeys(keys));
        }

        private static void DiscoverVendorItems(string vendorId)
        {
            Player player = Player.m_localPlayer;
            HashSet<string> materials = KnownMaterials(player);
            if (materials == null || s_vendorByKey == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in s_vendorByKey)
            {
                if (pair.Value == vendorId && !string.IsNullOrEmpty(pair.Key) && !pair.Key.ToLowerInvariant().Contains("mysterious"))
                {
                    materials.Add(pair.Key);
                }
            }

            PersistKnowledge(player, false);
        }

        private static void SelectVisibleVendor(string vendorId)
        {
            bool allSelected = true;
            int matches = 0;
            for (int i = 0; i < s_visible.Count; i++)
            {
                if (s_visible[i].vendor != vendorId)
                {
                    continue;
                }

                matches++;
                if (!s_selected.Contains(s_visible[i].key))
                {
                    allSelected = false;
                }
            }

            if (matches == 0)
            {
                return;
            }

            for (int i = 0; i < s_visible.Count; i++)
            {
                if (s_visible[i].vendor != vendorId)
                {
                    continue;
                }

                if (allSelected)
                {
                    s_selected.Remove(s_visible[i].key);
                }
                else
                {
                    s_selected.Add(s_visible[i].key);
                }
            }
        }

        private static string VendorLabel(string vendorId)
        {
            string label;
            if (vendorId != null && s_vendorLabel.TryGetValue(vendorId, out label) && !string.IsNullOrEmpty(label))
            {
                return label;
            }

            return vendorId;
        }

        private static void EnsureVendors()
        {
            if (s_vendorsReady || ZNetScene.instance == null || ZNetScene.instance.m_prefabs == null)
            {
                return;
            }

            s_vendorByKey = new Dictionary<string, string>(StringComparer.Ordinal);
            s_vendorOrder.Clear();
            s_vendorLabel.Clear();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                Trader trader = prefab.GetComponent<Trader>();
                if (trader == null)
                {
                    continue;
                }

                string id = prefab.name;
                if (!s_vendorLabel.ContainsKey(id))
                {
                    string raw = string.IsNullOrEmpty(trader.m_name) ? prefab.name : trader.m_name;
                    string label = Localization.instance != null ? Localization.instance.Localize(raw) : raw;
                    if (string.IsNullOrEmpty(label) || label.StartsWith("$") || (label.Length > 2 && label[0] == '[' && label[label.Length - 1] == ']'))
                    {
                        label = prefab.name;
                    }

                    s_vendorLabel[id] = label;
                    s_vendorOrder.Add(id);
                }

                if (trader.m_items != null)
                {
                    for (int i = 0; i < trader.m_items.Count; i++)
                    {
                        Trader.TradeItem trade = trader.m_items[i];
                        RegisterVendorItem(id, trade != null ? trade.m_prefab : null);
                    }
                }

                if (trader.m_useItems != null)
                {
                    for (int i = 0; i < trader.m_useItems.Count; i++)
                    {
                        Trader.TraderUseItem useItem = trader.m_useItems[i];
                        RegisterVendorItem(id, useItem != null ? useItem.m_prefab : null);
                    }
                }
            }

            s_vendorsReady = true;
        }

        private static void RegisterVendorItem(string vendorId, ItemDrop drop)
        {
            if (drop == null)
            {
                return;
            }

            RememberVendor(vendorId, drop.name);
            Piece ownPiece = drop.GetComponent<Piece>();
            if (ownPiece != null)
            {
                RememberVendor(vendorId, ownPiece.m_name);
            }

            if (drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                return;
            }

            RememberVendor(vendorId, drop.m_itemData.m_shared.m_name);
            PieceTable table = drop.m_itemData.m_shared.m_buildPieces;
            if (table == null || table.m_pieces == null)
            {
                return;
            }

            for (int i = 0; i < table.m_pieces.Count; i++)
            {
                GameObject piecePrefab = table.m_pieces[i];
                if (piecePrefab == null)
                {
                    continue;
                }

                RememberVendor(vendorId, piecePrefab.name);
                Piece piece = piecePrefab.GetComponent<Piece>();
                if (piece != null)
                {
                    RememberVendor(vendorId, piece.m_name);
                }
            }
        }

        private static void RememberVendor(string vendorId, string key)
        {
            if (string.IsNullOrEmpty(key) || s_vendorByKey.ContainsKey(key))
            {
                return;
            }

            s_vendorByKey[key] = vendorId;
            if (key.StartsWith("$"))
            {
                string bare = key.Substring(1);
                if (!s_vendorByKey.ContainsKey(bare))
                {
                    s_vendorByKey[bare] = vendorId;
                }
            }
            else if (!s_vendorByKey.ContainsKey("$" + key))
            {
                s_vendorByKey["$" + key] = vendorId;
            }
        }

        private static string VendorFor(string key, string prefabName)
        {
            string vendor = VendorForKey(key);
            if (vendor != null)
            {
                return vendor;
            }

            return VendorForKey(prefabName);
        }

        private static string VendorFor(string key)
        {
            return VendorForKey(key);
        }

        private static string VendorForKey(string key)
        {
            if (s_vendorByKey == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            string vendor;
            if (s_vendorByKey.TryGetValue(key, out vendor))
            {
                return vendor;
            }

            if (key.StartsWith("$"))
            {
                s_vendorByKey.TryGetValue(key.Substring(1), out vendor);
                return vendor;
            }

            s_vendorByKey.TryGetValue("$" + key, out vendor);
            return vendor;
        }

        private static void LearnBiome(RecipeBiome biome)
        {
            EnsureCatalog();
            List<string> keys = new List<string>();
            for (int i = 0; i < s_catalog.Count; i++)
            {
                if (s_catalog[i].biome == biome && string.IsNullOrEmpty(s_catalog[i].vendor) && !s_catalog[i].manualOnly)
                {
                    keys.Add(s_catalog[i].key);
                }
            }
            if (s_learnBiomeItems)
            {
                DiscoverBiomeMaterials(biome);
            }
            Notify(Player.m_localPlayer, LearnKeys(keys));
        }

        private static void DiscoverBiomeMaterials(RecipeBiome biome)
        {
            Player player = Player.m_localPlayer;
            HashSet<string> materials = KnownMaterials(player);
            if (materials == null)
            {
                return;
            }

            for (int i = 0; i < s_catalog.Count; i++)
            {
                RecipeEntry entry = s_catalog[i];
                if (entry.biome != biome || !string.IsNullOrEmpty(entry.vendor) || entry.manualOnly || entry.materials == null)
                {
                    continue;
                }

                for (int n = 0; n < entry.materials.Count; n++)
                {
                    if (!string.IsNullOrEmpty(entry.materials[n]))
                    {
                        materials.Add(entry.materials[n]);
                    }
                }
            }

            if (ObjectDB.instance != null && ObjectDB.instance.m_items != null)
            {
                foreach (GameObject prefab in ObjectDB.instance.m_items)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    {
                        continue;
                    }

                    string name = drop.m_itemData.m_shared.m_name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (VendorFor(name) != null || name.ToLowerInvariant().Contains("mysterious") || ContainsAny(name.ToLowerInvariant(), "barber", "ironpit", "iron_pit", "firepit_iron"))
                    {
                        continue;
                    }

                    string display = Localization.instance != null ? Localization.instance.Localize(name) : name;
                    RecipeBiome itemBiome = Classify("none", 1, prefab.name + " " + name + " " + display);
                    if (itemBiome == biome)
                    {
                        materials.Add(name);
                    }
                }
            }

            PersistKnowledge(player, false);
        }

        private static void SelectVisibleBiome(RecipeBiome biome)
        {
            bool allSelected = true;
            int matches = 0;
            for (int i = 0; i < s_visible.Count; i++)
            {
                if (s_visible[i].biome != biome || !string.IsNullOrEmpty(s_visible[i].vendor) || s_visible[i].manualOnly)
                {
                    continue;
                }

                matches++;
                if (!s_selected.Contains(s_visible[i].key))
                {
                    allSelected = false;
                }
            }

            if (matches == 0)
            {
                return;
            }

            for (int i = 0; i < s_visible.Count; i++)
            {
                if (s_visible[i].biome != biome || !string.IsNullOrEmpty(s_visible[i].vendor) || s_visible[i].manualOnly)
                {
                    continue;
                }

                if (allSelected)
                {
                    s_selected.Remove(s_visible[i].key);
                }
                else
                {
                    s_selected.Add(s_visible[i].key);
                }
            }
        }

        private static void LearnSelected()
        {
            if (s_selected.Count == 0)
            {
                return;
            }

            int added = LearnKeys(s_selected);
            s_selected.Clear();
            Notify(Player.m_localPlayer, added);
        }

        private static void ForgetAllStations()
        {
            Player player = Player.m_localPlayer;
            Dictionary<string, int> stations = KnownStations(player);
            if (stations == null)
            {
                return;
            }

            int count = stations.Count;
            stations.Clear();
            PersistKnowledge(player);
            Notify(player, count);
        }

        private static void LearnAllStations()
        {
            Player player = Player.m_localPlayer;
            Dictionary<string, int> stations = KnownStations(player);
            if (stations == null)
            {
                return;
            }

            int added = 0;
            Dictionary<string, int> catalog = CollectStations();
            foreach (KeyValuePair<string, int> pair in catalog)
            {
                int current;
                if (!stations.TryGetValue(pair.Key, out current))
                {
                    stations[pair.Key] = pair.Value;
                    added++;
                }
                else if (pair.Value > current)
                {
                    stations[pair.Key] = pair.Value;
                    added++;
                }
            }

            PersistKnowledge(player);
            Notify(player, added);
        }

        private static Dictionary<string, int> CollectStations()
        {
            var levels = new Dictionary<string, int>(StringComparer.Ordinal);
            if (ObjectDB.instance != null && ObjectDB.instance.m_recipes != null)
            {
                foreach (Recipe recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe == null)
                    {
                        continue;
                    }

                    NoteStation(levels, recipe.m_craftingStation, recipe.m_minStationLevel);
                    NoteStation(levels, recipe.m_repairStation, 1);
                }
            }

            if (ZNetScene.instance != null && ZNetScene.instance.m_prefabs != null)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                    CraftingStation station = prefab.GetComponent<CraftingStation>();
                    NoteStation(levels, station, 1);
                }
            }

            return levels;
        }

        private static void NoteStation(Dictionary<string, int> levels, CraftingStation station, int level)
        {
            if (station == null || string.IsNullOrEmpty(station.m_name))
            {
                return;
            }

            int next = Mathf.Max(1, level);
            int current;
            if (!levels.TryGetValue(station.m_name, out current) || next > current)
            {
                levels[station.m_name] = next;
            }
        }

        private static void ForgetSelected()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> recipes = KnownRecipes(player);
            if (recipes == null)
            {
                return;
            }

            int removed = 0;
            foreach (string key in s_selected.ToArray())
            {
                if (recipes.Remove(key))
                {
                    removed++;
                }
                SuppressKey(key);
            }

            s_selected.Clear();
            s_lastKnownCount = -1;
            PersistKnowledge(player);
            Notify(player, removed);
        }

        private static int LearnKeys(IEnumerable<string> keys)
        {
            Player player = Player.m_localPlayer;
            HashSet<string> recipes = KnownRecipes(player);
            if (recipes == null || keys == null)
            {
                return 0;
            }

            int added = 0;
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                s_suppressed.Remove(key);
                if (key.StartsWith("$"))
                {
                    s_suppressed.Remove(key.Substring(1));
                }
                else
                {
                    s_suppressed.Remove("$" + key);
                }
                if (recipes.Add(key))
                {
                    added++;
                }
                NoteLearned(key);
            }

            s_lastKnownCount = -1;
            PersistKnowledge(player);
            return added;
        }

        private static void PersistKnowledge(Player player, bool refreshPieces = true)
        {
            if (player != null && refreshPieces)
            {
                player.CallMethod("UpdateAvailablePiecesList");
            }

            RefreshCraftingPanel();

            if (Game.instance != null)
            {
                Game.instance.SavePlayerProfile(false, false);
            }
        }

        private static void RefreshCraftingPanel()
        {
            if (InventoryGui.instance == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] methods = typeof(InventoryGui).GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "UpdateCraftingPanel")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                try
                {
                    if (parameters.Length == 0)
                    {
                        method.Invoke(InventoryGui.instance, null);
                        return;
                    }
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
                    {
                        method.Invoke(InventoryGui.instance, new object[] { false });
                        return;
                    }
                }
                catch
                {
                }
            }
        }

        private static void Notify(Player player, int count)
        {
            if (player != null)
            {
                player.VTSendMessage(VTLocalization.instance.Localize("$vt_recipe_updated") + " " + count);
            }
        }

        private static void RebuildVisible()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> known = KnownRecipes(player);
            int knownCount = known != null ? known.Count : 0;
            string search = s_searchTerms == null ? "" : s_searchTerms.ToLowerInvariant();
            if (s_previousSearch == search && s_previousTab == s_tabIdx && s_lastKnownCount == knownCount)
            {
                return;
            }

            List<RecipeEntry> source = new List<RecipeEntry>();
            if (s_tabIdx == 1)
            {
                for (int i = 0; i < s_catalog.Count; i++)
                {
                    RecipeEntry entry = s_catalog[i];
                    if (known == null || !known.Contains(entry.key))
                    {
                        source.Add(entry);
                    }
                }
            }
            else if (known != null)
            {
                foreach (string key in known)
                {
                    RecipeEntry entry;
                    if (!s_catalogByKey.TryGetValue(key, out entry))
                    {
                        entry = UnknownEntry(key);
                    }

                    if (s_tabIdx == 2 && !s_learnedSeq.ContainsKey(key))
                    {
                        continue;
                    }

                    source.Add(entry);
                }
            }

            if (search.Length > 0)
            {
                source = source.Where(entry => entry.displayName.ToLowerInvariant().Contains(search) || entry.key.ToLowerInvariant().Contains(search)).ToList();
            }

            if (s_tabIdx == 2)
            {
                source.Sort(CompareRecent);
            }
            else
            {
                source.Sort(CompareUnlocked);
            }

            s_visible = source;
            s_previousSearch = search;
            s_previousTab = s_tabIdx;
            s_lastKnownCount = knownCount;
        }

        private static int CompareRecent(RecipeEntry a, RecipeEntry b)
        {
            int seqA;
            int seqB;
            s_learnedSeq.TryGetValue(a.key, out seqA);
            s_learnedSeq.TryGetValue(b.key, out seqB);
            int seq = seqB.CompareTo(seqA);
            if (seq != 0)
            {
                return seq;
            }

            return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareUnlocked(RecipeEntry a, RecipeEntry b)
        {
            bool aVendor = !string.IsNullOrEmpty(a.vendor);
            bool bVendor = !string.IsNullOrEmpty(b.vendor);
            if (aVendor != bVendor)
            {
                return aVendor ? 1 : -1;
            }

            if (aVendor)
            {
                int vendor = string.Compare(VendorLabel(a.vendor), VendorLabel(b.vendor), StringComparison.OrdinalIgnoreCase);
                if (vendor != 0)
                {
                    return vendor;
                }
            }
            else
            {
                int biome = a.biome.CompareTo(b.biome);
                if (biome != 0)
                {
                    return biome;
                }
            }

            return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        }

        private static RecipeEntry UnknownEntry(string key)
        {
            return new RecipeEntry
            {
                key = key,
                displayName = Localization.instance != null ? Localization.instance.Localize(key) : key,
                icon = GetPlaceholderIcon(),
                biome = RecipeBiome.Special,
                hasIcon = false
            };
        }

        private static void EnsureCatalog()
        {
            int recipeCount = ObjectDB.instance != null && ObjectDB.instance.m_recipes != null ? ObjectDB.instance.m_recipes.Count : 0;
            bool sceneReady = ZNetScene.instance != null && ZNetScene.instance.m_prefabs != null;
            if (s_catalog.Count > 0 && recipeCount == s_lastRecipeCount && (s_vendorsReady || !sceneReady))
            {
                return;
            }
            if (ObjectDB.instance == null || recipeCount == 0)
            {
                return;
            }

            s_catalog.Clear();
            s_catalogByKey.Clear();
            s_lastRecipeCount = recipeCount;
            EnsureVendors();

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                string key = KeyFromRecipe(recipe);
                if (string.IsNullOrEmpty(key) || s_catalogByKey.ContainsKey(key))
                {
                    continue;
                }

                ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
                string display = Localization.instance != null ? Localization.instance.Localize(shared.m_name) : shared.m_name;
                Texture icon = TextureFromItem(recipe.m_item.m_itemData);
                AddEntry(new RecipeEntry
                {
                    key = key,
                    displayName = display,
                    icon = icon,
                    biome = ClassifyRecipe(recipe, display),
                    vendor = VendorFor(key, recipe.m_item != null ? recipe.m_item.name : null),
                    hasIcon = icon != GetPlaceholderIcon(),
                    materials = ItemNames(recipe.m_item, recipe.m_resources)
                });
            }

            HashSet<GameObject> piecePrefabs = new HashSet<GameObject>();
            if (ObjectDB.instance.m_items != null)
            {
                foreach (GameObject item in ObjectDB.instance.m_items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    ItemDrop drop = item.GetComponent<ItemDrop>();
                    PieceTable table = drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null
                        ? drop.m_itemData.m_shared.m_buildPieces
                        : null;
                    if (table == null || table.m_pieces == null)
                    {
                        continue;
                    }

                    foreach (GameObject prefab in table.m_pieces)
                    {
                        if (prefab != null)
                        {
                            piecePrefabs.Add(prefab);
                        }
                    }
                }
            }

            if (ZNetScene.instance != null && ZNetScene.instance.m_prefabs != null)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                Piece piece = prefab.GetComponent<Piece>();
                if (piece == null || piece.m_icon == null)
                {
                    continue;
                }

                if (piecePrefabs.Contains(prefab) || IsSpecialText(PieceText(piece, prefab)))
                {
                    piecePrefabs.Add(prefab);
                }
                }
            }

            foreach (GameObject prefab in piecePrefabs)
            {
                Piece piece = prefab.GetComponent<Piece>();
                string key = KeyFromPiece(piece);
                if (piece == null || string.IsNullOrEmpty(key) || s_catalogByKey.ContainsKey(key))
                {
                    continue;
                }

                string display = Localization.instance != null ? Localization.instance.Localize(piece.m_name) : piece.m_name;
                Texture icon = TextureFromSprite(piece.m_icon);
                AddEntry(new RecipeEntry
                {
                    key = key,
                    displayName = display,
                    icon = icon,
                    biome = ClassifyPiece(piece, prefab, display),
                    vendor = VendorFor(key, prefab != null ? prefab.name : null),
                    hasIcon = icon != GetPlaceholderIcon(),
                    materials = ItemNames(null, piece.m_resources)
                });
            }
        }

        private static void AddEntry(RecipeEntry entry)
        {
            StampGroup(entry);
            s_catalog.Add(entry);
            s_catalogByKey[entry.key] = entry;
        }

        private static void StampGroup(RecipeEntry entry)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append(entry.key).Append(' ').Append(entry.displayName);
            if (entry.materials != null)
            {
                for (int i = 0; i < entry.materials.Count; i++)
                {
                    builder.Append(' ').Append(entry.materials[i]);
                    AppendLocalized(builder, entry.materials[i]);
                }
            }

            string hay = builder.ToString().ToLowerInvariant();
            if (ContainsAny(hay, "mysterious"))
            {
                entry.manualOnly = true;
                entry.vendor = null;
                return;
            }

            if (string.IsNullOrEmpty(entry.vendor) && ContainsAny(hay, "barber", "ironpit", "iron_pit", "firepit_iron", "ironfirepit", "iron fire pit", "iron firepit", "iron pit"))
            {
                entry.vendor = "Hildir";
                if (!s_vendorLabel.ContainsKey("Hildir"))
                {
                    s_vendorLabel["Hildir"] = "Hildir";
                    s_vendorOrder.Add("Hildir");
                }
            }

            if (!string.IsNullOrEmpty(entry.vendor))
            {
                return;
            }

            if (ContainsSap(hay) || ContainsAny(hay, "blackforge_ext2", "vise", "vice", "hare", "jute", "bile"))
            {
                entry.biome = RecipeBiome.Mistlands;
            }
            else if (ContainsAny(hay, "vile", "vilebone"))
            {
                entry.biome = RecipeBiome.Plains;
            }
            else if (ContainsAny(hay, "snowball", "snowlantern", "snow_lantern", "snow shovel", "snowshovel"))
            {
                entry.biome = RecipeBiome.DeepNorth;
            }
            else if (ContainsAny(hay, "pot_small_green", "pot_medium_green", "pot_large_green", "shieldgenerator"))
            {
                entry.biome = RecipeBiome.Ashlands;
            }
        }

        private static RecipeBiome ClassifyRecipe(Recipe recipe, string display)
        {
            string station = recipe.m_craftingStation != null ? recipe.m_craftingStation.name : "";
            int level = recipe.m_minStationLevel;
            string text = RecipeText(recipe, display);
            return Classify(station, level, text);
        }

        private static RecipeBiome ClassifyPiece(Piece piece, GameObject prefab, string display)
        {
            string station = piece.m_craftingStation != null ? piece.m_craftingStation.name : "";
            string text = PieceText(piece, prefab) + " " + display;
            return Classify(station, 1, text);
        }

        private static string RecipeText(Recipe recipe, string display)
        {
            ItemDrop item = recipe.m_item;
            string prefab = item != null ? item.name : "";
            string shared = item != null && item.m_itemData != null && item.m_itemData.m_shared != null
                ? item.m_itemData.m_shared.m_name
                : "";
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append(prefab).Append(' ').Append(shared).Append(' ').Append(display);
            AppendLocalized(builder, shared);
            if (recipe.m_resources != null)
            {
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    AppendRequirement(builder, requirement);
                }
            }

            return builder.ToString();
        }

        private static string PieceText(Piece piece, GameObject prefab)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (prefab != null)
            {
                builder.Append(prefab.name);
            }
            if (piece != null)
            {
                builder.Append(' ').Append(piece.m_name);
                AppendLocalized(builder, piece.m_name);
                if (piece.m_resources != null)
                {
                    foreach (Piece.Requirement requirement in piece.m_resources)
                    {
                        AppendRequirement(builder, requirement);
                    }
                }
            }

            return builder.ToString();
        }

        private static void AppendRequirement(System.Text.StringBuilder builder, Piece.Requirement requirement)
        {
            if (requirement == null || requirement.m_resItem == null)
            {
                return;
            }

            builder.Append(' ').Append(requirement.m_resItem.name);
            if (requirement.m_resItem.m_itemData == null || requirement.m_resItem.m_itemData.m_shared == null)
            {
                return;
            }

            string shared = requirement.m_resItem.m_itemData.m_shared.m_name;
            builder.Append(' ').Append(shared);
            AppendLocalized(builder, shared);
        }

        private static void AppendLocalized(System.Text.StringBuilder builder, string raw)
        {
            if (string.IsNullOrEmpty(raw) || Localization.instance == null)
            {
                return;
            }

            string shown = Localization.instance.Localize(raw);
            if (!string.IsNullOrEmpty(shown) && !string.Equals(shown, raw, System.StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(' ').Append(shown);
            }
        }

        private static RecipeBiome Classify(string station, int level, string text)
        {
            string haystack = ((station ?? "") + " " + (text ?? "")).ToLowerInvariant();
            if (IsSpecialText(haystack))
            {
                return RecipeBiome.Special;
            }
            if (ContainsAny(haystack, "deepnorth", "deep_north", "deep north", "fimbul", "northlands", "bloodgold", "frostcore", "frostfoundry", "frostkiln", "frigid", "jotun", "elaking", "timberwood", "timber wood", "moose", "seal", "nornthread", "frozenfuel", "frozenking", "crownjewel", "jotunpuff", "jotunbane", "spicedeepnorth", "feastdeepnorth", "liquidfrost", "liquid core", "liquidcore", "liquid_core", "frostfire", "frostorb", "frostwood", "woodfrost", "orbofahri", "spiritcaller", "thunderblood", "stafficeshard", "icecube", "item_ice", "item_mold", "mold_", "mould", "barka", "gammeltroll", "gammel", "hexen", "krigen", "captive"))
            {
                return RecipeBiome.DeepNorth;
            }
            if (ContainsAny(haystack, "flametal", "asksvin", "charred", "ashland", "volture", "morgen", "berserkir", "grausten", "sulfur", "bonemaw", "ashwood", "ember", "fader", "lavai", "putrid", "celestial"))
            {
                return RecipeBiome.Ashlands;
            }
            if (ContainsSap(haystack) || ContainsAny(haystack, "eitr", "carapace", "mistland", "ygg", "dvergr", "seeker", "gjall", "softtissue", "royaljelly", "wisp", "blackmarble", "refinedeitr", "mistwalker", "feathercape", "feather_cape", "bile"))
            {
                return RecipeBiome.Mistlands;
            }
            if (ContainsAny(haystack, "serpent", "chitin", "abyssal", "leviathan", "serpentscale", "serpentstew", "seaserpent"))
            {
                return RecipeBiome.Ocean;
            }
            if (ContainsAny(haystack, "blackmetal", "padded", "lox", "tar", "goblin", "fuling", "plains", "needle", "linen", "flax", "barley", "darkwood", "deathsquito", "bloodpudding", "vile", "vilebone"))
            {
                return RecipeBiome.Plains;
            }
            if (ContainsAny(haystack, "silver", "wolf", "frostner", "mountain", "crystal", "fenring", "fenris", "drake", "obsidian", "freeze", "onion", "dragontear", "golem"))
            {
                return RecipeBiome.Mountains;
            }
            if (ContainsAny(haystack, "iron", "rootarmor", "ancientbark", "ancient_bark", "guck", "withered", "abomination", "swamp", "turnip", "sunken", "draugr", "leech", "ironnail", "iron_nail"))
            {
                return RecipeBiome.Swamp;
            }
            if (ContainsAny(haystack, "bronze", "troll", "finewood", "copper", "tin", "corewood", "core_wood", "carrot", "greydwarf", "surtling", "bronzenail"))
            {
                return RecipeBiome.BlackForest;
            }

            RecipeBiome fromStation = FromStation(station, level);
            if (fromStation != RecipeBiome.Special)
            {
                return fromStation;
            }
            if (ContainsAny(haystack, "flint", "leather", "deer", "boar", "neck", "antler", "crude", "meadow", "raspberry", "campfire", "wood", "stone"))
            {
                return RecipeBiome.Meadows;
            }

            return RecipeBiome.Special;
        }

        private static RecipeBiome FromStation(string station, int level)
        {
            if (string.IsNullOrEmpty(station))
            {
                return RecipeBiome.Meadows;
            }

            string name = station.ToLowerInvariant();
            if (ContainsAny(name, "blackforge", "galdr", "magetable", "gemcutter", "eitrrefinery"))
            {
                return RecipeBiome.Mistlands;
            }
            if (ContainsAny(name, "artisan", "windmill", "spinning"))
            {
                return RecipeBiome.Plains;
            }
            if (name.Contains("cauldron") || name.Contains("oven"))
            {
                if (level >= 6)
                {
                    return RecipeBiome.Mistlands;
                }
                if (level >= 5)
                {
                    return RecipeBiome.Plains;
                }
                if (level >= 4)
                {
                    return RecipeBiome.Mountains;
                }
                if (level >= 3)
                {
                    return RecipeBiome.Swamp;
                }
                if (level >= 2)
                {
                    return RecipeBiome.BlackForest;
                }
                return RecipeBiome.Meadows;
            }
            if (name.Contains("forge"))
            {
                if (level >= 4)
                {
                    return RecipeBiome.Plains;
                }
                if (level >= 3)
                {
                    return RecipeBiome.Mountains;
                }
                if (level >= 2)
                {
                    return RecipeBiome.Swamp;
                }
                return RecipeBiome.BlackForest;
            }
            if (name.Contains("workbench"))
            {
                if (level >= 5)
                {
                    return RecipeBiome.Mistlands;
                }
                if (level >= 4)
                {
                    return RecipeBiome.Mountains;
                }
                if (level >= 3)
                {
                    return RecipeBiome.Swamp;
                }
                if (level >= 2)
                {
                    return RecipeBiome.BlackForest;
                }
                return RecipeBiome.Meadows;
            }
            if (name.Contains("stonecutter"))
            {
                return RecipeBiome.BlackForest;
            }

            return RecipeBiome.Special;
        }

        private static bool IsSpecialText(string text)
        {
            return ContainsAny(text.ToLowerInvariant(),
                "maypole", "midsummer", "yule", "xmas", "christmas", "halloween", "jackoturnip", "jacko",
                "firecracker", "firework", "gift1", "gift2", "gift3", "mistletoe", "festive", "seasonal",
                "treasurechest", "cargocrate", "yuletree", "xmastree", "xmas_tree", "christmasgift");
        }

        private static bool ContainsSap(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int index = 0;
            while ((index = text.IndexOf("sap", index, System.StringComparison.Ordinal)) >= 0)
            {
                bool sapling = text.Length >= index + 7 && string.Compare(text, index, "sapling", 0, 7, System.StringComparison.Ordinal) == 0;
                if (!sapling)
                {
                    return true;
                }

                index += 7;
            }

            return false;
        }

        private static bool ContainsAny(string text, params string[] parts)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (text.Contains(parts[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string EmptyLabel()
        {
            if (s_tabIdx == 1)
            {
                return "$vt_recipe_locked_empty";
            }
            if (s_tabIdx == 2)
            {
                return "$vt_recipe_recent_empty";
            }

            return "$vt_recipe_unlocked_empty";
        }

        private static string BiomeLabel(RecipeBiome biome)
        {
            switch (biome)
            {
                case RecipeBiome.Meadows:
                    return "$vt_recipe_biome_meadows";
                case RecipeBiome.BlackForest:
                    return "$vt_recipe_biome_blackforest";
                case RecipeBiome.Swamp:
                    return "$vt_recipe_biome_swamp";
                case RecipeBiome.Mountains:
                    return "$vt_recipe_biome_mountains";
                case RecipeBiome.Plains:
                    return "$vt_recipe_biome_plains";
                case RecipeBiome.Mistlands:
                    return "$vt_recipe_biome_mistlands";
                case RecipeBiome.Ashlands:
                    return "$vt_recipe_biome_ashlands";
                case RecipeBiome.Ocean:
                    return "$vt_recipe_biome_ocean";
                case RecipeBiome.DeepNorth:
                    return "$vt_recipe_biome_deepnorth";
                default:
                    return "$vt_recipe_biome_special";
            }
        }

        private static Texture TextureFromItem(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return GetPlaceholderIcon();
            }

            try
            {
                Sprite sprite = item.GetIcon();
                Texture texture = TextureFromSprite(sprite);
                if (texture != GetPlaceholderIcon())
                {
                    return texture;
                }
            }
            catch
            {
            }

            if (item.m_shared != null && item.m_shared.m_icons != null && item.m_shared.m_icons.Length > 0)
            {
                return TextureFromSprite(item.m_shared.m_icons[0]);
            }

            return GetPlaceholderIcon();
        }

        private static Texture TextureFromSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return GetPlaceholderIcon();
            }

            try
            {
                Texture texture = SpriteManager.TextureFromSprite(sprite);
                return texture != null ? texture : GetPlaceholderIcon();
            }
            catch
            {
                return GetPlaceholderIcon();
            }
        }

        private static Texture2D GetPlaceholderIcon()
        {
            if (s_placeholderIcon == null)
            {
                s_placeholderIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(0.25f, 0.25f, 0.25f, 1f);
                }
                s_placeholderIcon.SetPixels(pixels);
                s_placeholderIcon.Apply();
            }

            return s_placeholderIcon;
        }

        private class RecipeEntry
        {
            public string key;
            public string displayName;
            public Texture icon;
            public RecipeBiome biome;
            public string vendor;
            public bool manualOnly;
            public bool hasIcon;
            public List<string> materials;
        }

        private static List<string> ItemNames(ItemDrop item, Piece.Requirement[] resources)
        {
            List<string> names = new List<string>();
            if (item != null && item.m_itemData != null && item.m_itemData.m_shared != null && !string.IsNullOrEmpty(item.m_itemData.m_shared.m_name))
            {
                names.Add(item.m_itemData.m_shared.m_name);
            }

            if (resources == null)
            {
                return names;
            }

            for (int i = 0; i < resources.Length; i++)
            {
                Piece.Requirement requirement = resources[i];
                if (requirement == null || requirement.m_resItem == null || requirement.m_resItem.m_itemData == null || requirement.m_resItem.m_itemData.m_shared == null)
                {
                    continue;
                }

                string name = requirement.m_resItem.m_itemData.m_shared.m_name;
                if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }
    }
}
