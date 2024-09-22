using HarmonyLib;

namespace XLScrapApi.Patches.EntranceTeleportPatch;

[HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerClientRpc))]
public class TeleportPlayerClientPatch : TeleportPlayerBase
{
    [HarmonyPrefix]
    public static bool Prefix(EntranceTeleport __instance, int playerObj)
    {
        var player = __instance.playersManager.allPlayerScripts[playerObj];
        if (!IsTeleportingWithXl(player, out _))
        {
            return true;
        }

        Plugin.Logger.LogDebug($"Skipped teleport on ClientRpc (XL in hand)");
        return false;
    }
}
