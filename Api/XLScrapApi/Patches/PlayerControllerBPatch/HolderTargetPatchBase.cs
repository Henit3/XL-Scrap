using GameNetcodeStuff;
using UnityEngine;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.PlayerControllerBPatch;

public abstract class HolderTargetPatchBase
{
    private const float XlGrabDistance = 2.5f;

    // May want to transpile this later for efficiency
    // Would trade off readability/portability between versions though
    protected static bool ShouldProcessAfterHolderCheck(PlayerControllerB __instance,
        int ___interactableObjectsMask)
    {
        Ray interactRay = new(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
        if (!Physics.Raycast(interactRay, out var hit, __instance.grabDistance, ___interactableObjectsMask)
            || hit.collider.gameObject.layer == 8
            || hit.collider.tag != "PhysicsProp")
        {
            // If no grabbable object found, continue processing as usual (may incur second raycast)
            return true;
        }
        var grabTarget = hit.collider.transform.gameObject.GetComponent<GrabbableObject>();

        // Continue processing as usual if this object is not a holder
        if (grabTarget is not XLHolderItem targetHolder) return true;

        // Continue processing if the holder is within the smaller grab distance
        return Vector3.Distance(__instance.gameplayCamera.transform.position, targetHolder.transform.position) <= XlGrabDistance;
    }
}
