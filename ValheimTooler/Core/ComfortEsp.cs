using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimTooler.UI;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class ComfortEsp
    {
        private static readonly List<Piece> s_winners = new List<Piece>();
        private static readonly List<LineRenderer> s_rings = new List<LineRenderer>();
        private static Material s_material;
        private static GUIStyle s_label;
        private static float s_comfortRadius = -1f;
        private static float s_nextScan;

        public static void Draw()
        {
            bool show = ConfigManager.s_comfortEsp != null && ConfigManager.s_comfortEsp.Value && Player.m_localPlayer != null && !Minimap.IsOpen();
            if (!show)
            {
                s_nextScan = 0f;
                SetRingCount(0);
                return;
            }

            if (Time.unscaledTime >= s_nextScan)
            {
                s_nextScan = Time.unscaledTime + 0.6f;
                RefreshWinners();
            }

            UpdateRings();

            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera camera = global::Utils.GetMainCamera();
            if (camera == null)
            {
                return;
            }

            EnsureLabel();
            for (int i = 0; i < s_winners.Count; i++)
            {
                Piece piece = s_winners[i];
                if (piece == null)
                {
                    continue;
                }

                Vector3 point = camera.WorldToScreenPointScaled(piece.transform.position + Vector3.up * 1.2f);
                if (point.z <= 0f)
                {
                    continue;
                }

                string name = Localization.instance != null ? Localization.instance.Localize(piece.m_name) : piece.m_name;
                string text = name + "  +" + piece.GetComfort();
                Rect rect = new Rect(point.x - 8f, Screen.height - point.y - 16f, 260f, 24f);
                DrawOutlined(rect, text, ColorFor(piece));
            }
        }

        private static void RefreshWinners()
        {
            s_winners.Clear();
            if (Player.m_localPlayer == null)
            {
                return;
            }

            HashSet<Piece> pieces = ComfortPieces();
            if (pieces == null)
            {
                return;
            }

            var best = new Dictionary<string, Piece>();
            var distance = new Dictionary<string, float>();
            Vector3 origin = Player.m_localPlayer.transform.position;
            float limit = ComfortRadius();
            foreach (Piece piece in pieces)
            {
                if (piece == null)
                {
                    continue;
                }

                int comfort = piece.GetComfort();
                if (comfort <= 0)
                {
                    continue;
                }

                float away = Vector3.Distance(origin, piece.transform.position);
                if (away >= limit)
                {
                    continue;
                }

                string key = piece.m_comfortGroup.ToString();
                if (key == "None" || key == "0")
                {
                    key = piece.GetInstanceID().ToString();
                }

                Piece current;
                float currentAway;
                if (!best.TryGetValue(key, out current) || comfort > current.GetComfort() || (comfort == current.GetComfort() && distance.TryGetValue(key, out currentAway) && away < currentAway))
                {
                    best[key] = piece;
                    distance[key] = away;
                }
            }

            foreach (Piece piece in best.Values)
            {
                s_winners.Add(piece);
            }
        }

        private static HashSet<Piece> ComfortPieces()
        {
            FieldInfo field = typeof(Piece).GetField("s_allComfortPieces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                return null;
            }

            return field.GetValue(null) as HashSet<Piece>;
        }

        private static void UpdateRings()
        {
            EnsureMaterial();
            SetRingCount(s_winners.Count);
            float reach = ComfortRadius();
            float height = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position.y + 0.05f : 0f;
            const int segments = 48;
            for (int i = 0; i < s_winners.Count; i++)
            {
                Piece piece = s_winners[i];
                LineRenderer ring = s_rings[i];
                if (piece == null)
                {
                    ring.gameObject.SetActive(false);
                    continue;
                }

                Vector3 position = piece.transform.position;
                float vertical = position.y - height;
                float flat = reach * reach - vertical * vertical;
                if (flat <= 0.05f)
                {
                    ring.gameObject.SetActive(false);
                    continue;
                }

                float radius = Mathf.Sqrt(flat);
                ring.gameObject.SetActive(true);
                Color color = ColorFor(piece);
                ring.startColor = color;
                ring.endColor = color;
                for (int p = 0; p < segments; p++)
                {
                    float angle = p * Mathf.PI * 2f / segments;
                    ring.SetPosition(p, new Vector3(position.x + Mathf.Cos(angle) * radius, height, position.z + Mathf.Sin(angle) * radius));
                }
            }
        }

        private static void SetRingCount(int count)
        {
            while (s_rings.Count < count)
            {
                GameObject root = new GameObject("VT_ComfortRing");
                UnityEngine.Object.DontDestroyOnLoad(root);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = true;
                line.widthMultiplier = 0.12f;
                line.positionCount = 48;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.material = s_material;
                line.startColor = Color.white;
                line.endColor = Color.white;
                root.SetActive(false);
                s_rings.Add(line);
            }

            for (int i = count; i < s_rings.Count; i++)
            {
                s_rings[i].gameObject.SetActive(false);
            }
        }

        private static void EnsureLabel()
        {
            if (s_label != null)
            {
                return;
            }

            s_label = new GUIStyle(InterfaceMaker.CustomSkin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            s_label.normal.textColor = Color.white;
        }

        private static readonly Color[] s_colors =
        {
            new Color(1f, 0.35f, 0.25f, 0.95f),
            new Color(0.25f, 0.85f, 1f, 0.95f),
            new Color(1f, 0.85f, 0.2f, 0.95f),
            new Color(0.75f, 0.45f, 1f, 0.95f),
            new Color(0.35f, 1f, 0.45f, 0.95f),
            new Color(1f, 0.45f, 0.75f, 0.95f),
            new Color(1f, 0.6f, 0.2f, 0.95f),
            new Color(0.45f, 0.55f, 1f, 0.95f)
        };

        private static Color ColorFor(Piece piece)
        {
            string key = piece.m_comfortGroup.ToString();
            int hash = key == "None" || key == "0" ? piece.GetInstanceID() : key.GetHashCode();
            int index = Mathf.Abs(hash % s_colors.Length);
            return s_colors[index];
        }

        private static void DrawOutlined(Rect rect, string text, Color color)
        {
            Color previous = s_label.normal.textColor;
            s_label.normal.textColor = new Color(0f, 0f, 0f, 0.95f);
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, s_label);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, s_label);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, s_label);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, s_label);
            s_label.normal.textColor = color;
            GUI.Label(rect, text, s_label);
            s_label.normal.textColor = previous;
        }

        private static float ComfortRadius()
        {
            if (s_comfortRadius > 0f)
            {
                return s_comfortRadius;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            FieldInfo field = typeof(Player).GetField("c_ComfortRadius", flags);
            if (field != null)
            {
                object value = field.IsStatic ? field.GetValue(null) : field.GetValue(Player.m_localPlayer);
                if (value is float)
                {
                    s_comfortRadius = (float)value;
                }
            }

            if (s_comfortRadius <= 0f)
            {
                s_comfortRadius = 10f;
            }

            return s_comfortRadius;
        }

        private static void EnsureMaterial()
        {
            if (s_material != null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            s_material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_material.SetInt("_ZTest", (int)CompareFunction.Always);
        }
    }
}
