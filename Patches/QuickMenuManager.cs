using HarmonyLib;

namespace LateCompany.Patches;

[HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.DisableInviteFriendsButton))]
internal static class DisableInviteFriendsButton_Patch
{
    [HarmonyPrefix]
    private static bool Prefix() { return false; }
}

[HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.InviteFriendsButton))]
internal static class InviteFriendsButton_Patch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        if (Plugin.LobbyJoinable) GameNetworkManager.Instance.InviteFriendsUI();

        return false;
    }
}