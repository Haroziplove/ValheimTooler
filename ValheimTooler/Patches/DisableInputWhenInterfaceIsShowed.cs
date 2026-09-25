using System.Reflection;
using HarmonyLib;
using UnityEngine.EventSystems;

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
                if (EntryPoint.IsToolInteractive() && EntryPoint.IsPointerOverTool())
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(EventSystem), "Update")]
        class BlockGameUiClicksOnTool
        {
            private static bool Prefix()
            {
                return !(EntryPoint.IsToolInteractive() && EntryPoint.IsPointerOverTool());
            }
        }
    }
}
