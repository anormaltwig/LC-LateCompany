using UnityEngine;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

namespace LateCompany;

public static class PluginInfo {
	public const string GUID = "twig.latecompany";
	public const string PrintName = "Late Company";
	public const string Version = "1.0.17";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.PrintName, PluginInfo.Version)]
internal class Plugin: BaseUnityPlugin {
	public void Awake() {
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

