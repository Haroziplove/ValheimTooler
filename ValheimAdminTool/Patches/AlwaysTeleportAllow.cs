using System.Reflection;
using HarmonyLib;
using ValheimAdminTool.Core;

namespace ValheimAdminTool.Patches
{
    [HarmonyPatch]
    class AlwaysTeleportAllow
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Inventory), nameof(Inventory.IsTeleportable));
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix(ref bool __result)
        {
            if (PlayerHacks.s_bypassRestrictedTeleportable)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
