using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using ValheimAdminToolMod.Patches;

namespace ValheimAdminToolMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class ValheimAdminToolMod : BaseUnityPlugin
    {
        const string PluginGUID = "com.github.Haroziplove.ValheimAdminTool";
        const string PluginName = "ValheimAdminTool";
        const string PluginVersion = "2.0.6.0";

        private Harmony _harmony;

        private void Awake()
        {
            FejdStartupPatch.OnGameInitialized += LoadPlugin;
            _harmony = Harmony.CreateAndPatchAll(typeof(FejdStartupPatch), PluginGUID);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            CallLoaderUnload();
        }

        private void LoadPlugin()
        {
            FejdStartupPatch.OnGameInitialized -= LoadPlugin;
            _harmony?.UnpatchSelf();
            _harmony = null;

            var valheimAssemblyFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var configPathFilePath = Path.Combine(valheimAssemblyFolder, "config_vt.path");

            File.WriteAllText(configPathFilePath, Paths.ConfigPath);

            CallLoaderInit();
        }

        private static Type GetLoaderType()
        {
            var typeEntryPoint = typeof(ValheimAdminTool.EntryPoint);
            var typeLoader = typeEntryPoint.Assembly.GetType("ValheimAdminTool.Loader");

            if (typeLoader is null)
            {
                throw new Exception("Can't find Type ValheimAdminTool.Loader");
            }

            return typeLoader;
        }

        private void CallLoader(string methodName)
        {
            var typeLoader = GetLoaderType();
            var methodInfo = typeLoader.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

            if (methodInfo is null)
            {
                throw new Exception("Can't find method " + methodName);
            }
            methodInfo.Invoke(obj: null, parameters: new object[] { });
            Logger.LogDebug($"Called ValheimAdminTool.Loader.{methodName}");
        }

        private void CallLoaderInit()
        {
            CallLoader("Init");
        }

        private void CallLoaderUnload()
        {
            CallLoader("Unload");
        }
    }
}
