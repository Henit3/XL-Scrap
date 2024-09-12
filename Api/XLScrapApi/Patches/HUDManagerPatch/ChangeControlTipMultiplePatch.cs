using HarmonyLib;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.HUDManagerPatch;

[HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ChangeControlTipMultiple))]
public class ChangeControlTipMultiplePatch
{
    [HarmonyPostfix]
    public static void Postfix(HUDManager __instance, Item itemProperties)
    {
        var localHeldObject = StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer;
        if (localHeldObject is not XLHolderItem holder) return;
        __instance.controlTipLines[0].text = "Stop Holding " + holder.MainItem.itemProperties.itemName + " : [G]";
    }
}
