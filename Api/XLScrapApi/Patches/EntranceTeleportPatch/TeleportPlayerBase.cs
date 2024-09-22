using GameNetcodeStuff;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.EntranceTeleportPatch;

public class TeleportPlayerBase
{
    protected static bool IsTeleportingWithXl(PlayerControllerB player, out XLMainItem xlItemOut)
    {
        xlItemOut = null;
        if (player.currentlyHeldObjectServer == null
            || player.currentlyHeldObjectServer is not XLHolderItem teleportHolder
            || teleportHolder.MainItem is not XLMainItem xlItem
            || xlItem.HolderItems == null)
        {
            return false;
        }

        xlItemOut = xlItem;
        return true;
    }
}
