using HarmonyLib;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.Patches
{
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    class BlockCameraZoom
    {
        private static float s_distance;
        private static float s_zoomSens;
        private static bool s_block;

        private static void Prefix(GameCamera __instance)
        {
            s_block = EntryPoint.ShouldBlockCameraZoom();
            if (s_block)
            {
                s_distance = __instance.GetFieldValue<float>("m_distance");
                s_zoomSens = __instance.GetFieldValue<float>("m_zoomSens");
                __instance.SetFieldValue("m_zoomSens", 0f);
            }
        }

        private static void Postfix(GameCamera __instance)
        {
            if (s_block)
            {
                __instance.SetFieldValue("m_zoomSens", s_zoomSens);
                __instance.SetFieldValue("m_distance", s_distance);
            }
        }
    }

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    class BlockMouseScroll
    {
        private static void Postfix(ref float __result)
        {
            if (EntryPoint.ShouldBlockCameraZoom())
            {
                __result = 0f;
            }
        }
    }
}
