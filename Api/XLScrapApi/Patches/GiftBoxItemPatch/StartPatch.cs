using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.GiftBoxItemPatch;

[HarmonyPatch(typeof(GiftBoxItem), nameof(GiftBoxItem.Start))]
public class StartPatch
{
    private const int GiftboxItemId = 152767;
    private const int IndexRef = 4;

    private static bool IsXlAtIndex(List<SpawnableItemWithRarity> spawnChoices, int index)
    {
        var spawnPrefab = spawnChoices[index].spawnableItem.spawnPrefab;
        if (spawnPrefab == null) return false;

        return spawnPrefab.GetComponent<GrabbableObject>() is XLMainItem or XLHolderItem;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ExcludeXlItems(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(false, [new(OpCodes.Ldc_I4, GiftboxItemId)]);

        matcher.Advance(1);
        var normalWeightTarget = matcher.Instruction.operand;
        matcher.RemoveInstruction();

        // if (!(spawns[index].spawnableItem.itemId == GiftboxItemId || IsXlAtIndex(spawns, index))) ...
        matcher.InsertAndAdvance([
            new(OpCodes.Ceq),       // ==
            new(OpCodes.Call,       // RoundManager.Instance
                AccessTools.PropertyGetter(typeof(RoundManager), nameof(RoundManager.Instance))),
            new(OpCodes.Ldfld,      // .currentLevel
                AccessTools.Field(typeof(RoundManager), nameof(RoundManager.currentLevel))),
            new(OpCodes.Ldfld,      // .spawnableScrap
                AccessTools.Field(typeof(SelectableLevel), nameof(SelectableLevel.spawnableScrap))),
            new(OpCodes.Ldloc_S,      // index
                IndexRef),
            new(OpCodes.Callvirt,   // IsXlAtIndex()
                AccessTools.Method(typeof(StartPatch), nameof(StartPatch.IsXlAtIndex))),
            new(OpCodes.Or),        // ||
            new(OpCodes.Brfalse,
                normalWeightTarget)
        ]);

        return matcher.InstructionEnumeration();
    }
}
