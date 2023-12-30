using UnityEngine;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using HarmonyLib;

namespace LateCompany;

public static class PluginInfo {
	public const string GUID = "twig.latecompany";
	public const string PrintName = "Late Company";
	public const string Version = "1.0.8";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.PrintName, PluginInfo.Version)]
internal class Plugin: BaseUnityPlugin {
	private ConfigEntry<bool> configLateJoinOrbitOnly;

	public static bool OnlyLateJoinInOrbit = false;

	public void Awake() {
		configLateJoinOrbitOnly = Config.Bind("General", "Late join orbit only", true, "Don't allow joining while the ship is not in orbit.");
		OnlyLateJoinInOrbit = configLateJoinOrbitOnly.Value;

		Harmony harmony = new Harmony(PluginInfo.GUID);
		harmony.PatchAll(typeof(Plugin).Assembly);
		Logger.Log(LogLevel.Info, "Late Company loaded!");
	}

	public static bool LobbyJoinable = true;

	static public void SetLobbyJoinable(bool joinable) {
		LobbyJoinable = joinable;

		GameNetworkManager.Instance.SetLobbyJoinable(joinable);

		QuickMenuManager quickMenu = Object.FindObjectOfType<QuickMenuManager>();
		if (quickMenu) quickMenu.inviteFriendsTextAlpha.alpha = joinable ? 1f : 0.2f;
	}
}

