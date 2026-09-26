using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.Core
{
    public static class ComfortTable
    {
        public static bool Visible;

        private static Rect s_rect = new Rect(40f, 80f, 420f, 520f);
        private static Vector2 s_scroll;
        private static bool s_dragging;
        private static Vector2 s_dragOffset;
        private static bool s_catalogReady;
        private static readonly List<Row> s_rows = new List<Row>();
        private static readonly HashSet<string> s_ownedItems = new HashSet<string>();
        private static readonly HashSet<int> s_ownedGroups = new HashSet<int>();
        private static GUIStyle s_header;
        private static GUIStyle s_category;
        private static GUIStyle s_cell;
        private static GUIStyle s_value;
        private static GUIStyle s_owned;
        private static GUIStyle s_ownedValue;
        private static Texture2D s_ownedTex;
        private static Texture2D s_lineTex;
        private static float s_viewHeight = 440f;
        private static float s_maxScroll;
        private static float s_ownedScan;

        public static Rect ScreenRect
        {
            get
            {
                float scale = Scale;
                return new Rect(s_rect.x * scale, s_rect.y * scale, s_rect.width * scale, s_rect.height * scale);
            }
        }

        public static float Scale
        {
            get { return ConfigManager.s_comfortTableScale != null ? Mathf.Clamp(ConfigManager.s_comfortTableScale.Value, 0.6f, 1.8f) : 1f; }
        }

        public static void AdjustScale(float delta)
        {
            if (ConfigManager.s_comfortTableScale == null)
            {
                return;
            }

            ConfigManager.s_comfortTableScale.Value = Mathf.Clamp(ConfigManager.s_comfortTableScale.Value + delta, 0.6f, 1.8f);
        }

        public static void ScrollWhenHidden()
        {
            if (!Visible || EntryPoint.s_showMainWindow)
            {
                return;
            }

            float page = Mathf.Max(48f, s_viewHeight - 24f);
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                s_scroll.y = Mathf.Min(s_maxScroll, s_scroll.y + page);
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                s_scroll.y = Mathf.Max(0f, s_scroll.y - page);
            }
        }

        public static void Toggle()
        {
            Visible = !Visible;
        }

        public static void Draw()
        {
            if (!Visible)
            {
                return;
            }

            Event ev = Event.current;
            bool toolVisible = EntryPoint.s_showMainWindow;
            if (!toolVisible && ev != null && ev.type != EventType.Repaint && ev.type != EventType.Layout)
            {
                return;
            }

            EnsureCatalog();
            RefreshOwned();
            EnsureStyles();

            Matrix4x4 previous = GUI.matrix;
            float scale = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            if (toolVisible)
            {
                HandleDrag(ev);
            }

            GUI.Box(s_rect, GUIContent.none);
            float titleWidth = toolVisible ? s_rect.width - 78f : s_rect.width - 20f;
            GUI.Label(new Rect(s_rect.x + 10f, s_rect.y + 6f, titleWidth, 24f), VTLocalization.instance.Localize("$vt_comfort_table_title"), s_header);
            if (toolVisible)
            {
                DrawScaleButtons();
            }

            Rect view = new Rect(s_rect.x + 8f, s_rect.y + 36f, s_rect.width - 16f, s_rect.height - 46f);
            s_viewHeight = view.height;
            const float rowHeight = 24f;
            const float valueWidth = 52f;
            float contentHeight = s_rows.Count * rowHeight;
            s_maxScroll = Mathf.Max(0f, contentHeight - view.height);
            s_scroll.y = Mathf.Clamp(s_scroll.y, 0f, s_maxScroll);
            s_scroll = GUI.BeginScrollView(view, s_scroll, new Rect(0f, 0f, view.width - 18f, contentHeight));
            float lineWidth = view.width - 18f;
            for (int i = 0; i < s_rows.Count; i++)
            {
                Row row = s_rows[i];
                Rect line = new Rect(0f, i * rowHeight, lineWidth, rowHeight);
                bool owned = row.header ? s_ownedGroups.Contains(row.group) : s_ownedItems.Contains(row.match);
                if (owned)
                {
                    GUI.DrawTexture(line, s_ownedTex, ScaleMode.StretchToFill);
                }

                if (row.header)
                {
                    GUI.Label(new Rect(line.x + 6f, line.y, line.width - 12f, line.height), row.text, owned ? s_owned : s_category);
                }
                else
                {
                    GUI.Label(new Rect(line.x + 28f, line.y, line.width - valueWidth - 36f, line.height), row.text, owned ? s_owned : s_cell);
                    GUI.Label(new Rect(line.x + line.width - valueWidth - 8f, line.y, valueWidth, line.height), "+" + row.comfort, owned ? s_ownedValue : s_value);
                    GUI.DrawTexture(new Rect(line.x + line.width - valueWidth - 10f, line.y + 3f, 1f, line.height - 6f), s_lineTex);
                }

                GUI.DrawTexture(new Rect(line.x, line.yMax - 1f, line.width, 1f), s_lineTex);
            }

            GUI.EndScrollView();
            GUI.matrix = previous;

            if (ConfigManager.s_comfortTablePosition != null)
            {
                ConfigManager.s_comfortTablePosition.Value = s_rect.position;
            }
        }

        private static void HandleDrag(Event ev)
        {
            if (ev == null)
            {
                return;
            }

            Rect bar = new Rect(s_rect.x, s_rect.y, Mathf.Max(40f, s_rect.width - 70f), 32f);
            Vector2 mouse = ev.mousePosition;
            if (ev.type == EventType.MouseDown && ev.button == 0 && bar.Contains(mouse))
            {
                s_dragging = true;
                s_dragOffset = mouse - s_rect.position;
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp)
            {
                s_dragging = false;
            }
            else if (s_dragging && (ev.type == EventType.MouseDrag || ev.type == EventType.MouseUp))
            {
                s_rect.position = mouse - s_dragOffset;
                ev.Use();
            }
        }

        private static void EnsureCatalog()
        {
            if (s_catalogReady || ZNetScene.instance == null || ZNetScene.instance.m_prefabs == null)
            {
                return;
            }

            var seen = new HashSet<string>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                Piece piece = prefab.GetComponent<Piece>();
                if (piece == null || piece.m_comfort <= 0 || string.IsNullOrEmpty(piece.m_name) || !GivesComfort(prefab, piece))
                {
                    continue;
                }

                if (!seen.Add(piece.m_name))
                {
                    continue;
                }

                string display = PieceName(piece);
                s_rows.Add(new Row
                {
                    header = false,
                    group = (int)piece.m_comfortGroup,
                    key = piece.m_name,
                    match = MatchKey(piece.m_name),
                    comfort = piece.m_comfort,
                    text = display
                });
            }

            s_rows.Sort(CompareRows);
            InsertHeaders();
            s_catalogReady = s_rows.Count > 0;
            if (ConfigManager.s_comfortTablePosition != null)
            {
                s_rect.position = ConfigManager.s_comfortTablePosition.Value;
            }
        }

        private static bool GivesComfort(GameObject prefab, Piece piece)
        {
            if (piece.m_category == Piece.PieceCategory.Food
                || piece.m_category == Piece.PieceCategory.Meads
                || piece.m_category == Piece.PieceCategory.Feasts)
            {
                return false;
            }

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null)
            {
                ItemDrop.ItemData.ItemType itemType = drop.m_itemData.m_shared.m_itemType;
                if (itemType == ItemDrop.ItemData.ItemType.Consumable)
                {
                    return false;
                }
            }

            return true;
        }

        private static string MatchKey(string name)
        {
            string stem = name ?? "";
            if (stem.StartsWith("$"))
            {
                stem = stem.Substring(1);
            }

            stem = TrimVariant(stem).ToLowerInvariant();
            if (stem.StartsWith("piece_"))
            {
                stem = stem.Substring(6);
            }

            return stem;
        }

        private static string PieceName(Piece piece)
        {
            string raw = piece.m_name ?? "";
            string stem = raw.StartsWith("$") ? raw.Substring(1) : raw;
            string[] candidates = new string[]
            {
                raw,
                "$" + stem,
                "$" + TrimVariant(stem),
                "$piece_" + TrimVariant(stem)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                if (!candidate.StartsWith("$"))
                {
                    candidate = "$" + candidate;
                }

                string localized = Localization.instance != null ? Localization.instance.Localize(candidate) : candidate;
                if (!IsMissingName(localized))
                {
                    return localized;
                }
            }

            return PrettyToken(TrimVariant(stem));
        }

        private static string TrimVariant(string stem)
        {
            string[] suffixes = new string[] { "_male", "_female", "_horizontal", "_vertical" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (stem.EndsWith(suffixes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return stem.Substring(0, stem.Length - suffixes[i].Length);
                }
            }

            return stem;
        }

        private static bool IsMissingName(string value)
        {
            return string.IsNullOrEmpty(value) || (value.StartsWith("[") && value.EndsWith("]"));
        }

        private static string PrettyToken(string stem)
        {
            if (stem.StartsWith("piece_"))
            {
                stem = stem.Substring(6);
            }

            string[] parts = stem.Split('_');
            var words = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                {
                    continue;
                }

                if (words.Length > 0)
                {
                    words.Append(' ');
                }

                words.Append(char.ToUpperInvariant(parts[i][0]));
                if (parts[i].Length > 1)
                {
                    words.Append(parts[i].Substring(1));
                }
            }

            return words.Length > 0 ? words.ToString() : stem;
        }

        private static void InsertHeaders()
        {
            var withHeaders = new List<Row>();
            int lastGroup = int.MinValue;
            for (int i = 0; i < s_rows.Count; i++)
            {
                Row row = s_rows[i];
                if (row.group != lastGroup)
                {
                    withHeaders.Add(new Row
                    {
                        header = true,
                        group = row.group,
                        key = "group:" + row.group,
                        text = GroupName(row.group).ToUpperInvariant()
                    });
                    lastGroup = row.group;
                }

                withHeaders.Add(row);
            }

            s_rows.Clear();
            s_rows.AddRange(withHeaders);
        }

        private static int CompareRows(Row a, Row b)
        {
            int group = a.group.CompareTo(b.group);
            if (group != 0)
            {
                return group;
            }

            int comfort = b.comfort.CompareTo(a.comfort);
            if (comfort != 0)
            {
                return comfort;
            }

            return string.Compare(a.text, b.text, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void RefreshOwned()
        {
            if (Time.unscaledTime < s_ownedScan)
            {
                return;
            }

            s_ownedScan = Time.unscaledTime + 0.5f;
            s_ownedItems.Clear();
            s_ownedGroups.Clear();
            if (Player.m_localPlayer == null)
            {
                return;
            }

            FieldInfo field = typeof(Piece).GetField("s_allComfortPieces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            HashSet<Piece> pieces = field != null ? field.GetValue(null) as HashSet<Piece> : null;
            if (pieces == null)
            {
                return;
            }

            Vector3 origin = Player.m_localPlayer.transform.position;
            foreach (Piece piece in pieces)
            {
                if (piece == null || piece.GetComfort() <= 0)
                {
                    continue;
                }

                if (Vector3.Distance(origin, piece.transform.position) >= 10f)
                {
                    continue;
                }

                s_ownedGroups.Add((int)piece.m_comfortGroup);
                s_ownedItems.Add(MatchKey(piece.m_name));
                string prefab = piece.gameObject.name.Replace("(Clone)", "").Trim();
                s_ownedItems.Add(MatchKey(prefab));
            }
        }

        private static string GroupName(int group)
        {
            switch (group)
            {
                case 1: return VTLocalization.instance.Localize("$vt_comfort_group_fire");
                case 2: return VTLocalization.instance.Localize("$vt_comfort_group_bed");
                case 3: return VTLocalization.instance.Localize("$vt_comfort_group_banner");
                case 4: return VTLocalization.instance.Localize("$vt_comfort_group_chair");
                case 5: return VTLocalization.instance.Localize("$vt_comfort_group_table");
                case 6: return VTLocalization.instance.Localize("$vt_comfort_group_rug");
                case 7: return VTLocalization.instance.Localize("$vt_comfort_group_stands");
                case 8: return VTLocalization.instance.Localize("$vt_comfort_group_decor");
                case 9: return VTLocalization.instance.Localize("$vt_comfort_group_plants");
                case 10: return VTLocalization.instance.Localize("$vt_comfort_group_lights");
                case 11: return VTLocalization.instance.Localize("$vt_comfort_group_bathroom");
                default: return VTLocalization.instance.Localize("$vt_comfort_group_other");
            }
        }

        private static void EnsureStyles()
        {
            if (s_header != null)
            {
                return;
            }

            s_header = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            s_header.normal.textColor = new Color(0.85f, 0.95f, 1f, 1f);
            s_category = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            s_category.normal.textColor = new Color(0.9f, 0.95f, 1f, 1f);
            s_cell = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            s_cell.normal.textColor = Color.white;
            s_value = new GUIStyle(s_cell)
            {
                alignment = TextAnchor.MiddleRight
            };
            s_owned = new GUIStyle(s_cell)
            {
                fontStyle = FontStyle.Bold
            };
            s_owned.normal.textColor = new Color(0.15f, 0.12f, 0.02f, 1f);
            s_ownedValue = new GUIStyle(s_owned)
            {
                alignment = TextAnchor.MiddleRight
            };
            s_ownedTex = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_ownedTex.SetPixel(0, 0, new Color(0.95f, 0.78f, 0.25f, 0.72f));
            s_ownedTex.Apply();
            s_lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_lineTex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.16f));
            s_lineTex.Apply();
        }

        private static void DrawScaleButtons()
        {
            GUIStyle style = GUI.skin.button;
            Rect minus = new Rect(s_rect.xMax - 64f, s_rect.y + 5f, 26f, 22f);
            Rect plus = new Rect(s_rect.xMax - 34f, s_rect.y + 5f, 26f, 22f);
            if (GUI.Button(minus, "-", style))
            {
                AdjustScale(-0.1f);
            }
            if (GUI.Button(plus, "+", style))
            {
                AdjustScale(0.1f);
            }
        }

        private class Row
        {
            public bool header;
            public int group;
            public int comfort;
            public string key;
            public string match;
            public string text;
        }
    }
}
