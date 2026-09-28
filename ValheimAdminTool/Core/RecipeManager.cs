using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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
        private static string s_previousSearch;
        private static int s_previousTab = -1;
        private static int s_lastKnownCount = -1;
        private static int s_lastRecipeCount = -1;
        private static int s_learnSeq;

        private static readonly List<RecipeEntry> s_catalog = new List<RecipeEntry>();
        private static readonly Dictionary<string, RecipeEntry> s_catalogByKey = new Dictionary<string, RecipeEntry>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_selected = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> s_learnedSeq = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly List<string> s_vendorOrder = new List<string>();
        private static readonly Dictionary<string, string> s_vendorLabel = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<ItemDrop>> s_vendorGoods = new Dictionary<string, List<ItemDrop>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_vendorByItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> s_reportedMissing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool s_vendorsReady;
        private static List<RecipeEntry> s_visible = new List<RecipeEntry>();

        private static readonly GUIContent s_cellContent = new GUIContent();
        private static MethodInfo s_addKnownRecipe;
        private static MethodInfo s_addKnownPiece;
        private static MethodInfo s_updateKnownRecipes;

        private static readonly RecipeBiome[] s_learnOrder =
        {
            RecipeBiome.Meadows,
            RecipeBiome.BlackForest,
            RecipeBiome.Swamp,
            RecipeBiome.Mountains,
            RecipeBiome.Ocean,
            RecipeBiome.Plains,
            RecipeBiome.Mistlands,
            RecipeBiome.Ashlands,
            RecipeBiome.DeepNorth
        };

        private static readonly RecipeBiome[] s_selectOrder =
        {
            RecipeBiome.Meadows,
            RecipeBiome.BlackForest,
            RecipeBiome.Swamp,
            RecipeBiome.Mountains,
            RecipeBiome.Ocean,
            RecipeBiome.Plains,
            RecipeBiome.Mistlands,
            RecipeBiome.Ashlands,
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
            Controls.BeginSection("$vt_recipe_title", "$vt_recipe_hint");

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
                Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_recipes_confirm", ForgetRecipesAndItems);
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
            GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_biome_title"), SectionTitleStyle());

            const float rowHeight = 30f;
            const float gap = 8f;
            for (int i = 0; i < s_learnOrder.Length; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                if (Controls.ActionButtonAt(left, BiomeLabel(s_learnOrder[i]), FeatureMethod.Direct, "$vt_recipe_biome_learn"))
                {
                    LearnBiome(s_learnOrder[i]);
                }
                if (i + 1 < s_learnOrder.Length && Controls.ActionButtonAt(right, BiomeLabel(s_learnOrder[i + 1]), FeatureMethod.Direct, "$vt_recipe_biome_learn"))
                {
                    LearnBiome(s_learnOrder[i + 1]);
                }
                GUILayout.Space(4);
            }

            EnsureVendors();
            if (s_vendorOrder.Count > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label(VTLocalization.instance.Localize("$vt_recipe_vendor_title"), SectionTitleStyle());
                DrawVendorButtons(true);
            }

            Controls.EndSection();
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
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            s_learnSeq++;
            s_learnedSeq[key] = s_learnSeq;
            s_lastKnownCount = -1;
        }

        private static GUIStyle SectionTitleStyle()
        {
            return InterfaceMaker.CustomSkin != null ? InterfaceMaker.CustomSkin.FindStyle("sectionTitle") : GUI.skin.label;
        }

        // Learning through items. Every button below hands items to Player.AddKnownItem,
        // the same call Valheim makes when an item enters the inventory for the first time.
        // Valheim then runs its own discovery and unlocks whatever those items allow.

        private static void LearnBiome(RecipeBiome biome)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            Dictionary<string, ItemDrop> items = IndexItems();
            List<ItemDrop> drops = new List<ItemDrop>();
            foreach (string name in RecipeData.ItemsFor(biome))
            {
                ItemDrop drop;
                if (items.TryGetValue(name, out drop))
                {
                    drops.Add(drop);
                }
                else if (s_reportedMissing.Add(name))
                {
                    ZLog.LogWarning("[ValheimAdminTool] Biome item not found in this game version: " + name);
                }
            }

            if (biome == RecipeBiome.DeepNorth)
            {
                foreach (KeyValuePair<string, ItemDrop> pair in items)
                {
                    if (pair.Key.StartsWith("$") && RecipeData.IsBiomeMould(biome, pair.Key))
                    {
                        drops.Add(pair.Value);
                    }
                }
            }

            FinishIntroduce(player, IntroduceItems(player, drops));
        }

        private static void LearnVendor(string vendorId)
        {
            Player player = Player.m_localPlayer;
            List<ItemDrop> goods;
            if (player == null || !s_vendorGoods.TryGetValue(vendorId, out goods))
            {
                return;
            }

            FinishIntroduce(player, IntroduceItems(player, goods));
        }

        private static void DiscoverAllItems()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> materials = KnownMaterials(player);
            if (materials == null)
            {
                return;
            }

            int added = 0;
            foreach (ItemDrop drop in IndexItems().Values.Distinct())
            {
                if (IsUsable(drop) && materials.Add(drop.m_itemData.m_shared.m_name))
                {
                    added++;
                }
            }

            CheckDiscoveries(player);
            Refresh(player);
            Notify(player, added);
        }

        private static int IntroduceItems(Player player, IEnumerable<ItemDrop> drops)
        {
            HashSet<string> known = KnownMaterials(player);
            HashSet<string> done = new HashSet<string>(StringComparer.Ordinal);
            int added = 0;
            foreach (ItemDrop drop in drops)
            {
                if (!IsUsable(drop))
                {
                    continue;
                }

                string name = drop.m_itemData.m_shared.m_name;
                if (!done.Add(name) || (known != null && known.Contains(name)))
                {
                    continue;
                }

                try
                {
                    player.AddKnownItem(drop.m_itemData);
                    added++;
                }
                catch (Exception ex)
                {
                    ZLog.LogWarning("[ValheimAdminTool] Could not introduce " + name + ": " + ex.Message);
                }
            }

            return added;
        }

        private static void FinishIntroduce(Player player, int added)
        {
            CheckDiscoveries(player);
            Refresh(player);
            Notify(player, added);
        }

        private static void CheckDiscoveries(Player player)
        {
            if (s_updateKnownRecipes == null)
            {
                s_updateKnownRecipes = AccessTools.Method(typeof(Player), "UpdateKnownRecipesList");
            }

            try
            {
                s_updateKnownRecipes?.Invoke(player, null);
            }
            catch (Exception ex)
            {
                ZLog.LogWarning("[ValheimAdminTool] Recipe discovery check failed: " + ex.Message);
            }
        }

        private static bool IsUsable(ItemDrop drop)
        {
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                return false;
            }

            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            if (string.IsNullOrEmpty(shared.m_name) || shared.m_icons == null || shared.m_icons.Length == 0)
            {
                return false;
            }

            int variant = drop.m_itemData.m_variant;
            return variant >= 0 && variant < shared.m_icons.Length && shared.m_icons[variant] != null;
        }

        private static Dictionary<string, ItemDrop> IndexItems()
        {
            Dictionary<string, ItemDrop> map = new Dictionary<string, ItemDrop>(StringComparer.OrdinalIgnoreCase);
            if (ObjectDB.instance == null || ObjectDB.instance.m_items == null)
            {
                return map;
            }

            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (!IsUsable(drop))
                {
                    continue;
                }

                string name = drop.m_itemData.m_shared.m_name;
                if (!map.ContainsKey(name))
                {
                    map.Add(name, drop);
                }
            }

            return map;
        }

        // Direct actions. These are explicit overrides and use the game's own learn calls.

        private static void LearnAllRecipes()
        {
            EnsureCatalog();
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            int added = 0;
            for (int i = 0; i < s_catalog.Count; i++)
            {
                if (LearnEntry(player, s_catalog[i]))
                {
                    added++;
                }
            }

            Refresh(player);
            Notify(player, added);
        }

        private static void LearnSelected()
        {
            Player player = Player.m_localPlayer;
            if (player == null || s_selected.Count == 0)
            {
                return;
            }

            int added = 0;
            foreach (string key in s_selected.ToArray())
            {
                RecipeEntry entry;
                if (s_catalogByKey.TryGetValue(key, out entry) && LearnEntry(player, entry))
                {
                    added++;
                }
            }

            s_selected.Clear();
            Refresh(player);
            Notify(player, added);
        }

        private static bool LearnEntry(Player player, RecipeEntry entry)
        {
            if (player.IsRecipeKnown(entry.key))
            {
                return false;
            }

            try
            {
                if (entry.recipe != null)
                {
                    if (s_addKnownRecipe == null)
                    {
                        s_addKnownRecipe = AccessTools.Method(typeof(Player), "AddKnownRecipe", new[] { typeof(Recipe) });
                    }
                    s_addKnownRecipe.Invoke(player, new object[] { entry.recipe });
                }
                else if (entry.piece != null)
                {
                    if (s_addKnownPiece == null)
                    {
                        s_addKnownPiece = AccessTools.Method(typeof(Player), "AddKnownPiece", new[] { typeof(Piece) });
                    }
                    s_addKnownPiece.Invoke(player, new object[] { entry.piece });
                }
            }
            catch (Exception ex)
            {
                ZLog.LogWarning("[ValheimAdminTool] Could not learn " + entry.key + ": " + ex.Message);
                return false;
            }

            return player.IsRecipeKnown(entry.key);
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
            Refresh(player);
            Notify(player, count);
        }

        private static void ForgetRecipesAndItems()
        {
            Player player = Player.m_localPlayer;
            HashSet<string> recipes = KnownRecipes(player);
            HashSet<string> materials = KnownMaterials(player);
            if (recipes == null || materials == null)
            {
                return;
            }

            int count = recipes.Count;
            recipes.Clear();
            materials.Clear();
            s_selected.Clear();
            s_learnedSeq.Clear();
            s_lastKnownCount = -1;
            Refresh(player);
            Notify(player, count);
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
                s_learnedSeq.Remove(key);
            }

            s_selected.Clear();
            s_lastKnownCount = -1;
            Refresh(player);
            Notify(player, removed);
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
            Refresh(player);
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
            foreach (KeyValuePair<string, int> pair in CollectStations())
            {
                int current;
                if (!stations.TryGetValue(pair.Key, out current) || pair.Value > current)
                {
                    stations[pair.Key] = pair.Value;
                    added++;
                }
            }

            CheckDiscoveries(player);
            Refresh(player);
            Notify(player, added);
        }

        private static Dictionary<string, int> CollectStations()
        {
            Dictionary<string, int> levels = new Dictionary<string, int>(StringComparer.Ordinal);
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
                    NoteStation(levels, prefab != null ? prefab.GetComponent<CraftingStation>() : null, 1);
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

        // Vendors come from the Trader components in the game, so the lists always match what
        // each trader actually sells.

        private static void EnsureVendors()
        {
            if (s_vendorsReady || ZNetScene.instance == null || ZNetScene.instance.m_prefabs == null)
            {
                return;
            }

            s_vendorOrder.Clear();
            s_vendorLabel.Clear();
            s_vendorGoods.Clear();
            s_vendorByItem.Clear();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Trader trader = prefab != null ? prefab.GetComponent<Trader>() : null;
                if (trader == null || trader.m_items == null)
                {
                    continue;
                }

                string id = prefab.name;
                if (s_vendorGoods.ContainsKey(id))
                {
                    continue;
                }

                string raw = string.IsNullOrEmpty(trader.m_name) ? prefab.name : trader.m_name;
                string label = Localization.instance != null ? Localization.instance.Localize(raw) : raw;
                if (string.IsNullOrEmpty(label) || label.StartsWith("$") || (label.Length > 2 && label[0] == '[' && label[label.Length - 1] == ']'))
                {
                    label = prefab.name;
                }

                List<ItemDrop> goods = new List<ItemDrop>();
                for (int i = 0; i < trader.m_items.Count; i++)
                {
                    ItemDrop drop = trader.m_items[i] != null ? trader.m_items[i].m_prefab : null;
                    if (!IsUsable(drop))
                    {
                        continue;
                    }

                    goods.Add(drop);
                    string name = drop.m_itemData.m_shared.m_name;
                    if (!s_vendorByItem.ContainsKey(name))
                    {
                        s_vendorByItem.Add(name, id);
                    }
                }

                s_vendorLabel[id] = label;
                s_vendorGoods[id] = goods;
                s_vendorOrder.Add(id);
            }

            s_vendorOrder.Sort((a, b) => string.Compare(VendorLabel(a), VendorLabel(b), StringComparison.OrdinalIgnoreCase));
            s_vendorsReady = true;
            s_lastRecipeCount = -1;
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

        private static void DrawVendorButtons(bool learn)
        {
            const float rowHeight = 30f;
            const float gap = 8f;
            string tipCode = learn ? "$vt_recipe_vendor_learn" : "$vt_recipe_vendor_select";
            for (int i = 0; i < s_vendorOrder.Count; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                DrawVendorButton(left, s_vendorOrder[i], learn, tipCode);
                if (i + 1 < s_vendorOrder.Count)
                {
                    DrawVendorButton(right, s_vendorOrder[i + 1], learn, tipCode);
                }
                GUILayout.Space(4);
            }
        }

        private static void DrawVendorButton(Rect rect, string vendorId, bool learn, string tipCode)
        {
            if (learn)
            {
                if (Controls.ActionButtonAt(rect, VendorLabel(vendorId), FeatureMethod.Direct, tipCode))
                {
                    LearnVendor(vendorId);
                }
                return;
            }

            if (GUI.Button(rect, new GUIContent(VendorLabel(vendorId), Controls.Tip(tipCode))))
            {
                ToggleSelection(entry => entry.vendor == vendorId);
            }
        }

        // Recipe manager window.

        private static void DrawWindow(int windowID)
        {
            EntryPoint.HandleToggleHotkey();

            if (Event.current != null && new Rect(0f, 0f, s_windowRect.width, s_windowRect.height).Contains(Event.current.mousePosition))
            {
                Controls.ClearCoveredHover();
            }

            EnsureCatalog();

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
            string selectTip = Controls.Tip("$vt_recipe_biome_select");
            for (int i = 0; i < s_selectOrder.Length; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(1f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
                float columnWidth = Mathf.Max(0f, (row.width - gap) * 0.5f);
                Rect left = new Rect(row.x, row.y, columnWidth, row.height);
                Rect right = new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height);
                DrawSelectButton(left, s_selectOrder[i], selectTip);
                if (i + 1 < s_selectOrder.Length)
                {
                    DrawSelectButton(right, s_selectOrder[i + 1], selectTip);
                }
                GUILayout.Space(2);
            }

            EnsureVendors();
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
            else if (Controls.ActionButton("$vt_recipe_forget_selected", FeatureMethod.Direct) && s_selected.Count > 0)
            {
                Controls.AskConfirm("$vt_recipe_title", "$vt_recipe_forget_selected_confirm", ForgetSelected);
            }

            if (GUI.Button(new Rect(s_windowRect.width - 36, 6, 26, 26), "X", Controls.CloseButtonStyle()))
            {
                EntryPoint.s_showRecipeManager = false;
            }

            GUI.DragWindow(new Rect(0, 0, s_windowRect.width - 40, 32));
        }

        private static void DrawSelectButton(Rect rect, RecipeBiome biome, string tip)
        {
            if (GUI.Button(rect, new GUIContent(VTLocalization.instance.Localize(BiomeLabel(biome)), tip)))
            {
                ToggleSelection(entry => string.IsNullOrEmpty(entry.vendor) && entry.biome == biome);
            }
            if (Event.current != null && Event.current.type == EventType.Repaint && rect.Contains(Event.current.mousePosition))
            {
                Controls.SetHoverTooltip(tip);
            }
        }

        private static void ToggleSelection(Func<RecipeEntry, bool> match)
        {
            List<RecipeEntry> matches = s_visible.Where(match).ToList();
            if (matches.Count == 0)
            {
                return;
            }

            bool allSelected = matches.All(entry => s_selected.Contains(entry.key));
            foreach (RecipeEntry entry in matches)
            {
                if (allSelected)
                {
                    s_selected.Remove(entry.key);
                }
                else
                {
                    s_selected.Add(entry.key);
                }
            }
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
                Rect cellRect = new Rect((i % GridColumns) * cell, (i / GridColumns) * cell, cell, cell);
                if (!cellRect.Overlaps(visible))
                {
                    Controls.SkipToggle(cellRect);
                    continue;
                }

                bool on = s_selected.Contains(entry.key);
                s_cellContent.image = entry.icon;
                bool next = GUI.Toggle(cellRect, on, s_cellContent, cellStyle);
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
                    Controls.SetHoverTooltip(entry.displayName + "\n" + VTLocalization.instance.Localize(GroupLabel(entry)));
                }
            }

            GUI.EndScrollView();
        }

        private static void RebuildVisible()
        {
            HashSet<string> known = KnownRecipes(Player.m_localPlayer);
            int knownCount = known != null ? known.Count : 0;
            string search = s_searchTerms == null ? "" : s_searchTerms.Trim().ToLowerInvariant();
            if (s_previousSearch == search && s_previousTab == s_tabIdx && s_lastKnownCount == knownCount)
            {
                return;
            }

            List<RecipeEntry> source = new List<RecipeEntry>();
            for (int i = 0; i < s_catalog.Count; i++)
            {
                RecipeEntry entry = s_catalog[i];
                bool isKnown = known != null && known.Contains(entry.key);
                if (s_tabIdx == 0 && !isKnown)
                {
                    continue;
                }
                if (s_tabIdx == 1 && isKnown)
                {
                    continue;
                }
                if (s_tabIdx == 2 && (!isKnown || !s_learnedSeq.ContainsKey(entry.key)))
                {
                    continue;
                }

                if (search.Length > 0 && !entry.searchText.Contains(search))
                {
                    continue;
                }

                source.Add(entry);
            }

            source.Sort(s_tabIdx == 2 ? (Comparison<RecipeEntry>)CompareRecent : CompareByGroup);
            s_visible = source;
            s_selected.IntersectWith(source.Select(entry => entry.key));
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
            return seq != 0 ? seq : string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareByGroup(RecipeEntry a, RecipeEntry b)
        {
            bool aVendor = !string.IsNullOrEmpty(a.vendor);
            bool bVendor = !string.IsNullOrEmpty(b.vendor);
            if (aVendor != bVendor)
            {
                return aVendor ? 1 : -1;
            }

            int group = aVendor
                ? string.Compare(VendorLabel(a.vendor), VendorLabel(b.vendor), StringComparison.OrdinalIgnoreCase)
                : RecipeData.Rank(a.biome).CompareTo(RecipeData.Rank(b.biome));
            return group != 0 ? group : string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
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

        private static string GroupLabel(RecipeEntry entry)
        {
            return string.IsNullOrEmpty(entry.vendor) ? BiomeLabel(entry.biome) : VendorLabel(entry.vendor);
        }

        // Catalog. Every recipe in ObjectDB and every piece in a tool's build table. Each entry is
        // grouped by the latest biome among the ingredients Valheim checks when it discovers the
        // recipe, so the group matches the point in progression where the game would unlock it.

        private static void EnsureCatalog()
        {
            EnsureVendors();
            int recipeCount = ObjectDB.instance != null && ObjectDB.instance.m_recipes != null ? ObjectDB.instance.m_recipes.Count : 0;
            if (recipeCount == 0 || (s_catalog.Count > 0 && recipeCount == s_lastRecipeCount))
            {
                return;
            }

            s_catalog.Clear();
            s_catalogByKey.Clear();
            s_lastRecipeCount = recipeCount;
            s_lastKnownCount = -1;

            Dictionary<string, Recipe> recipeByProduct = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                string key = KeyFromRecipe(recipe);
                if (!string.IsNullOrEmpty(key) && !recipeByProduct.ContainsKey(key))
                {
                    recipeByProduct.Add(key, recipe);
                }
            }

            Dictionary<string, Group> groupCache = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Recipe> pair in recipeByProduct)
            {
                Recipe recipe = pair.Value;
                ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
                Group group = RequirementsGroup(recipe.m_resources, recipe.m_requireOnlyOneIngredient, recipeByProduct, groupCache, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { pair.Key });
                if (!string.IsNullOrEmpty(shared.m_dlc) || !recipe.m_enabled)
                {
                    group = Group.SpecialGroup;
                }

                AddEntry(new RecipeEntry
                {
                    key = pair.Key,
                    displayName = Localize(shared.m_name),
                    icon = TextureFromItem(recipe.m_item.m_itemData),
                    biome = group.biome,
                    vendor = group.vendor,
                    recipe = recipe
                });
            }

            HashSet<Piece> pieces = new HashSet<Piece>();
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
                PieceTable table = drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null ? drop.m_itemData.m_shared.m_buildPieces : null;
                if (table == null || table.m_pieces == null)
                {
                    continue;
                }

                foreach (GameObject prefab in table.m_pieces)
                {
                    Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                    if (piece == null || !pieces.Add(piece))
                    {
                        continue;
                    }

                    string key = KeyFromPiece(piece);
                    if (string.IsNullOrEmpty(key) || s_catalogByKey.ContainsKey(key))
                    {
                        continue;
                    }

                    Group group = RequirementsGroup(piece.m_resources, false, recipeByProduct, groupCache, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(piece.m_dlc) || !piece.m_enabled)
                    {
                        group = Group.SpecialGroup;
                    }

                    AddEntry(new RecipeEntry
                    {
                        key = key,
                        displayName = Localize(piece.m_name),
                        icon = TextureFromSprite(piece.m_icon),
                        biome = group.biome,
                        vendor = group.vendor,
                        piece = piece
                    });
                }
            }
        }

        private static void AddEntry(RecipeEntry entry)
        {
            entry.searchText = (entry.displayName + " " + entry.key).ToLowerInvariant();
            s_catalog.Add(entry);
            s_catalogByKey[entry.key] = entry;
        }

        private static Group RequirementsGroup(Piece.Requirement[] resources, bool anyOne, Dictionary<string, Recipe> recipeByProduct, Dictionary<string, Group> cache, HashSet<string> visiting)
        {
            if (resources == null || resources.Length == 0)
            {
                return Group.SpecialGroup;
            }

            List<Group> groups = new List<Group>();
            bool anyAmount = resources.Any(r => r != null && r.m_resItem != null && r.m_amount > 0);
            foreach (Piece.Requirement requirement in resources)
            {
                if (requirement == null || requirement.m_resItem == null || requirement.m_resItem.m_itemData == null || requirement.m_resItem.m_itemData.m_shared == null)
                {
                    continue;
                }
                if (anyAmount && requirement.m_amount <= 0)
                {
                    continue;
                }

                groups.Add(MaterialGroup(requirement.m_resItem.m_itemData.m_shared.m_name, recipeByProduct, cache, visiting));
            }

            if (groups.Count == 0)
            {
                return Group.SpecialGroup;
            }

            if (anyOne)
            {
                return groups.OrderBy(g => g.SortRank).First();
            }

            Group vendor = groups.FirstOrDefault(g => !string.IsNullOrEmpty(g.vendor));
            if (vendor.vendor != null)
            {
                return vendor;
            }

            return groups.OrderByDescending(g => g.SortRank).First();
        }

        private static Group MaterialGroup(string name, Dictionary<string, Recipe> recipeByProduct, Dictionary<string, Group> cache, HashSet<string> visiting)
        {
            Group cached;
            if (cache.TryGetValue(name, out cached))
            {
                return cached;
            }

            Group result;
            RecipeBiome biome;
            string vendor;
            Recipe producer;
            if (RecipeData.TryGetBiome(name, out biome))
            {
                result = new Group(biome, null);
            }
            else if (s_vendorByItem.TryGetValue(name, out vendor))
            {
                result = new Group(RecipeBiome.Special, vendor);
            }
            else if (recipeByProduct.TryGetValue(name, out producer) && visiting.Add(name))
            {
                result = RequirementsGroup(producer.m_resources, producer.m_requireOnlyOneIngredient, recipeByProduct, cache, visiting);
                visiting.Remove(name);
            }
            else
            {
                result = Group.SpecialGroup;
            }

            cache[name] = result;
            return result;
        }

        private struct Group
        {
            public static readonly Group SpecialGroup = new Group(RecipeBiome.Special, null);

            public readonly RecipeBiome biome;
            public readonly string vendor;

            public Group(RecipeBiome biome, string vendor)
            {
                this.biome = biome;
                this.vendor = vendor;
            }

            public int SortRank => string.IsNullOrEmpty(vendor) ? RecipeData.Rank(biome) : 500;
        }

        private class RecipeEntry
        {
            public string key;
            public string displayName;
            public string searchText;
            public Texture icon;
            public RecipeBiome biome;
            public string vendor;
            public Recipe recipe;
            public Piece piece;
        }

        // Helpers.

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

        private static void Refresh(Player player)
        {
            if (player != null)
            {
                player.CallMethod("UpdateAvailablePiecesList");
            }

            s_lastKnownCount = -1;
            RefreshCraftingPanel();
        }

        private static void RefreshCraftingPanel()
        {
            if (InventoryGui.instance == null || !InventoryGui.IsVisible())
            {
                return;
            }

            MethodInfo method = AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel");
            if (method == null)
            {
                return;
            }

            try
            {
                object[] args = method.GetParameters().Length == 1 ? new object[] { false } : null;
                method.Invoke(InventoryGui.instance, args);
            }
            catch
            {
            }
        }

        private static void Notify(Player player, int count)
        {
            if (player != null)
            {
                player.VTSendMessage(VTLocalization.instance.Localize("$vt_recipe_updated") + " " + count);
            }
        }

        private static string Localize(string raw)
        {
            return Localization.instance != null ? Localization.instance.Localize(raw) : raw;
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
            if (item == null || item.m_shared == null || item.m_shared.m_icons == null || item.m_shared.m_icons.Length == 0)
            {
                return GetPlaceholderIcon();
            }

            int variant = Mathf.Clamp(item.m_variant, 0, item.m_shared.m_icons.Length - 1);
            return TextureFromSprite(item.m_shared.m_icons[variant]);
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
    }
}
