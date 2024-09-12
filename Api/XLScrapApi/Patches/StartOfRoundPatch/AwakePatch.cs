using HarmonyLib;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.StartOfRoundPatch;

[HarmonyPatch(typeof(StartOfRound), "Awake")]
public class AwakePatch
{
    [HarmonyPrefix]
    public static void Prefix(StartOfRound __instance)
        => ModSaveFile.Init();
}
