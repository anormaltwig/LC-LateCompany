using System.Reflection;
using System.Collections.Generic;

using Unity.Netcode;
using GameNetcodeStuff;

using HarmonyLib;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using System;

namespace LateCompany.Patches;

[HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
[HarmonyWrapSafe]
internal static class OnPlayerConnectedClientRpc_Patch {
	internal static void UpdateControlledState() {
		for (int j = 0; j < StartOfRound.Instance.connectedPlayersAmount + 1; j++) {
			// Don't set the player as controlled if they are dead.
			if ((j == 0 || !StartOfRound.Instance.allPlayerScripts[j].IsOwnedByServer) && !StartOfRound.Instance.allPlayerScripts[j].isPlayerDead) {
				StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled = true;
			}
		}
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
		var newInstructions = new List<CodeInstruction>();
		bool foundInitial = false;
		bool shouldSkip = false;
		bool alreadyReplaced = false;

		foreach (var instruction in instructions) {
			if (!alreadyReplaced) {
				if (!foundInitial && instruction.opcode == OpCodes.Call && instruction.operand != null && instruction.operand.ToString() == "System.Collections.IEnumerator setPlayerToSpawnPosition(UnityEngine.Transform, UnityEngine.Vector3)") {
					foundInitial = true;
				}
				else if (foundInitial && instruction.opcode == OpCodes.Ldc_I4_0) {
					shouldSkip = true;
					continue;
				}
				else if (shouldSkip && instruction.opcode == OpCodes.Ldloc_0) {
					shouldSkip = false;
					alreadyReplaced = true;
					newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OnPlayerConnectedClientRpc_Patch), "UpdateControlledState", new Type[] {})));
				}
			}

			if (!shouldSkip)
				newInstructions.Add(instruction);
		}

		if (!alreadyReplaced) Debug.LogError("Failed to transpile StartOfRound::OnPlayerConnectedClientRpc");

		return newInstructions.AsEnumerable();
	}

	public static MethodInfo BeginSendClientRpc = typeof(RoundManager).GetMethod("__beginSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);
	public static MethodInfo EndSendClientRpc = typeof(RoundManager).GetMethod("__endSendClientRpc", BindingFlags.NonPublic | BindingFlags.Instance);

	// Best guess at getting new players to load into the map after the game starts.
	[HarmonyPostfix]
	private static void Postfix(StartOfRound __instance, ulong clientId, int connectedPlayers, ulong[] connectedPlayerIdsOrdered, int assignedPlayerObjectId, int serverMoneyAmount, int levelID, int profitQuota, int timeUntilDeadline, int quotaFulfilled, int randomSeed) {
		if (__instance.connectedPlayersAmount + 1 >= __instance.allPlayerScripts.Length)
			Plugin.SetLobbyJoinable(false);

		PlayerControllerB ply = __instance.allPlayerScripts[assignedPlayerObjectId];
		// Make their player model visible.
		ply.DisablePlayerModel(__instance.allPlayerObjects[assignedPlayerObjectId], true, true);

		__instance.livingPlayers = __instance.connectedPlayersAmount + 1;
		for (int i = 0; i < __instance.allPlayerScripts.Length; i++) {
			PlayerControllerB pcb = __instance.allPlayerScripts[i];
			if (pcb.isPlayerControlled && pcb.isPlayerDead) __instance.livingPlayers--;
		}

		if (__instance.IsServer && !__instance.inShipPhase) {
			RoundManager rm = RoundManager.Instance;

			ClientRpcParams clientRpcParams = new() {
				Send = new ClientRpcSendParams() {
					TargetClientIds = new List<ulong>() { clientId },
				},
			};

			// Tell the new client to generate the level.
			{
				FastBufferWriter fastBufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(rm, new object[] { 1193916134U, clientRpcParams, 0 });
				BytePacker.WriteValueBitPacked(fastBufferWriter, __instance.randomMapSeed);
				BytePacker.WriteValueBitPacked(fastBufferWriter, __instance.currentLevelID);
				BytePacker.WriteValueBitPacked(fastBufferWriter, (int)rm.currentLevel.currentWeather + 0xFF);
				EndSendClientRpc.Invoke(rm, new object[] { fastBufferWriter, 1193916134U, clientRpcParams, 0 });
			}

			// And also tell them that everyone is done generating it.
			{
				FastBufferWriter fastBufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(rm, new object[] { 2729232387U, clientRpcParams, 0 });
				EndSendClientRpc.Invoke(rm, new object[] { fastBufferWriter, 2729232387U, clientRpcParams, 0 });
			}
		}
	}
}

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
[HarmonyWrapSafe]
internal static class OnPlayerDC_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		if (StartOfRound.Instance.inShipPhase || (Plugin.AllowJoiningWhileLanded && StartOfRound.Instance.shipHasLanded))
			Plugin.SetLobbyJoinable(true);
	}
}

[HarmonyPatch(typeof(StartOfRound), "SetShipReadyToLand")]
internal static class SetShipReadyToLand_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		if (StartOfRound.Instance.connectedPlayersAmount + 1 < StartOfRound.Instance.allPlayerScripts.Length)
			Plugin.SetLobbyJoinable(true);
	}
}

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
internal static class StartGame_Patch {
	[HarmonyPrefix]
	private static void Prefix() {
		Plugin.SetLobbyJoinable(false);
	}
}

[HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
internal static class OnShipLandedMiscEvents_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		if (Plugin.AllowJoiningWhileLanded && StartOfRound.Instance.connectedPlayersAmount + 1 < StartOfRound.Instance.allPlayerScripts.Length)
			Plugin.SetLobbyJoinable(true);
	}
}

[HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
internal static class ShipLeave_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		Plugin.SetLobbyJoinable(false);
	}
}
