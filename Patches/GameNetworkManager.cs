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
	private static void Postfix(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
		if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
			return;

		if (response.Reason.Contains("Game has already started") && GameNetworkManager.Instance.gameHasStarted) {
			response.Reason = "";
			response.CreatePlayerObject = false;
			response.Approved = true;
			response.Pending = false;
		}
	}
}

