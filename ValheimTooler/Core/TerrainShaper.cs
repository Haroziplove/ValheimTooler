using System;
using System.Collections.Generic;
using System.Linq;
using RapidGUI;
using UnityEngine;
using ValheimTooler.Core.Extensions;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class TerrainShaper
    {
        private static float s_radius = 10f;
        private static float s_depth = 1f;
        private static float s_strength = 0.01f;
        private static TerrainModifier.PaintType s_paintType = TerrainModifier.PaintType.Dirt;

        public static float Radius => s_radius;

        public static void Start()
        {
            return;
        }

        public static void Update()
        {
            if (ConfigManager.s_terrainShapeShortcut.Value.IsDown())
            {
                ActionToggleTerrainShape(true);
            }
            if (ConfigManager.s_terrainLevelShortcut.Value.IsDown())
            {
                ActionTerrainLevel();
            }
            if (ConfigManager.s_terrainLowerShortcut.Value.IsDown())
            {
                ActionTerrainLower();
            }
            if (ConfigManager.s_terrainRaiseShortcut.Value.IsDown())
            {
                ActionTerrainRaise();
            }
            if (ConfigManager.s_terrainResetShortcut.Value.IsDown())
            {
                ActionTerrainReset();
            }
            if (ConfigManager.s_terrainSmoothShortcut.Value.IsDown())
            {
                ActionTerrainSmooth();
            }
            if (ConfigManager.s_terrainPaintShortcut.Value.IsDown())
            {
                ActionTerrainPaint();
            }

            TerrainRadiusPreview.UpdateFromUi();
        }

        public static bool IsPreviewHover(string action)
        {
            return action == "$vt_terrainshaper_radius"
                || action == "$vt_terrainshaper_shape"
                || action == "$vt_terrainshaper_action_level"
                || action == "$vt_terrainshaper_action_lower"
                || action == "$vt_terrainshaper_action_raise"
                || action == "$vt_terrainshaper_action_reset"
                || action == "$vt_terrainshaper_action_smooth"
                || action == "$vt_terrainshaper_action_paint";
        }

        public static void DisplayGUI()
        {
            UI.Controls.BeginSection("$vt_terrainshaper_settings");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(VTLocalization.instance.Localize("$vt_terrainshaper_radius (") + s_radius.ToString("F1") + ")", GUILayout.ExpandWidth(false));
                    UI.Controls.NoteHoverAction("$vt_terrainshaper_radius");
                    UI.Controls.NoteHoverTooltip(UI.Controls.Tip("$vt_terrainshaper_radius"));
                    s_radius = GUILayout.HorizontalSlider(s_radius, 1f, 50f, GUILayout.ExpandWidth(true));
                    UI.Controls.NoteHoverAction("$vt_terrainshaper_radius");
                    UI.Controls.NoteHoverTooltip(UI.Controls.Tip("$vt_terrainshaper_radius"));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(VTLocalization.instance.Localize("$vt_terrainshaper_depth (") + s_depth.ToString("F2") + ")", GUILayout.ExpandWidth(false));
                    s_depth = GUILayout.HorizontalSlider(s_depth, 1f, 10f, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(VTLocalization.instance.Localize("$vt_terrainshaper_strength (") + s_strength.ToString("F2") + ")", GUILayout.ExpandWidth(false));
                    s_strength = GUILayout.HorizontalSlider(s_strength, 0.001f, 0.1f, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();

                string shapeTip = VTLocalization.instance.Localize("$vt_terrainshaper_shape_tip");
                if (GUILayout.Button(new GUIContent(UI.Utils.ToggleButtonLabelCustom("$vt_terrainshaper_shape", Ground.square, "$vt_terrainshaper_shape_square", "$vt_terrainshaper_shape_circle", ConfigManager.s_terrainShapeShortcut.Value), shapeTip), GUILayout.MinHeight(28)))
                {
                    ActionToggleTerrainShape();
                }
                UI.Controls.NoteHoverTooltip(shapeTip);
                UI.Controls.NoteHoverAction("$vt_terrainshaper_shape");
            }
            UI.Controls.EndSection();

            UI.Controls.BeginSection("$vt_terrainshaper_actions");
            {
                if (UI.Controls.ActionButton("$vt_terrainshaper_action_level", FeatureMethod.Direct, ConfigManager.s_terrainLevelShortcut.Value))
                {
                    ActionTerrainLevel();
                }
                if (UI.Controls.ActionButton("$vt_terrainshaper_action_lower", FeatureMethod.Direct, ConfigManager.s_terrainLowerShortcut.Value))
                {
                    ActionTerrainLower();
                }
                if (UI.Controls.ActionButton("$vt_terrainshaper_action_raise", FeatureMethod.Direct, ConfigManager.s_terrainRaiseShortcut.Value))
                {
                    ActionTerrainRaise();
                }
                if (UI.Controls.ActionButton("$vt_terrainshaper_action_reset", FeatureMethod.Direct, ConfigManager.s_terrainResetShortcut.Value))
                {
                    ActionTerrainReset();
                }
                if (UI.Controls.ActionButton("$vt_terrainshaper_action_smooth", FeatureMethod.Direct, ConfigManager.s_terrainSmoothShortcut.Value))
                {
                    ActionTerrainSmooth();
                }
            }
            UI.Controls.EndSection();

            UI.Controls.BeginSection("$vt_terrainshaper_painter");
            {

                GUILayout.BeginHorizontal();
                {
                    UI.Controls.FieldLabel("$vt_terrainshaper_paint_type");

                    var enumValues = Enum.GetValues(typeof(TerrainModifier.PaintType)).Cast<object>().ToList();
                    var idx = enumValues.IndexOf(s_paintType);
                    var valueNames = enumValues.Select(value => value.ToString()).ToArray();

                    idx = RGUI.SelectionPopup(idx, valueNames);

                    s_paintType = (TerrainModifier.PaintType)enumValues.ElementAtOrDefault(idx);
                }
                GUILayout.EndHorizontal();

                if (UI.Controls.ActionButton("$vt_terrainshaper_action_paint", FeatureMethod.Direct, ConfigManager.s_terrainPaintShortcut.Value))
                {
                    ActionTerrainPaint();
                }
            }
            UI.Controls.EndSection();
        }

        private static void ActionToggleTerrainShape(bool sendNotification = false)
        {
            Ground.square = !Ground.square;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabelCustom("$vt_terrainshaper_shape", Ground.square, "$vt_terrainshaper_shape_square", "$vt_terrainshaper_shape_circle"));
            }
        }

        private static void ActionTerrainLevel()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Level(Player.m_localPlayer.transform.position, s_radius);
            }
        }

        private static void ActionTerrainLower()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Lower(Player.m_localPlayer.transform.position, s_radius, s_depth, s_strength);
            }
        }


        private static void ActionTerrainRaise()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Raise(Player.m_localPlayer.transform.position, s_radius, s_depth, s_strength);
            }
        }

        private static void ActionTerrainReset()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Reset(Player.m_localPlayer.transform.position, s_radius);
            }
        }

        private static void ActionTerrainSmooth()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Smooth(Player.m_localPlayer.transform.position, s_radius, s_strength);
            }
        }

        private static void ActionTerrainPaint()
        {
            if (Player.m_localPlayer != null)
            {
                Ground.Paint(Player.m_localPlayer.transform.position, s_radius, s_paintType);
            }
        }
    }

    // Class originally created by Gungnir mod: https://github.com/zambony/Gungnir
    internal static class Ground
    {
        public static bool square = false;

        public static Quaternion FacingRotation()
        {
            if (Player.m_localPlayer == null)
            {
                return Quaternion.identity;
            }

            Vector3 forward = Player.m_localPlayer.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        public static void Level(Vector3 position, float radius)
        {
            var settings = new TerrainOp.Settings
            {
                m_level = true,
                m_levelRadius = radius,
                m_levelOffset = 0f,
                m_square = square
            };
            Apply(position, radius, settings);
        }

        public static void Raise(Vector3 position, float radius, float height, float strength = 0.01f)
        {
            var settings = new TerrainOp.Settings
            {
                m_raise = true,
                m_raiseRadius = radius,
                m_raiseDelta = height,
                m_raisePower = strength,
                m_square = square
            };
            Apply(position, radius, settings);
        }

        public static void Lower(Vector3 position, float radius, float depth, float strength = 0.01f)
        {
            var settings = new TerrainOp.Settings
            {
                m_raise = true,
                m_raiseRadius = radius,
                m_raiseDelta = -depth,
                m_raisePower = strength,
                m_square = square
            };
            Apply(position, radius, settings);
        }

        public static void Smooth(Vector3 position, float radius, float strength = 0.5f)
        {
            var settings = new TerrainOp.Settings
            {
                m_smooth = true,
                m_smoothRadius = radius,
                m_smoothPower = strength,
                m_square = square
            };
            Apply(position, radius, settings);
        }

        public static void Paint(Vector3 position, float radius, TerrainModifier.PaintType type)
        {
            var settings = new TerrainOp.Settings
            {
                m_paintCleared = true,
                m_paintRadius = radius,
                m_paintType = type,
                m_paintStrength = 1f,
                m_square = square
            };
            Apply(position, radius, settings);
        }

        public static void Reset(Vector3 position, float radius)
        {
            foreach (var obj in TerrainModifier.GetAllInstances())
            {
                if (global::Utils.DistanceXZ(position, obj.transform.position) <= radius)
                {
                    ZNetView netView = obj.GetComponent<ZNetView>();
                    if (netView == null)
                    {
                        continue;
                    }

                    netView.ClaimOwnership();
                    netView.Destroy();
                }
            }

            List<Heightmap> heightmaps = new List<Heightmap>();
            Heightmap.FindHeightmap(position, radius + 50f, heightmaps);

            foreach (Heightmap heightmap in heightmaps)
            {
                TerrainComp compiler = heightmap.GetAndCreateTerrainCompiler();
                if (compiler == null || !compiler.GetFieldValue<bool>("m_initialized"))
                {
                    continue;
                }

                ClaimCompiler(compiler);

                heightmap.WorldToVertex(position, out int x, out int y);
                float scale = heightmap.GetFieldValue<float>("m_scale");
                if (scale <= 0f)
                {
                    scale = 1f;
                }

                int width = compiler.GetFieldValue<int>("m_width");
                float[] levelDelta = compiler.GetFieldValue<float[]>("m_levelDelta");
                float[] smoothDelta = compiler.GetFieldValue<float[]>("m_smoothDelta");
                bool[] modifiedHeight = compiler.GetFieldValue<bool[]>("m_modifiedHeight");
                Color[] paintMask = compiler.GetFieldValue<Color[]>("m_paintMask");
                bool[] modifiedPaint = compiler.GetFieldValue<bool[]>("m_modifiedPaint");
                if (levelDelta == null || modifiedHeight == null)
                {
                    continue;
                }

                for (int h = 0; h <= width; ++h)
                {
                    for (int w = 0; w <= width; ++w)
                    {
                        if (!InsideReset(heightmap, position, w, h, radius, scale, width))
                        {
                            continue;
                        }

                        int heightIndex = w + (h * (width + 1));
                        if (heightIndex >= 0 && heightIndex < modifiedHeight.Length)
                        {
                            modifiedHeight[heightIndex] = false;
                            levelDelta[heightIndex] = 0f;
                            if (smoothDelta != null && heightIndex < smoothDelta.Length)
                            {
                                smoothDelta[heightIndex] = 0f;
                            }
                        }

                        if (h < width && w < width && modifiedPaint != null && paintMask != null)
                        {
                            int paintIndex = w + (h * width);
                            if (paintIndex >= 0 && paintIndex < modifiedPaint.Length)
                            {
                                modifiedPaint[paintIndex] = false;
                                paintMask[paintIndex] = Color.clear;
                            }
                        }
                    }
                }

                compiler.SetFieldValue("m_levelDelta", levelDelta);
                compiler.SetFieldValue("m_smoothDelta", smoothDelta);
                compiler.SetFieldValue("m_modifiedHeight", modifiedHeight);
                compiler.SetFieldValue("m_paintMask", paintMask);
                compiler.SetFieldValue("m_modifiedPaint", modifiedPaint);
                compiler.CallMethod("Save", false);

                ZNetView netView = compiler.GetComponent<ZNetView>();
                if (netView != null && netView.IsValid() && ZDOMan.instance != null)
                {
                    ZDO zdo = netView.GetZDO();
                    if (zdo != null)
                    {
                        ZDOMan.instance.ForceSendZDO(zdo.m_uid);
                    }
                }

                heightmap.Poke(0, false);
            }

            if (ClutterSystem.instance != null)
            {
                ClutterSystem.instance.ResetGrass(position, radius);
            }
        }

        private static bool InsideReset(Heightmap heightmap, Vector3 center, int x, int y, float radius, float scale, int width)
        {
            Vector3 world = VertexWorld(heightmap, x, y, scale, width);
            if (!square)
            {
                return global::Utils.DistanceXZ(center, world) <= radius;
            }

            return InsideFacingShape(center, world, radius, Quaternion.Inverse(FacingRotation()));
        }

        private static void Apply(Vector3 position, float radius, TerrainOp.Settings settings)
        {
            List<Heightmap> heightmaps = new List<Heightmap>();
            Heightmap.FindHeightmap(position, radius * 1.5f + 16f, heightmaps);

            foreach (Heightmap heightmap in heightmaps)
            {
                TerrainComp compiler = heightmap.GetAndCreateTerrainCompiler();
                if (compiler == null)
                {
                    continue;
                }

                if (!compiler.GetFieldValue<bool>("m_initialized"))
                {
                    compiler.CallMethod("Initialize");
                }

                ClaimCompiler(compiler);
                if (square)
                {
                    ApplyRotated(heightmap, compiler, position, radius, settings);
                }
                else
                {
                    compiler.CallMethod("DoOperation", position, Vector3.zero, settings);
                }
            }
        }

        private static void ApplyRotated(Heightmap heightmap, TerrainComp compiler, Vector3 position, float radius, TerrainOp.Settings settings)
        {
            heightmap.WorldToVertex(position, out int originX, out int originY);
            float scale = heightmap.GetFieldValue<float>("m_scale");
            if (scale <= 0f)
            {
                scale = 1f;
            }

            int width = compiler.GetFieldValue<int>("m_width");
            int pitch = width + 1;
            int extent = Mathf.CeilToInt(radius * 1.42f / scale) + 2;
            float[] levelDelta = compiler.GetFieldValue<float[]>("m_levelDelta");
            float[] smoothDelta = compiler.GetFieldValue<float[]>("m_smoothDelta");
            bool[] modifiedHeight = compiler.GetFieldValue<bool[]>("m_modifiedHeight");
            Color[] paintMask = compiler.GetFieldValue<Color[]>("m_paintMask");
            bool[] modifiedPaint = compiler.GetFieldValue<bool[]>("m_modifiedPaint");
            if (levelDelta == null || modifiedHeight == null)
            {
                return;
            }

            float targetLocalY = position.y - compiler.transform.position.y;
            Quaternion inverseFacing = Quaternion.Inverse(FacingRotation());
            Color paintColor = PaintColor(settings.m_paintType);

            for (int y = originY - extent; y <= originY + extent; y++)
            {
                for (int x = originX - extent; x <= originX + extent; x++)
                {
                    if (x < 0 || y < 0 || x >= pitch || y >= pitch)
                    {
                        continue;
                    }

                    Vector3 world = VertexWorld(heightmap, x, y, scale, width);
                    if (!InsideFacingShape(position, world, radius, inverseFacing))
                    {
                        continue;
                    }

                    int heightIndex = y * pitch + x;
                    if (heightIndex < 0 || heightIndex >= modifiedHeight.Length)
                    {
                        continue;
                    }

                    if (settings.m_level)
                    {
                        ApplyLevelVertex(heightmap, x, y, heightIndex, targetLocalY, levelDelta, smoothDelta, modifiedHeight);
                    }
                    else if (settings.m_raise)
                    {
                        ApplyRaiseVertex(heightmap, x, y, heightIndex, targetLocalY, settings.m_raiseDelta, levelDelta, smoothDelta, modifiedHeight);
                    }
                    else if (settings.m_smooth)
                    {
                        ApplySmoothVertex(heightmap, x, y, heightIndex, targetLocalY, settings.m_smoothPower, position, world, radius, smoothDelta, modifiedHeight);
                    }

                    if (settings.m_paintCleared && paintMask != null && modifiedPaint != null)
                    {
                        int paintIndex = heightIndex < paintMask.Length ? heightIndex : x + y * width;
                        if (paintIndex >= 0 && paintIndex < paintMask.Length && paintIndex < modifiedPaint.Length)
                        {
                            paintMask[paintIndex] = paintColor;
                            modifiedPaint[paintIndex] = true;
                        }
                    }
                }
            }

            compiler.SetFieldValue("m_levelDelta", levelDelta);
            compiler.SetFieldValue("m_smoothDelta", smoothDelta);
            compiler.SetFieldValue("m_modifiedHeight", modifiedHeight);
            compiler.SetFieldValue("m_paintMask", paintMask);
            compiler.SetFieldValue("m_modifiedPaint", modifiedPaint);
            compiler.CallMethod("Save", false);
            heightmap.Poke(0, false);
            if (ClutterSystem.instance != null)
            {
                ClutterSystem.instance.ResetGrass(position, radius);
            }
        }

        private static void ApplyLevelVertex(Heightmap heightmap, int x, int y, int index, float targetLocalY, float[] levelDelta, float[] smoothDelta, bool[] modifiedHeight)
        {
            float height = heightmap.GetHeight(x, y);
            float delta = targetLocalY - height;
            if (smoothDelta != null && index < smoothDelta.Length)
            {
                delta += smoothDelta[index];
                smoothDelta[index] = 0f;
            }

            levelDelta[index] = Mathf.Clamp(levelDelta[index] + delta, -8f, 8f);
            modifiedHeight[index] = true;
        }

        private static void ApplyRaiseVertex(Heightmap heightmap, int x, int y, int index, float targetLocalY, float raiseDelta, float[] levelDelta, float[] smoothDelta, bool[] modifiedHeight)
        {
            float factor = 1f;
            float height = heightmap.GetHeight(x, y);
            float amount = raiseDelta * factor;
            float target = targetLocalY + amount;
            if (raiseDelta < 0f && target > height)
            {
                return;
            }
            if (raiseDelta > 0f && target < height)
            {
                return;
            }
            if (raiseDelta > 0f && target > height + amount)
            {
                target = height + amount;
            }

            float change = target - height;
            if (smoothDelta != null && index < smoothDelta.Length)
            {
                change += smoothDelta[index];
                smoothDelta[index] = 0f;
            }

            levelDelta[index] = Mathf.Clamp(levelDelta[index] + change, -8f, 8f);
            modifiedHeight[index] = true;
        }

        private static void ApplySmoothVertex(Heightmap heightmap, int x, int y, int index, float targetLocalY, float power, Vector3 center, Vector3 world, float radius, float[] smoothDelta, bool[] modifiedHeight)
        {
            float dist = global::Utils.DistanceXZ(center, world);
            float t = Mathf.Clamp01(dist / Mathf.Max(0.01f, radius));
            t = Mathf.Approximately(power, 3f) ? t * t * t : Mathf.Pow(t, power);
            float height = heightmap.GetHeight(x, y);
            float change = Mathf.Lerp(height, targetLocalY, 1f - t) - height;
            if (smoothDelta != null && index < smoothDelta.Length)
            {
                smoothDelta[index] = Mathf.Clamp(smoothDelta[index] + change, -1f, 1f);
            }

            modifiedHeight[index] = true;
        }

        private static bool InsideFacingShape(Vector3 center, Vector3 point, float radius, Quaternion inverseFacing)
        {
            Vector3 local = inverseFacing * new Vector3(point.x - center.x, 0f, point.z - center.z);
            return Mathf.Abs(local.x) <= radius && Mathf.Abs(local.z) <= radius;
        }

        private static Vector3 VertexWorld(Heightmap heightmap, int x, int y, float scale, int width)
        {
            int half = width / 2;
            Vector3 origin = heightmap.transform.position;
            return new Vector3(origin.x + (x - half) * scale, origin.y, origin.z + (y - half) * scale);
        }

        private static Color PaintColor(TerrainModifier.PaintType type)
        {
            switch (type)
            {
                case TerrainModifier.PaintType.Dirt:
                    return Heightmap.m_paintMaskDirt;
                case TerrainModifier.PaintType.Cultivate:
                    return Heightmap.m_paintMaskCultivated;
                case TerrainModifier.PaintType.Paved:
                    return Heightmap.m_paintMaskPaved;
                case TerrainModifier.PaintType.Reset:
                    return Heightmap.m_paintMaskNothing;
                case TerrainModifier.PaintType.ClearVegetation:
                    return Heightmap.m_paintMaskClearVegetation;
                case TerrainModifier.PaintType.DeepSnow:
                    return Heightmap.m_paintMaskDeepSnow;
                default:
                    return Color.clear;
            }
        }

        private static void ClaimCompiler(TerrainComp compiler)
        {
            ZNetView netView = compiler.GetComponent<ZNetView>();
            if (netView != null && netView.IsValid() && !netView.IsOwner())
            {
                netView.ClaimOwnership();
            }
        }

    }
}
