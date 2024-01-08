using Unity.Netcode;

using HarmonyLib;

namespace LateCompany.Patches;

[HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.LeaveLobbyAtGameStart))]
[HarmonyWrapSafe]
internal static class LeaveLobbyAtGameStart_Patch {
	[HarmonyPrefix]
	private static bool Prefix() { return false; }
}

[HarmonyPatch(typeof(GameNetworkManager), "ConnectionApproval")]
[HarmonyWrapSafe]
internal static class ConnectionApproval_Patch {
	[HarmonyPostfix]
	private static void Postfix(ref NetworkManager.ConnectionApprovalRequest request, ref NetworkManager.ConnectionApprovalResponse response) {
		if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
			return;

		if (Plugin.LobbyJoinable && response.Reason == "Game has already started!") {
			response.Reason = "";
			response.Approved = true;
		}
	}
}
