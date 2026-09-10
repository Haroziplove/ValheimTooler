using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ValheimTooler.Patches
{
    class DisableInputWhenInterfaceIsShowed
    {
        [HarmonyPatch]
        class PlayerTakeInput
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return PatchHelpers.FindMethods(typeof(Player), "TakeInput")
                    .Concat(PatchHelpers.FindMethods(typeof(PlayerController), "TakeInput"));
            }

            private static bool Prepare()
            {
                return TargetMethods().Any();
            }

            private static bool Prefix(ref bool __result)
            {
                if (EntryPoint.s_showMainWindow)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

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
                if (EntryPoint.s_showMainWindow)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        class InventoryInteraction
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return PatchHelpers.FindMethods(typeof(InventoryGrid), "OnLeftClick", "OnLeftDown", "OnRightClick", "OnRightDown")
                    .Concat(PatchHelpers.FindMethods(typeof(InventoryGui), "OnSelectedItem", "OnRightClickItem"));
            }

            private static bool Prepare()
            {
                return TargetMethods().Any();
            }

            private static bool Prefix()
            {
                if (EntryPoint.s_showMainWindow)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
