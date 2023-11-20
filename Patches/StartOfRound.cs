using System.Reflection;
using System.Collections.Generic;

using Unity.Netcode;
using GameNetcodeStuff;

using HarmonyLib;

namespace LateCompany.Patches;

[HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
[HarmonyWrapSafe]
internal class OnPlayerConnectedClientRpc_Patch {
	// Best guess at getting new players to load into the map after the game starts.
	[HarmonyPostfix]
	private static void Postfix(ulong clientId, int connectedPlayers, ulong[] connectedPlayerIdsOrdered, int assignedPlayerObjectId, int serverMoneyAmount, int levelID, int profitQuota, int timeUntilDeadline, int quotaFulfilled, int randomSeed) {
		StartOfRound sor = StartOfRound.Instance;
		PlayerControllerB ply = sor.allPlayerScripts[assignedPlayerObjectId];

		if (sor.IsServer && !sor.inShipPhase) {
			RoundManager rm = RoundManager.Instance;

			MethodInfo beginSendClientRpc = typeof(RoundManager).GetMethod("__beginSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo endSendClientRpc = typeof(RoundManager).GetMethod("__endSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);

			ClientRpcParams clientRpcParams = new() {
				Send = new ClientRpcSendParams() {
					TargetClientIds = new List<ulong>(){ clientId }
				}
			};

			// Tell the new client to generate the level.
			{
				FastBufferWriter fastBufferWriter = (FastBufferWriter)beginSendClientRpc.Invoke(rm, new object[]{1193916134U, clientRpcParams, 0});
				BytePacker.WriteValueBitPacked(fastBufferWriter, StartOfRound.Instance.randomMapSeed);
				BytePacker.WriteValueBitPacked(fastBufferWriter, StartOfRound.Instance.currentLevelID);
				endSendClientRpc.Invoke(rm, new object[]{fastBufferWriter, 1193916134U, clientRpcParams, 0});
			}

			// And also tell them that everyone is done generating it.
			{
				FastBufferWriter fastBufferWriter = (FastBufferWriter)beginSendClientRpc.Invoke(rm, new object[]{2729232387U, clientRpcParams, 0});
				endSendClientRpc.Invoke(rm, new object[]{fastBufferWriter, 2729232387U, clientRpcParams, 0});
			}

			// This is where I would tell the new player what the weather outside is, if I could find any function to do that.
			// Maybe I can find something to force the weather to be the same or just sync it myself.
		}

		// Ensure their player model is visible.
		// I think everyone in the lobby needs to mod installed for this to work but, oh well.
		ply.DisablePlayerModel(sor.allPlayerObjects[assignedPlayerObjectId], true, true);
	}
}

