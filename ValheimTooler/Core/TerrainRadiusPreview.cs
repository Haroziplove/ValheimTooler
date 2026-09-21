using UnityEngine;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class TerrainRadiusPreview
    {
        private static GameObject s_root;
        private static LineRenderer s_line;
        private static Material s_material;
        private static bool s_visible;

        public static void UpdateFromUi()
        {
            if (!EntryPoint.IsToolInteractive() || Player.m_localPlayer == null)
            {
                Hide();
                return;
            }

            string hover = UI.Controls.HoveredAction;
            Vector3 pos = Player.m_localPlayer.transform.position;
            if (hover == "$vt_player_clean_cheated_drops" || hover == "$vt_player_clean_cheated_drops_radius")
            {
                Tick(true, pos, CheatStatus.DropCleanRadius, false);
                return;
            }

            if (hover == "$vt_player_tame_creatures" || hover == "$vt_player_tame_radius")
            {
                Tick(true, pos, ConfigManager.s_tameRadius.Value, false);
                return;
            }

            if (hover == "$vt_entities_drops_radius_button" || hover == "$vt_entities_drops_radius")
            {
                Tick(true, pos, ConfigManager.s_removeDropsRadius.Value, false);
                return;
            }

            if (hover == "$vt_misc_esp_radius" || hover == "$vt_misc_radius_enable")
            {
                Tick(true, pos, ConfigManager.s_espRadius.Value, false);
                return;
            }

            if (hover == "$vt_misc_damage_button_radius" || hover == "$vt_misc_damage_radius")
            {
                Tick(true, pos, ConfigManager.s_killRadius.Value, false);
                return;
            }

            if (hover == "$vt_misc_autopin" || hover == "$vt_misc_autopin_radius")
            {
                Tick(true, pos, ConfigManager.s_autopinRadius.Value, false);
                return;
            }

            if (TerrainShaper.IsPreviewHover(hover))
            {
                Tick(true, pos, TerrainShaper.Radius, Ground.square);
                return;
            }

            Hide();
        }

        public static void Tick(bool show, Vector3 center, float radius, bool square)
        {
            if (!show || Player.m_localPlayer == null)
            {
                Hide();
                return;
            }

            EnsureRenderer();
            if (s_line == null)
            {
                return;
            }

            s_visible = true;
            if (!s_root.activeSelf)
            {
                s_root.SetActive(true);
            }

            float visualRadius = Mathf.Max(0.05f, radius - s_line.widthMultiplier * 0.5f);
            if (square)
            {
                BuildSquare(center, visualRadius);
            }
            else
            {
                BuildCircle(center, visualRadius);
            }

            if (s_material != null)
            {
                s_material.mainTextureScale = new Vector2(Mathf.Max(8f, radius * 0.7f), 1f);
            }
        }

        public static void Hide()
        {
            if (s_visible && s_root != null)
            {
                s_root.SetActive(false);
            }

            s_visible = false;
        }

        private static void EnsureRenderer()
        {
            if (s_line != null)
            {
                return;
            }

            s_root = new GameObject("VT_TerrainRadiusPreview");
            UnityEngine.Object.DontDestroyOnLoad(s_root);

            s_line = s_root.AddComponent<LineRenderer>();
            s_line.useWorldSpace = true;
            s_line.loop = true;
            s_line.widthMultiplier = 0.12f;
            s_line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s_line.receiveShadows = false;
            s_line.numCapVertices = 2;
            s_line.numCornerVertices = 2;
            s_line.textureMode = LineTextureMode.Tile;

            s_material = CreateDashMaterial();
            if (s_material != null)
            {
                s_line.material = s_material;
            }

            s_line.startColor = Color.white;
            s_line.endColor = Color.white;
        }

        private static Material CreateDashMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }
            if (shader == null)
            {
                return null;
            }

            Texture2D tex = new Texture2D(32, 4, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int x = 0; x < 32; x++)
            {
                Color color = x < 20 ? Color.white : Color.clear;
                for (int y = 0; y < 4; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = tex,
                color = Color.white
            };
            return material;
        }

        private static void BuildCircle(Vector3 center, float radius)
        {
            const int segments = 72;
            s_line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 point = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y, center.z + Mathf.Sin(angle) * radius);
                point.y = GroundY(point);
                s_line.SetPosition(i, point);
            }
        }

        private static void BuildSquare(Vector3 center, float radius)
        {
            Quaternion rotation = Ground.FacingRotation();
            Vector3[] corners =
            {
                center + rotation * new Vector3(-radius, 0f, -radius),
                center + rotation * new Vector3(radius, 0f, -radius),
                center + rotation * new Vector3(radius, 0f, radius),
                center + rotation * new Vector3(-radius, 0f, radius)
            };

            const int perSide = 16;
            s_line.positionCount = perSide * 4;
            int index = 0;
            for (int side = 0; side < 4; side++)
            {
                Vector3 a = corners[side];
                Vector3 b = corners[(side + 1) % 4];
                for (int i = 0; i < perSide; i++)
                {
                    Vector3 point = Vector3.Lerp(a, b, i / (float)perSide);
                    point.y = GroundY(point);
                    s_line.SetPosition(index++, point);
                }
            }
        }

        private static float GroundY(Vector3 pos)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(pos, out float height))
            {
                return height + 0.2f;
            }

            return pos.y + 0.2f;
        }
    }
}
