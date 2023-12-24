using System.Reflection;

using Unity.Netcode;

using HarmonyLib;

namespace LateCompany.Patches;

// I hate protected enums.
internal class RpcEnum: NetworkBehaviour {
	public static int None { get { return (int)NetworkBehaviour.__RpcExecStage.None; } }
	public static int Client { get { return (int)NetworkBehaviour.__RpcExecStage.Client; } }
	public static int Server { get { return (int)NetworkBehaviour.__RpcExecStage.Server; } }
}

internal static class WeatherSync {
	public static bool DoOverride = false;
	public static LevelWeatherType CurrentWeather = LevelWeatherType.None;
}

[HarmonyPatch(typeof(RoundManager), "__rpc_handler_1193916134")]
[HarmonyWrapSafe]
internal static class __rpc_handler_1193916134_Patch {
	public static FieldInfo RPCExecStage = typeof(NetworkBehaviour).GetField("__rpc_exec_stage", BindingFlags.NonPublic | BindingFlags.Instance);

	[HarmonyPrefix]
	private static bool Prefix(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) {
		NetworkManager networkManager = target.NetworkManager;
		if (networkManager != null && networkManager.IsListening) {
			try {
				int randomSeed;
				ByteUnpacker.ReadValueBitPacked(reader, out randomSeed);
				int levelID;
				ByteUnpacker.ReadValueBitPacked(reader, out levelID);

				int weatherId;
				ByteUnpacker.ReadValueBitPacked(reader, out weatherId);

				WeatherSync.CurrentWeather = (LevelWeatherType)weatherId;
				WeatherSync.DoOverride = true;

				RPCExecStage.SetValue(target, RpcEnum.Client);
				(target as RoundManager).GenerateNewLevelClientRpc(randomSeed, levelID);
				RPCExecStage.SetValue(target, RpcEnum.None);
			}
			catch {
				// Something went wrong, default to original method.
				WeatherSync.DoOverride = false;
				reader.Seek(0);
				return true;
			}
		}

		return false;
	}
}

[HarmonyPatch(typeof(RoundManager), "SetToCurrentLevelWeather")]
internal static class SetToCurrentLevelWeather_Patch {
	[HarmonyPrefix]
	private static void Prefix() {
		if (!WeatherSync.DoOverride) return;
		RoundManager.Instance.currentLevel.currentWeather = WeatherSync.CurrentWeather;
		WeatherSync.DoOverride = false;
	}
}
