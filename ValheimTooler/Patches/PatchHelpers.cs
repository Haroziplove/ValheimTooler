using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ValheimTooler.Patches
{
    internal static class PatchHelpers
    {
        internal static IEnumerable<MethodBase> FindMethods(Type type, params string[] methodNames)
        {
            if (type == null)
            {
                yield break;
            }

            foreach (string methodName in methodNames)
            {
                MethodInfo method = AccessTools.Method(type, methodName);
                if (method != null)
                {
                    yield return method;
                }
            }
        }
    }
}
