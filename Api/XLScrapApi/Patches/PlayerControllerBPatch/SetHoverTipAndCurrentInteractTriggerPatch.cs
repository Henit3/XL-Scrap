using GameNetcodeStuff;
using HarmonyLib;

namespace XLScrapApi.Patches.PlayerControllerBPatch;

[HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
public class SetHoverTipAndCurrentInteractTriggerPatch : HolderTargetPatchBase
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControllerB __instance, int ___interactableObjectsMask)
        => ShouldProcessAfterHolderCheck(__instance, ___interactableObjectsMask);
}
