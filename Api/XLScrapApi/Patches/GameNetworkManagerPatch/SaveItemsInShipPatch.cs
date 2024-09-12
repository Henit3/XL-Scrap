using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.GameNetworkManagerPatch;

[HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip")]
public class SaveItemsInShipPatch
{
    [HarmonyPostfix]
    public static void Postfix(GameNetworkManager __instance)
    {
        if (!__instance.isHostingGame
            || !StartOfRound.Instance.inShipPhase
            || StartOfRound.Instance.isChallengeFile)
        {
            return;
        }

        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) return;

        ModSaveFile.Instance.SaveXlScrap(Object.FindObjectsByType<XLMainItem>(0, 0));
    }
}
