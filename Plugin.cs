using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LateCompany;

public static class PluginInfo {
	public const string GUID = "twig.latecompany";
	public const string PrintName = "Late Company";
	public const string Version = "1.0.3";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.PrintName, PluginInfo.Version)]
internal class Plugin: BaseUnityPlugin
{
	public void Awake()
	{
		Harmony harmony = new Harmony(PluginInfo.GUID);
		harmony.PatchAll(typeof(Plugin).Assembly);
		Logger.Log(LogLevel.Info, "Late Company loaded!");
	}
}

