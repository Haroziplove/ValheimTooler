using System.Reflection;
using HarmonyLib;
using ValheimAdminTool.Core;

namespace ValheimAdminTool.Patches
{
    [HarmonyPatch]
    class RecipeLearnLogRecipe
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "AddKnownRecipe", new[] { typeof(Recipe) });
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix(Player __instance, Recipe recipe)
        {
            if (__instance != Player.m_localPlayer)
            {
                return true;
            }

            string key = RecipeManager.KeyFromRecipe(recipe);
            return !RecipeManager.IsSuppressed(key);
        }

        private static void Postfix(Player __instance, Recipe recipe)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            string key = RecipeManager.KeyFromRecipe(recipe);
            if (!string.IsNullOrEmpty(key) && __instance.IsRecipeKnown(key))
            {
                RecipeManager.NoteLearned(key);
            }
        }
    }

    [HarmonyPatch]
    class RecipeLearnLogPiece
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "AddKnownPiece", new[] { typeof(Piece) });
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static bool Prefix(Player __instance, Piece piece)
        {
            if (__instance != Player.m_localPlayer)
            {
                return true;
            }

            string key = RecipeManager.KeyFromPiece(piece);
            return !RecipeManager.IsSuppressed(key);
        }

        private static void Postfix(Player __instance, Piece piece)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            string key = RecipeManager.KeyFromPiece(piece);
            if (!string.IsNullOrEmpty(key) && __instance.IsRecipeKnown(key))
            {
                RecipeManager.NoteLearned(key);
            }
        }
    }
}
