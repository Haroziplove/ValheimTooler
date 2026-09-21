using System.Reflection;
using HarmonyLib;

namespace ValheimTooler.Patches
{
    class DisableInputWhenInterfaceIsShowed
    {
        [HarmonyPatch]
        class PlayerControllerInInventoryEtc
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(PlayerController), "InInventoryEtc");
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static bool Prefix(ref bool __result)
            {
                if (EntryPoint.s_showMainWindow && EntryPoint.IsPointerOverTool())
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }
    }
}
