using System.Reflection;
using System.Collections.Generic;

using Unity.Netcode;
using GameNetcodeStuff;

using HarmonyLib;

namespace LateCompany.Patches;

[HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
[HarmonyWrapSafe]
internal static class OnPlayerConnectedClientRpc_Patch
{
    public static MethodInfo BeginSendClientRpc = typeof(RoundManager).GetMethod("__beginSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);
    public static MethodInfo EndSendClientRpc = typeof(RoundManager).GetMethod("__endSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);

    // Best guess at getting new players to load into the map after the game starts.
    [HarmonyPostfix]
    private static void Postfix(ulong clientId, int connectedPlayers, ulong[] connectedPlayerIdsOrdered, int assignedPlayerObjectId, int serverMoneyAmount, int levelID, int profitQuota, int timeUntilDeadline, int quotaFulfilled, int randomSeed)
    {
        StartOfRound sor = StartOfRound.Instance;
        PlayerControllerB ply = sor.allPlayerScripts[assignedPlayerObjectId];

        if (sor.connectedPlayersAmount + 1 >= sor.allPlayerScripts.Length)
        {
            Plugin.SetLobbyJoinable(false);
        }

        // Make their player model visible.
        ply.DisablePlayerModel(sor.allPlayerObjects[assignedPlayerObjectId], true, true);

        if (sor.IsServer && !sor.inShipPhase)
        {
            RoundManager rm = RoundManager.Instance;

            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams()
                {
                    TargetClientIds = new List<ulong>() { clientId },
                },
            };

            // Tell the new client to generate the level.
            {
                FastBufferWriter fastBufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(rm, new object[] { 1193916134U, clientRpcParams, 0 });
                BytePacker.WriteValueBitPacked(fastBufferWriter, StartOfRound.Instance.randomMapSeed);
                BytePacker.WriteValueBitPacked(fastBufferWriter, StartOfRound.Instance.currentLevelID);
                BytePacker.WriteValueBitPacked(fastBufferWriter, (short)rm.currentLevel.currentWeather);
                EndSendClientRpc.Invoke(rm, new object[] { fastBufferWriter, 1193916134U, clientRpcParams, 0 });
            }

            // And also tell them that everyone is done generating it.
            {
                FastBufferWriter fastBufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(rm, new object[] { 2729232387U, clientRpcParams, 0 });
                EndSendClientRpc.Invoke(rm, new object[] { fastBufferWriter, 2729232387U, clientRpcParams, 0 });
            }
        }

        sor.livingPlayers = sor.connectedPlayersAmount + 1;
        for (int i = 0; i < sor.allPlayerScripts.Length; i++)
        {
            PlayerControllerB pcb = sor.allPlayerScripts[i];
            if (pcb.isPlayerControlled && pcb.isPlayerDead) sor.livingPlayers--;
        }
    }
}

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
[HarmonyWrapSafe]
internal static class OnPlayerDC_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		if (StartOfRound.Instance.inShipPhase || (!Plugin.OnlyLateJoinInOrbit && StartOfRound.Instance.shipHasLanded)) {
			Plugin.SetLobbyJoinable(true);
		}
	}
}

[HarmonyPatch(typeof(StartOfRound), "SetShipReadyToLand")]
internal static class SetShipReadyToLand_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (Plugin.OnlyLateJoinInOrbit && StartOfRound.Instance.connectedPlayersAmount + 1 < StartOfRound.Instance.allPlayerScripts.Length)
        {
            Plugin.SetLobbyJoinable(true);
        }
    }
}

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
internal static class StartGame_Patch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        Plugin.SetLobbyJoinable(false);
    }
}

[HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
internal static class OnShipLandedMiscEvents_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!Plugin.OnlyLateJoinInOrbit && StartOfRound.Instance.connectedPlayersAmount + 1 < StartOfRound.Instance.allPlayerScripts.Length)
        {
            Plugin.SetLobbyJoinable(true);
        }
    }
}

[HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
internal static class ShipLeave_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        Plugin.SetLobbyJoinable(false);
    }
}