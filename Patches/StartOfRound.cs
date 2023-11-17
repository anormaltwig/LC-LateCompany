using System.Reflection;
using System.Collections.Generic;

using Unity.Netcode;

using HarmonyLib;

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
[HarmonyWrapSafe]
internal class OnClientConnect_Patch
{
	[HarmonyPostfix]
	private static void Postfix(ulong clientId)
	{
		// Best guess at getting new players to load into the map after the game starts.
		StartOfRound sor = StartOfRound.Instance;

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
	}
}

