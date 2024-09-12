using GameNetcodeStuff;
using HarmonyLib;

namespace XLScrapApi.Patches.PlayerControllerBPatch;

[HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
public class BeginGrabObjectPatch : HolderTargetPatchBase
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControllerB __instance, int ___interactableObjectsMask)
        => !(__instance.twoHanded || __instance.sinkingValue > 0.73000001907348633)
            && ShouldProcessAfterHolderCheck(__instance, ___interactableObjectsMask);
}
