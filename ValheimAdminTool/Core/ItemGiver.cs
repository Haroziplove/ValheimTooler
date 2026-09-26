using System;
using System.Collections.Generic;
using System.Linq;
using RapidGUI;
using UnityEngine;
using ValheimAdminTool.Core.Extensions;
using ValheimAdminTool.UI;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.Core
{
    public static class ItemGiver
    {
        private static Rect s_itemGiverRect;
        private static Vector2 s_itemGiverScrollPosition;
        private static readonly List<InventoryItem> s_items = new List<InventoryItem>();
        private static List<InventoryItem> s_itemsFiltered = new List<InventoryItem>();
        private static List<GUIContent> s_itemsGUIFiltered = new List<GUIContent>();

        private static int s_selectedItem = 0;
        private static string s_quantityItem = "1";
        private static int s_qualityIdx = 0;
        private static string s_searchTerms = "";
        private static string s_previousSearchTerms = "";
        private static int s_categoryIdx = 0;
        private static int s_previousCategoryIdx = -1;
        private static Texture2D s_placeholderIcon;
        private static FeatureMethod s_giveMethod = FeatureMethod.Direct;
        private static bool s_setCrafter = true;
        private static string s_crafterName = "";
        private static bool s_crafterFilled;
        private static string s_crafterItemKey = "";
        private static readonly Dictionary<string, bool> s_crafterOverrides = new Dictionary<string, bool>();

        private static readonly string[] s_categoryKeys =
        {
            "$vt_item_giver_cat_all",
            "$vt_item_giver_cat_weapons",
            "$vt_item_giver_cat_armor",
            "$vt_item_giver_cat_tools",
            "$vt_item_giver_cat_trinkets",
            "$vt_item_giver_cat_food",
            "$vt_item_giver_cat_materials",
            "$vt_item_giver_cat_ammo",
            "$vt_item_giver_cat_trophies",
            "$vt_item_giver_cat_misc"
        };

        public static void Start()
        {
            s_itemGiverRect = new Rect(ConfigManager.s_itemGiverWindowPosition.Value.x, ConfigManager.s_itemGiverWindowPosition.Value.y, 460, 520);
            EnsureItems();
        }

        private static void EnsureItems()
        {
            if (s_items.Count > 0 || ObjectDB.instance == null || ObjectDB.instance.m_items == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            Dictionary<string, int> recipeScores = BuildRecipeScores();

            foreach (GameObject gameObject in ObjectDB.instance.m_items)
            {
                if (gameObject == null)
                {
                    continue;
                }

                ItemDrop component = gameObject.GetComponent<ItemDrop>();
                if (component?.m_itemData?.m_shared == null)
                {
                    continue;
                }

                Sprite[] icons = component.m_itemData.m_shared.m_icons;
                int variantCount = icons != null && icons.Length > 0 ? icons.Length : 1;

                for (var variant = 0; variant < variantCount; variant++)
                {
                    string displayName = Localization.instance.Localize(component.m_itemData.m_shared.m_name + (variant > 0 ? " Variant " + variant.ToString() : ""));
                    Texture texture = GetItemTexture(icons, variant);
                    bool hasIcon = texture != null && texture != GetPlaceholderIcon();
                    var content = new GUIContent(texture, displayName);
                    int progression = ProgressionScore(gameObject, component, displayName, recipeScores);
                    s_items.Add(new InventoryItem(gameObject, component, content, variant, hasIcon, progression));
                }
            }

            s_items.Sort(CompareItems);
            s_itemsFiltered = new List<InventoryItem>(s_items);
            s_itemsGUIFiltered = s_items.Select(i => i.guiContent).ToList();
            s_previousCategoryIdx = -1;
            s_previousSearchTerms = "";
        }

        private static Texture GetItemTexture(Sprite[] icons, int variant)
        {
            if (icons == null || variant < 0 || variant >= icons.Length)
            {
                return GetPlaceholderIcon();
            }

            try
            {
                Texture texture = SpriteManager.TextureFromSprite(icons[variant]);
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

        public static void Update()
        {
            return;
        }

        public static Rect WindowRect => s_itemGiverRect;

        public static void DisplayGUI()
        {
            s_itemGiverRect = GUILayout.Window(1002, s_itemGiverRect, ItemGiverWindow, VTLocalization.instance.Localize("$vt_item_giver_title"), GUILayout.MinWidth(460));

            ConfigManager.s_itemGiverWindowPosition.Value = s_itemGiverRect.position;
        }

        public static void ItemGiverWindow(int windowID)
        {
            EntryPoint.HandleToggleHotkey();

            if (Event.current != null && new Rect(0f, 0f, s_itemGiverRect.width, s_itemGiverRect.height).Contains(Event.current.mousePosition))
            {
                Controls.ClearCoveredHover();
            }

            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                return;

            EnsureItems();

            if (!s_crafterFilled && Player.m_localPlayer != null)
            {
                s_crafterName = Player.m_localPlayer.GetPlayerName();
                s_crafterFilled = true;
            }

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;

            GUILayout.Space(6);
            string[] categories = s_categoryKeys.Select(key => VTLocalization.instance.Localize(key)).ToArray();
            s_categoryIdx = GUILayout.SelectionGrid(s_categoryIdx, categories, 5, InterfaceMaker.CustomSkin.GetStyle("toolbar"));

            s_searchTerms = GUILayout.TextField(s_searchTerms, GUILayout.MinHeight(26));
            FilterItems();

            if (s_itemsGUIFiltered.Count > 0)
            {
                if (s_selectedItem < 0 || s_selectedItem >= s_itemsGUIFiltered.Count)
                {
                    s_selectedItem = 0;
                }

                DrawItemGrid();
                ApplyCraftedByForSelection();
            }
            else
            {
                s_selectedItem = 0;
                s_crafterItemKey = "";
                GUILayout.Label(VTLocalization.instance.Localize("$vt_item_giver_empty"), GUILayout.Height(280));
            }

            GUILayout.BeginHorizontal();
            {
                Controls.FieldLabel("$vt_item_giver_quantity");
                s_quantityItem = GUILayout.TextField(s_quantityItem, GUILayout.ExpandWidth(true));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                Controls.FieldLabel("$vt_item_giver_quality");
                string[] qualities = BuildQualityOptions();
                if (s_qualityIdx < 0 || s_qualityIdx >= qualities.Length)
                {
                    s_qualityIdx = 0;
                }
                s_qualityIdx = RGUI.SelectionPopup(s_qualityIdx, qualities);
            }
            GUILayout.EndHorizontal();

            bool nextCrafter = Controls.LabeledToggle("$vt_item_giver_crafted_by_enable", s_setCrafter);
            if (nextCrafter != s_setCrafter)
            {
                s_setCrafter = nextCrafter;
                string key = CurrentItemKey();
                if (!string.IsNullOrEmpty(key))
                {
                    s_crafterOverrides[key] = nextCrafter;
                }
            }
            if (s_setCrafter)
            {
                GUILayout.BeginHorizontal();
                {
                    Controls.FieldLabel("$vt_item_giver_crafted_by");
                    s_crafterName = GUILayout.TextField(s_crafterName, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();
            }

            s_giveMethod = Controls.MethodPicker(s_giveMethod, "$vt_item_giver_method");

            if (Controls.ActionButtonNarrow("$vt_item_giver_button", s_giveMethod, 180f))
            {
                if (s_selectedItem >= 0 && s_selectedItem < s_itemsFiltered.Count && int.TryParse(s_quantityItem, out int quantity))
                {
                    int quality = s_qualityIdx + 1;
                    bool cheated = s_giveMethod == FeatureMethod.DevCommands;
                    Player.m_localPlayer.VTAddItemToInventory(
                        s_itemsFiltered[s_selectedItem].itemPrefab.name,
                        quantity,
                        quality,
                        s_itemsFiltered[s_selectedItem].variant,
                        cheated,
                        s_crafterName,
                        s_setCrafter);
                }
            }

            GUIStyle closeStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0)
            };
            if (GUI.Button(new Rect(s_itemGiverRect.width - 36, 6, 26, 26), "X", closeStyle))
            {
                EntryPoint.s_showItemGiver = false;
            }

            GUI.DragWindow(new Rect(0, 0, s_itemGiverRect.width - 40, 32));
        }

        private static void DrawItemGrid()
        {
            const int columns = 4;
            const float viewHeight = 280f;
            int count = s_itemsGUIFiltered.Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));

            Rect view = GUILayoutUtility.GetRect(1f, viewHeight, GUILayout.ExpandWidth(true), GUILayout.Height(viewHeight));
            float cell = view.width > 32f ? Mathf.Floor((view.width - 16f) / columns) : 80f;
            Rect content = new Rect(0f, 0f, columns * cell, rows * cell);
            s_itemGiverScrollPosition = GUI.BeginScrollView(view, s_itemGiverScrollPosition, content, false, true);

            GUIStyle cellStyle = InterfaceMaker.CustomSkin.GetStyle("itemCell");
            Rect visible = new Rect(s_itemGiverScrollPosition.x, s_itemGiverScrollPosition.y, view.width, view.height);
            Vector2 mouse = Event.current.mousePosition;

            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect cellRect = new Rect(col * cell, row * cell, cell, cell);
                GUIContent contentItem = s_itemsGUIFiltered[i];
                if (GUI.Toggle(cellRect, s_selectedItem == i, new GUIContent(contentItem.image), cellStyle))
                {
                    s_selectedItem = i;
                }

                if (Event.current.type == EventType.Repaint && cellRect.Contains(mouse) && visible.Contains(mouse))
                {
                    string name = contentItem.tooltip;
                    if (string.IsNullOrEmpty(name) && i < s_itemsFiltered.Count && s_itemsFiltered[i].itemDrop?.m_itemData?.m_shared != null)
                    {
                        name = Localization.instance.Localize(s_itemsFiltered[i].itemDrop.m_itemData.m_shared.m_name);
                    }
                    Controls.SetHoverTooltip(name);
                }
            }

            GUI.EndScrollView();
        }

        private static string[] BuildQualityOptions()
        {
            int maxQuality = 1;
            if (s_selectedItem >= 0 && s_selectedItem < s_itemsFiltered.Count)
            {
                ItemDrop.ItemData.SharedData shared = s_itemsFiltered[s_selectedItem].itemDrop.m_itemData.m_shared;
                if (shared != null)
                {
                    maxQuality = Mathf.Max(1, shared.m_maxQuality);
                }
            }

            string[] qualities = new string[maxQuality];
            for (int i = 0; i < maxQuality; i++)
            {
                qualities[i] = (i + 1).ToString();
            }
            return qualities;
        }

        private static void FilterItems()
        {
            string search = s_searchTerms == null ? "" : s_searchTerms.ToLower();
            if (s_previousSearchTerms.Equals(search) && s_previousCategoryIdx == s_categoryIdx)
            {
                return;
            }

            IEnumerable<InventoryItem> filtered = s_items.Where(item => MatchesCategory(item, s_categoryIdx));
            if (search.Length > 0)
            {
                filtered = filtered.Where(i => Localization.instance.Localize(i.itemDrop.m_itemData.m_shared.m_name + (i.variant > 0 ? " Variant " + i.variant.ToString() : "")).ToLower().Contains(search));
            }

            s_itemsFiltered = filtered.ToList();
            s_itemsGUIFiltered = s_itemsFiltered.Select(i => i.guiContent).ToList();
            if (s_selectedItem >= s_itemsFiltered.Count)
            {
                s_selectedItem = 0;
            }

            s_previousSearchTerms = search;
            s_previousCategoryIdx = s_categoryIdx;
        }

        private static bool MatchesCategory(InventoryItem item, int category)
        {
            if (category <= 0 || item?.itemDrop?.m_itemData?.m_shared == null)
            {
                return true;
            }

            ItemDrop.ItemData.ItemType type = item.itemDrop.m_itemData.m_shared.m_itemType;
            switch (category)
            {
                case 1:
                    return IsWeaponItem(item) && !IsToolItem(item);
                case 2:
                    return type == ItemDrop.ItemData.ItemType.Helmet
                        || type == ItemDrop.ItemData.ItemType.Chest
                        || type == ItemDrop.ItemData.ItemType.Legs
                        || type == ItemDrop.ItemData.ItemType.Hands
                        || type == ItemDrop.ItemData.ItemType.Shoulder
                        || type == ItemDrop.ItemData.ItemType.Utility;
                case 3:
                    return IsToolItem(item);
                case 4:
                    return type == ItemDrop.ItemData.ItemType.Trinket;
                case 5:
                    return type == ItemDrop.ItemData.ItemType.Consumable
                        || type == ItemDrop.ItemData.ItemType.Fish;
                case 6:
                    return type == ItemDrop.ItemData.ItemType.Material;
                case 7:
                    return type == ItemDrop.ItemData.ItemType.Ammo
                        || type == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
                case 8:
                    return type == ItemDrop.ItemData.ItemType.Trophy;
                case 9:
                    return type == ItemDrop.ItemData.ItemType.Misc
                        || type == ItemDrop.ItemData.ItemType.Customization
                        || type == ItemDrop.ItemData.ItemType.None;
                default:
                    return true;
            }
        }

        private static bool IsWeaponItem(InventoryItem item)
        {
            ItemDrop.ItemData.ItemType type = item.itemDrop.m_itemData.m_shared.m_itemType;
            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                || type == ItemDrop.ItemData.ItemType.Bow
                || type == ItemDrop.ItemData.ItemType.Shield
                || type == ItemDrop.ItemData.ItemType.Attach_Atgeir;
        }

        private static bool IsToolItem(InventoryItem item)
        {
            ItemDrop.ItemData.ItemType type = item.itemDrop.m_itemData.m_shared.m_itemType;
            if (type == ItemDrop.ItemData.ItemType.Tool || type == ItemDrop.ItemData.ItemType.Torch)
            {
                return true;
            }

            string id = ((item.itemPrefab != null ? item.itemPrefab.name : "") + " " + item.itemDrop.m_itemData.m_shared.m_name).ToLowerInvariant();
            if (id.Contains("pickaxe") || id.Contains("pick_axe"))
            {
                return true;
            }
            if (id.Contains("scythe"))
            {
                return true;
            }
            if (id.Contains("shovel"))
            {
                return true;
            }
            if (id.Contains("cultivator"))
            {
                return true;
            }
            if (id.Contains("fishingrod") || id.Contains("fishing_rod"))
            {
                return true;
            }
            if (id.Contains("hoe") && !id.Contains("shoe"))
            {
                return true;
            }
            if (id.Contains("hammer") && !id.Contains("warhammer") && !id.Contains("sledge") && !id.Contains("battle"))
            {
                return true;
            }

            return false;
        }

        private static int CompareItems(InventoryItem a, InventoryItem b)
        {
            int icon = (a.hasIcon ? 0 : 1).CompareTo(b.hasIcon ? 0 : 1);
            if (icon != 0)
            {
                return icon;
            }

            int progression = a.progression.CompareTo(b.progression);
            if (progression != 0)
            {
                return progression;
            }

            string nameA = a.guiContent != null ? a.guiContent.tooltip : "";
            string nameB = b.guiContent != null ? b.guiContent.tooltip : "";
            return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, int> BuildRecipeScores()
        {
            var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null)
            {
                return scores;
            }

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe == null || recipe.m_item == null)
                {
                    continue;
                }

                int score = StationScore(recipe);
                string key = recipe.m_item.name;
                if (!scores.ContainsKey(key) || score > scores[key])
                {
                    scores[key] = score;
                }
            }

            return scores;
        }

        private static int StationScore(Recipe recipe)
        {
            int level = recipe.m_minStationLevel;
            string station = recipe.m_craftingStation != null ? recipe.m_craftingStation.name.ToLowerInvariant() : "";
            if (string.IsNullOrEmpty(station))
            {
                return 6 + level;
            }
            if (station.Contains("blackforge") || station.Contains("galdr") || station.Contains("gemcutter"))
            {
                return 82 + level * 3;
            }
            if (station.Contains("mage") || station.Contains("eitr"))
            {
                return 76 + level * 3;
            }
            if (station.Contains("artisan"))
            {
                return 72 + level;
            }
            if (station.Contains("cauldron"))
            {
                return 16 + level * 12;
            }
            if (station.Contains("forge"))
            {
                return 32 + level * 10;
            }
            if (station.Contains("workbench"))
            {
                return 8 + level * 6;
            }
            if (station.Contains("stonecutter"))
            {
                return 36 + level;
            }

            return 22 + level * 5;
        }

        private static int ProgressionScore(GameObject prefab, ItemDrop itemDrop, string displayName, Dictionary<string, int> recipeScores)
        {
            ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
            string prefabName = prefab != null ? prefab.name : "";
            int score = KeywordProgression(prefabName + " " + shared.m_name + " " + displayName);
            if (recipeScores != null && recipeScores.TryGetValue(prefabName, out int recipeScore))
            {
                score = Mathf.Max(score, recipeScore);
            }

            score += shared.m_toolTier * 5;
            score += Mathf.RoundToInt(shared.m_armor / 10f);
            return score;
        }

        private static int KeywordProgression(string text)
        {
            string id = text != null ? text.ToLowerInvariant() : "";
            if (ContainsAny(id, "flametal", "asksvin", "charred", "ashlands", "slayer", "berserkir"))
            {
                return 90;
            }
            if (ContainsAny(id, "eitr", "carapace", "mistlands", "ygg", "dvergr", "seeker", "gjall", "mage", "feather"))
            {
                return 80;
            }
            if (ContainsAny(id, "blackmetal", "padded", "lox", "tar", "goblin", "plains", "needle", "linen"))
            {
                return 70;
            }
            if (ContainsAny(id, "silver", "wolf", "frost", "mountain", "crystal", "fenring", "drake"))
            {
                return 60;
            }
            if (ContainsAny(id, "iron", "root", "ancient", "swamp", "poison", "crypt", "abomination"))
            {
                return 50;
            }
            if (ContainsAny(id, "bronze", "troll", "finewood", "copper", "tin", "bronzeage"))
            {
                return 40;
            }
            if (ContainsAny(id, "flint", "leather", "deer", "boar", "neck", "bone", "antler", "crude"))
            {
                return 20;
            }
            if (ContainsAny(id, "wood", "stone", "club", "torch", "hammer", "hoe", "axe", "pickaxe", "meadow"))
            {
                return 10;
            }

            return 28;
        }

        private static bool ContainsAny(string text, params string[] parts)
        {
            foreach (string part in parts)
            {
                if (text.Contains(part))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyCraftedByForSelection()
        {
            string key = CurrentItemKey();
            if (string.IsNullOrEmpty(key) || key == s_crafterItemKey)
            {
                return;
            }

            s_crafterItemKey = key;
            if (s_crafterOverrides.TryGetValue(key, out bool forced))
            {
                s_setCrafter = forced;
                return;
            }

            s_setCrafter = IsCraftable(s_itemsFiltered[s_selectedItem]);
        }

        private static string CurrentItemKey()
        {
            if (s_selectedItem < 0 || s_selectedItem >= s_itemsFiltered.Count)
            {
                return "";
            }

            InventoryItem item = s_itemsFiltered[s_selectedItem];
            if (item?.itemPrefab == null)
            {
                return "";
            }

            return item.itemPrefab.name + ":" + item.variant;
        }

        private static bool IsCraftable(InventoryItem item)
        {
            if (ObjectDB.instance == null || item?.itemDrop?.m_itemData == null)
            {
                return false;
            }

            Recipe recipe = ObjectDB.instance.GetRecipe(item.itemDrop.m_itemData);
            return recipe != null && recipe.m_enabled;
        }

        class InventoryItem
        {
            public GameObject itemPrefab;
            public ItemDrop itemDrop;
            public GUIContent guiContent;
            public int variant;
            public bool hasIcon;
            public int progression;

            public InventoryItem(GameObject itemPrefab, ItemDrop itemDrop, GUIContent guiContent, int variant, bool hasIcon, int progression)
            {
                this.itemPrefab = itemPrefab;
                this.itemDrop = itemDrop;
                this.guiContent = guiContent;
                this.variant = variant;
                this.hasIcon = hasIcon;
                this.progression = progression;
            }
        }
    }
}
