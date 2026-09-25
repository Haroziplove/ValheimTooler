using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ValheimTooler.Core.Extensions;
using ValheimTooler.UI;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
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

        private static readonly List<RecipeEntry> s_catalog = new List<RecipeEntry>();
        private static readonly Dictionary<string, RecipeEntry> s_catalogByKey = new Dictionary<string, RecipeEntry>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_selected = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_suppressed = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> s_learnedSeq = new Dictionary<string, int>(StringComparer.Ordinal);
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
            return !string.IsNullOrEmpty(key) && s_suppressed.Contains(key);
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
            int count = recipes.Count;
            foreach (string key in recipes)
            {
                s_suppressed.Add(key);
                s_learnedSeq.Remove(key);
            }
            for (int i = 0; i < s_catalog.Count; i++)
            {
                s_suppressed.Add(s_catalog[i].key);
            }

            recipes.Clear();

            s_selected.Clear();
            s_lastKnownCount = -1;
            PersistKnowledge(player);
            Notify(player, count);
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

        private static void LearnBiome(RecipeBiome biome)
        {
            EnsureCatalog();
            List<string> keys = new List<string>();
            for (int i = 0; i < s_catalog.Count; i++)
            {
                if (s_catalog[i].biome == biome)
                {
                    keys.Add(s_catalog[i].key);
                }
            }
            DiscoverBiomeMaterials(biome);
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
                if (entry.biome != biome || entry.materials == null)
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

            PersistKnowledge(player, false);
        }

        private static void SelectVisibleBiome(RecipeBiome biome)
        {
            bool allSelected = true;
            int matches = 0;
            for (int i = 0; i < s_visible.Count; i++)
            {
                if (s_visible[i].biome != biome)
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
                if (s_visible[i].biome != biome)
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
                s_suppressed.Add(key);
                s_learnedSeq.Remove(key);
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
            int biome = a.biome.CompareTo(b.biome);
            if (biome != 0)
            {
                return biome;
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
            if (s_catalog.Count > 0 && recipeCount == s_lastRecipeCount)
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
                    hasIcon = icon != GetPlaceholderIcon(),
                    materials = ItemNames(null, piece.m_resources)
                });
            }
        }

        private static void AddEntry(RecipeEntry entry)
        {
            s_catalog.Add(entry);
            s_catalogByKey[entry.key] = entry;
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
            if (recipe.m_resources != null)
            {
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    if (requirement == null || requirement.m_resItem == null)
                    {
                        continue;
                    }

                    builder.Append(' ').Append(requirement.m_resItem.name);
                    if (requirement.m_resItem.m_itemData != null && requirement.m_resItem.m_itemData.m_shared != null)
                    {
                        builder.Append(' ').Append(requirement.m_resItem.m_itemData.m_shared.m_name);
                    }
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
                if (piece.m_resources != null)
                {
                    foreach (Piece.Requirement requirement in piece.m_resources)
                    {
                        if (requirement == null || requirement.m_resItem == null)
                        {
                            continue;
                        }

                        builder.Append(' ').Append(requirement.m_resItem.name);
                    }
                }
            }

            return builder.ToString();
        }

        private static RecipeBiome Classify(string station, int level, string text)
        {
            string haystack = ((station ?? "") + " " + (text ?? "")).ToLowerInvariant();
            if (IsSpecialText(haystack))
            {
                return RecipeBiome.Special;
            }
            if (ContainsAny(haystack, "deepnorth", "deep_north", "deep north", "fimbul", "northlands"))
            {
                return RecipeBiome.DeepNorth;
            }
            if (ContainsAny(haystack, "flametal", "asksvin", "charred", "ashland", "volture", "morgen", "berserkir", "grausten", "sulfur", "bonemaw", "ashwood", "ember", "fader", "lavai", "putrid", "celestial"))
            {
                return RecipeBiome.Ashlands;
            }
            if (ContainsAny(haystack, "eitr", "carapace", "mistland", "ygg", "dvergr", "seeker", "gjall", "softtissue", "royaljelly", "wisp", "blackmarble", "refinedeitr", "mistwalker", "feathercape", "feather_cape"))
            {
                return RecipeBiome.Mistlands;
            }
            if (ContainsAny(haystack, "serpent", "chitin", "abyssal", "leviathan", "serpentscale", "serpentstew", "seaserpent"))
            {
                return RecipeBiome.Ocean;
            }
            if (ContainsAny(haystack, "blackmetal", "padded", "lox", "tar", "goblin", "fuling", "plains", "needle", "linen", "flax", "barley", "darkwood", "deathsquito", "bloodpudding"))
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
