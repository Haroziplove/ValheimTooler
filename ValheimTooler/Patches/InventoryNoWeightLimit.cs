using System.Reflection;
using HarmonyLib;
using ValheimTooler.Core;

namespace ValheimTooler.Patches
{
    [HarmonyPatch]
    class InventoryNoWeightLimit
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Inventory), nameof(Inventory.GetTotalWeight));
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix(ref float __result)
        {
            if (PlayerHacks.s_inventoryNoWeightLimit)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}
