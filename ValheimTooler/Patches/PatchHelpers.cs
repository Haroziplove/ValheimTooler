using System;
using System.Collections.Generic;
using System.Reflection;

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
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    yield return method;
                }
            }
        }
    }
}
