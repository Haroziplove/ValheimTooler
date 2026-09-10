using System.Reflection;
using HarmonyLib;
using ValheimTooler.Core;

namespace ValheimTooler.Patches
{
    [HarmonyPatch]
    class InfiniteStamina
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.UseStamina))
                ?? AccessTools.Method(typeof(Player), "UseStamina");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix(Player __instance)
        {
            if (PlayerHacks.s_isInfiniteStaminaMe && Player.m_localPlayer != null && __instance.GetPlayerID() == Player.m_localPlayer.GetPlayerID())
            {
                return false;
            }
            return true;
        }
    }
}
