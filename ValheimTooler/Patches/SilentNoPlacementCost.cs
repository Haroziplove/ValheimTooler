using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimTooler.Utils;

namespace ValheimTooler.Patches
{
    public static class SilentNoPlacement
    {
        public static bool Enabled;

        public static bool IsActive(Player player)
        {
            return Enabled && (player == null || player == Player.m_localPlayer);
        }

        public static void RefreshPieces()
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.CallMethod("UpdateAvailablePiecesList");
            }
        }
    }

    class SilentNoPlacementCost
    {
        [HarmonyPatch]
        public class HaveRequirementsPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Player), "HaveRequirements", new[] { typeof(Piece), typeof(Player.RequirementMode) });
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static bool Prefix(Player __instance, ref bool __result)
            {
                if (!SilentNoPlacement.IsActive(__instance) || !__instance.InPlaceMode())
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch]
        public class ConsumeResourcesPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return AccessTools.GetDeclaredMethods(typeof(Player)).Where(m => m.Name == "ConsumeResources");
            }

            private static bool Prepare()
            {
                return TargetMethods().Any();
            }

            private static bool Prefix(Player __instance)
            {
                return !SilentNoPlacement.IsActive(__instance);
            }
        }

        [HarmonyPatch]
        public class HaveRecipeRequirementsPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(Player)))
                {
                    if (method.Name != "HaveRequirements")
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Recipe))
                    {
                        yield return method;
                    }
                }
            }

            private static bool Prepare()
            {
                return TargetMethods().Any();
            }

            private static bool Prefix(Player __instance, bool discover, ref bool __result)
            {
                if (!SilentNoPlacement.IsActive(__instance) || discover)
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch]
        public class AddItemUncheatedPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(Inventory)))
                {
                    if (method.Name != "AddItem")
                    {
                        continue;
                    }

                    if (method.GetParameters().Any(p => p.Name == "cheated"))
                    {
                        yield return method;
                    }
                }
            }

            private static bool Prepare()
            {
                return TargetMethods().Any();
            }

            private static void Prefix(ref bool cheated)
            {
                if (SilentNoPlacement.Enabled)
                {
                    cheated = false;
                }
            }
        }

        [HarmonyPatch]
        public class PlacePiecePatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Player), "PlacePiece", new[] { typeof(Piece), typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(bool) });
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static void Prefix(Player __instance, ref bool cheated)
            {
                if (SilentNoPlacement.IsActive(__instance))
                {
                    cheated = false;
                }
            }
        }

        [HarmonyPatch]
        public class UpdateAvailablePiecesListPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList");
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static bool Prefix(Player __instance)
            {
                if (!SilentNoPlacement.IsActive(__instance))
                {
                    return true;
                }

                PieceTable buildPieces = __instance.GetFieldValue<PieceTable>("m_buildPieces");
                if (buildPieces != null)
                {
                    HashSet<string> knownRecipes = __instance.GetFieldValue<HashSet<string>>("m_knownRecipes");
                    buildPieces.UpdateAvailable(knownRecipes, __instance, false, true);
                }

                __instance.CallMethod("SetupPlacementGhost");
                return false;
            }
        }

        [HarmonyPatch]
        public class CheckCanRemovePiecePatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Player), "CheckCanRemovePiece");
            }

            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static bool Prefix(Player __instance, ref bool __result)
            {
                if (!SilentNoPlacement.IsActive(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }
    }
}
