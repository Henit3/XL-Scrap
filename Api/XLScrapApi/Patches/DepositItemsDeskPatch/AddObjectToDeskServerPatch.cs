using HarmonyLib;
using Unity.Netcode;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.DepositItemsDeskPatch;

[HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.AddObjectToDeskServerRpc))]
public class AddObjectToDeskServerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(DepositItemsDesk __instance, NetworkObjectReference grabbableObjectNetObject)
    {
        // Must be host since we despawn
        var networkManager = __instance.NetworkManager;
        if (networkManager == null
            || !networkManager.IsListening
            || (!networkManager.IsServer && !networkManager.IsHost))
        {
            return true;
        }
        
        // Only target if adding a XL Holder
        if (!grabbableObjectNetObject.TryGet(out var grabbableObject)
            || grabbableObject.GetComponentInChildren<GrabbableObject>() is not XLHolderItem holder)
        {
            return true;
        }

        foreach (var mainHolder in holder.MainItem.HolderItems)
        {
            mainHolder.GetComponent<NetworkObject>().Despawn();
        }
        holder.MainItem.HolderItems = null;

        __instance.AddObjectToDeskServerRpc(holder.MainItem.gameObject.GetComponent<NetworkObject>());
        return false;
    }
}
