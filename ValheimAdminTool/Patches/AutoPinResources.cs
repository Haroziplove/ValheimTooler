using System.Reflection;
using HarmonyLib;
using ValheimAdminTool.Core;

namespace ValheimAdminTool.Patches
{
    [HarmonyPatch]
    class AutoPinResources
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Destructible), "Start");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static void Postfix(Destructible __instance)
        {
            MiscHacks.TryPinDeposit(__instance);
        }
    }
}
