using System.Reflection;
using HarmonyLib;
using ValheimTooler.Core;

namespace ValheimTooler.Patches
{
    [HarmonyPatch]
    class InstantCraft
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(InventoryGui), "UpdateRecipe");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static void Prefix(ref float dt)
        {
            if (PlayerHacks.s_instantCraft)
            {
                dt = 2f;
            }
        }
    }
}
