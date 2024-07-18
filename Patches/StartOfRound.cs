using System.Collections.Generic;

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

	[HarmonyPostfix]
	private static void Postfix() {
		if (StartOfRound.Instance.connectedPlayersAmount + 1 >= StartOfRound.Instance.allPlayerScripts.Length)
			Plugin.SetLobbyJoinable(false);
	}
}

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
[HarmonyWrapSafe]
internal static class OnPlayerDC_Patch {
	[HarmonyPostfix]
	private static void Postfix() {
		if (StartOfRound.Instance.inShipPhase)
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
