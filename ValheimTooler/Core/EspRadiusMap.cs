using UnityEngine;
using UnityEngine.UI;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class EspRadiusMap
    {
        private static Texture2D s_lineTex;
        private static readonly Color s_color = new Color(0.25f, 0.92f, 1f, 0.95f);

        public static void Draw()
        {
            if (!ConfigManager.s_espRadiusEnabled.Value || Minimap.instance == null || Player.m_localPlayer == null)
            {
                return;
            }

            if (!Minimap.IsOpen() && GameUiBlocksMinimapOverlay())
            {
                return;
            }

            Minimap minimap = Minimap.instance;
            RawImage image = GetVisibleMapImage(minimap);
            if (image == null || image.rectTransform == null)
            {
                return;
            }

            Rect clip = ScreenRect(image.rectTransform);
            if (clip.width < 8f || clip.height < 8f)
            {
                return;
            }

            Vector3 center = Player.m_localPlayer.transform.position;
            float radius = ConfigManager.s_espRadius.Value;
            const int segments = 64;
            Vector2 prev = WorldToGui(minimap, image, PointOnCircle(center, radius, 0f));
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 current = WorldToGui(minimap, image, PointOnCircle(center, radius, angle));
                DrawClippedLine(prev, current, clip);
                prev = current;
            }
        }

        private static bool GameUiBlocksMinimapOverlay()
        {
            if (InventoryGui.IsVisible())
            {
                return true;
            }

            return Menu.IsVisible();
        }

        private static RawImage GetVisibleMapImage(Minimap minimap)
        {
            if (Minimap.IsOpen())
            {
                return minimap.GetFieldValue<RawImage>("m_mapImageLarge");
            }

            GameObject smallRoot = minimap.GetFieldValue<GameObject>("m_smallRoot");
            if (smallRoot != null && smallRoot.activeInHierarchy)
            {
                return minimap.GetFieldValue<RawImage>("m_mapImageSmall");
            }

            return null;
        }

        private static Vector3 PointOnCircle(Vector3 center, float radius, float angle)
        {
            return new Vector3(center.x + Mathf.Cos(angle) * radius, center.y, center.z + Mathf.Sin(angle) * radius);
        }

        private static Vector2 WorldToGui(Minimap minimap, RawImage image, Vector3 world)
        {
            WorldToMap(minimap, world, out float mx, out float my);
            Vector2 local = MapPointToLocal(image, mx, my);
            Vector3 worldPoint = image.rectTransform.TransformPoint(local);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            return new Vector2(screen.x, Screen.height - screen.y);
        }

        private static void WorldToMap(Minimap minimap, Vector3 world, out float mx, out float my)
        {
            int textureSize = minimap.GetFieldValue<int>("m_textureSize");
            float pixelSize = minimap.GetFieldValue<float>("m_pixelSize");
            if (textureSize <= 0 || pixelSize <= 0f)
            {
                mx = 0.5f;
                my = 0.5f;
                return;
            }

            float half = textureSize / 2f;
            mx = (world.x / pixelSize + half) / textureSize;
            my = (world.z / pixelSize + half) / textureSize;
        }

        private static Vector2 MapPointToLocal(RawImage image, float mx, float my)
        {
            Rect uv = image.uvRect;
            float u = uv.width != 0f ? (mx - uv.x) / uv.width : mx;
            float v = uv.height != 0f ? (my - uv.y) / uv.height : my;
            Rect rect = image.rectTransform.rect;
            return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, u), Mathf.Lerp(rect.yMin, rect.yMax, v));
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
        private static void DrawClippedLine(Vector2 a, Vector2 b, Rect clip)
        {
            if (!Clip(clip, ref a, ref b))
            {
                return;
            }

            DrawLine(a, b);
        }

        private static void DrawLine(Vector2 a, Vector2 b)
        {
            if (s_lineTex == null)
            {
                s_lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                s_lineTex.SetPixel(0, 0, Color.white);
                s_lineTex.Apply();
            }

            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.5f)
            {
                return;
            }

            float angle = Vector2.SignedAngle(Vector2.right, delta);
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - 1f, length, 2f), s_lineTex, ScaleMode.StretchToFill, true, 0f, s_color, 0f, 0f);
            GUI.matrix = matrix;
        }

        private static bool Clip(Rect rect, ref Vector2 a, ref Vector2 b)
        {
            const int left = 1;
            const int right = 2;
            const int bottom = 4;
            const int top = 8;

            int Code(Vector2 p)
            {
                int code = 0;
                if (p.x < rect.xMin) code |= left;
                else if (p.x > rect.xMax) code |= right;
                if (p.y < rect.yMin) code |= top;
                else if (p.y > rect.yMax) code |= bottom;
                return code;
            }

            int codeA = Code(a);
            int codeB = Code(b);
            for (int i = 0; i < 8; i++)
            {
                if ((codeA | codeB) == 0)
                {
                    return true;
                }
                if ((codeA & codeB) != 0)
                {
                    return false;
                }

                int code = codeA != 0 ? codeA : codeB;
                Vector2 p = a;
                if ((code & left) != 0)
                {
                    p.y = a.y + (b.y - a.y) * (rect.xMin - a.x) / (b.x - a.x);
                    p.x = rect.xMin;
                }
                else if ((code & right) != 0)
                {
                    p.y = a.y + (b.y - a.y) * (rect.xMax - a.x) / (b.x - a.x);
                    p.x = rect.xMax;
                }
                else if ((code & top) != 0)
                {
                    p.x = a.x + (b.x - a.x) * (rect.yMin - a.y) / (b.y - a.y);
                    p.y = rect.yMin;
                }
                else
                {
                    p.x = a.x + (b.x - a.x) * (rect.yMax - a.y) / (b.y - a.y);
                    p.y = rect.yMax;
                }

                if (code == codeA)
                {
                    a = p;
                    codeA = Code(a);
                }
                else
                {
                    b = p;
                    codeB = Code(b);
                }
            }

            return false;
        }
    }
}
