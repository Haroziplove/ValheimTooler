using System.Reflection;
using HarmonyLib;
using ValheimTooler.Core.Extensions;

namespace ValheimTooler.Patches
{
    [HarmonyPatch]
    class EdgeMapKill
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "EdgeOfWorldKill");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix()
        {
            if (Player.m_localPlayer != null && Player.m_localPlayer.VTInGodMode())
            {
                return false;
            }
            return true;
        }
    }
}
