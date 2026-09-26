using System.Reflection;
using HarmonyLib;
using ValheimAdminTool.Utils;

namespace ValheimAdminTool.Patches
{
    class NoPlacementCostRepair
    {
        [HarmonyPatch]
        public class CanRepairPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(InventoryGui), "CanRepair");
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static bool Prefix(ref bool __result)
            {
                if (Player.m_localPlayer == null)
                {
                    return true;
                }
                var m_noPlacementCost = Player.m_localPlayer.GetFieldValue<bool>("m_noPlacementCost");
                if (m_noPlacementCost || SilentNoPlacement.Enabled)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
