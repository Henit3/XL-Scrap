using HarmonyLib;
using System.Linq;
using UnityEngine;
using XLScrapApi.Models;

namespace XlScrap.Patches.StartOfRoundPatch;

[HarmonyPatch(typeof(StartOfRound), "Awake")]
public class AwakePatch
{
    private static readonly string[] _xlItemsInThisMod =
        [
            "CRT TV",
            "Couch",
            "L Couch"
        ];

    [HarmonyPostfix]
    public static void Postfix(StartOfRound __instance)
    {
        if (!Plugin.Config.SensibleSpawns.Value) return;

        ItemGroup generalItemClassGroup = __instance.allItemsList.itemsList
            .SelectMany(x => x.spawnPositionTypes)
            .First(x => x.name == "GeneralItemClass");

        var xlItems = Resources.FindObjectsOfTypeAll<XLMainItem>()
            .Where(x => _xlItemsInThisMod.Contains(x.itemProperties.itemName));

        foreach (var xlItem in xlItems)
        {
            xlItem.itemProperties.spawnPositionTypes.Clear();
            xlItem.itemProperties.spawnPositionTypes.Add(generalItemClassGroup);
        }
    }
}
