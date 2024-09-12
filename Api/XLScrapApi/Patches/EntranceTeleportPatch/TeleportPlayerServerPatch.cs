using HarmonyLib;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.EntranceTeleportPatch;

[HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerServerRpc))]
public class TeleportPlayerServerPatch
{
    [HarmonyPrefix]
    public static void Prefix(EntranceTeleport __instance, int playerObj)
    {
        var player = __instance.playersManager.allPlayerScripts[playerObj];
        if (player.currentlyHeldObjectServer == null
            || player.currentlyHeldObjectServer is not XLHolderItem teleportHolder
            || teleportHolder.MainItem is not XLMainItem xlItem
            || xlItem.HolderItems == null)
        {
            Plugin.Logger.LogDebug($"TeleportXlScrap Initial Invalid");
            return;
        }

        xlItem.TeleportXlScrapServer(__instance, playerObj);
    }
}
