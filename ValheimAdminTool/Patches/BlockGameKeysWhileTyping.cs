using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ValheimAdminTool.Patches
{
    // Valheim reads every button and key through ZInput, so while a tool text field has keyboard
    // focus, report those as not pressed. Typing then no longer moves, jumps, or opens game menus.
    [HarmonyPatch]
    class BlockGameKeysWhileTyping
    {
        private static readonly string[] s_methods = { "GetButton", "GetButtonDown", "GetButtonUp", "GetKey", "GetKeyDown", "GetKeyUp" };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(ZInput))
                .Where(method => method.IsStatic && method.ReturnType == typeof(bool) && s_methods.Contains(method.Name))
                .Cast<MethodBase>();
        }

        private static bool Prepare()
        {
            return TargetMethods().Any();
        }

        private static bool Prefix(ref bool __result)
        {
            if (EntryPoint.IsTypingInTool())
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
