using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using XLScrapApi.Models;
using XLScrapApi.Util;

namespace XLScrapApi.Patches.RoundManagerPatch;

// Note that this patch is likely to break if vanilla Lethal Company updates this function
// Usually this will be due to a shifted number being assigned to the variables we reference
// i.e. The constants we define at top of the class corresponding to each of the variables
[HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
public class SpawnScrapInLevelPatch
{
    private const float MaxSpawnDiff = 20f;

    private static int _failedSpawnAttempts = 0;
    private const int MaxFailedSpawnAttempts = 3;

    private const int Component1Ref = 18;
    private const int VRef = 17;
    private const int ArrayRef = 6;
    private const int RandomScrapSpawnRef = 7;

    private static bool HandleXlSpawn(GrabbableObject component)
    {
        if (component is not XLMainItem xlItem) return true;
        if (!XLSpawner.CorrectToValidPosition(xlItem)) return InvalidateXlSpawn(component);

        return true;
    }

    private static bool InvalidateXlSpawn(Component component)
    {
        // Invalid position so destroy the object and mark to continue
        Object.Destroy(component.gameObject);
        Plugin.Logger.LogWarning($"XL Spawn failed! ({_failedSpawnAttempts})");
        return false;
    }

    private static void HandleFailedXlSpawn(RoundManager __instance,
        int i,
        List<Item> scrapToSpawn,
        int[] array,
        List<RandomScrapSpawn> usedSpawns,
        RandomScrapSpawn randomScrapSpawn)
    {
        if (++_failedSpawnAttempts >= MaxFailedSpawnAttempts)
        {
            _failedSpawnAttempts = 0;
            scrapToSpawn[i] = __instance.currentLevel
                .spawnableScrap[__instance.GetRandomWeightedIndex(array)].spawnableItem;

            Plugin.Logger.LogWarning($"XL Spawn failed too many times: Rerolling spawned item");
        }
        usedSpawns.Remove(randomScrapSpawn);
        randomScrapSpawn.spawnUsed = false;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddXlSpawnLogic(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // Match from the end to get field operands for the index, locals, and scrapToSpawn
        matcher.End();
        matcher.MatchBack(false, [new(OpCodes.Beq)]);
        matcher.MatchBack(false, [new(OpCodes.Blt)]);
        matcher.MatchBack(false, [new(OpCodes.Ldloc_S)]);
        var localsField = matcher.InstructionAt(1).operand;
        var scrapToSpawnField = matcher.InstructionAt(2).operand;

        matcher.Advance(-1);
        matcher.MatchBack(false, [new(OpCodes.Ldloc_S)]);
        var indexField = matcher.InstructionAt(1).operand;
        // Use pre-existing label here if we can
        Label repeatSpawnTarget;
        if ((repeatSpawnTarget = matcher.Instruction.labels.FirstOrDefault()) == default)
        {
            repeatSpawnTarget = generator.DefineLabel();
            matcher.AddLabelsAt(matcher.Pos, [repeatSpawnTarget]);
        }

        // Match for usedSpawns (complicated steps since simpler matches failed)
        matcher.Start();
        matcher.MatchForward(false, [new(OpCodes.Ldstr)]);
        matcher.Advance(1);
        matcher.MatchForward(false, [new(OpCodes.Ldstr)]);
        matcher.MatchForward(false, [new(OpCodes.Br)]);
        matcher.MatchBack(false, [new(OpCodes.Ldloc_0)]);
        matcher.MatchBack(false, [new(OpCodes.Stfld)]);
        var usedSpawnsField = matcher.Instruction.operand;

        // Match at first usage of intList1 (after initialisation of the object)
        // To add a label to branch to if XL spawn handling is not invoked
        matcher.MatchForward(false, [new(OpCodes.Ldloc_3)]);
        matcher.MatchBack(false, [new(OpCodes.Ldloc_2)]);
        var continueSpawnTarget = generator.DefineLabel();
        matcher.AddLabelsAt(matcher.Pos, [continueSpawnTarget]);

        // We needed to pop _something_ out here (<= v60) for the next iteration to be clean; not sure what though...
        // Likely the internally generated DisplayClass class' V_14 instance for generics?

        /*
        if (!HandleXlSpawn(component1))
            HandleFailedXlSpawn(this, i, ScrapToSpawn, array, usedSpawns, randomScrapSpawn); pop; repeat iteration
        else
            continue iteration
        */
        matcher.InsertAndAdvance([
            new(OpCodes.Ldloc_S, Component1Ref),   // component1
            new(OpCodes.Call,           // HandleXlSpawn()
                AccessTools.Method(typeof(SpawnScrapInLevelPatch), nameof(HandleXlSpawn))),
            new(OpCodes.Brtrue,         // if true, continue iteration
                continueSpawnTarget),

            new(OpCodes.Ldarg_0),       // this
            new(OpCodes.Ldloc_S, VRef),
            new(OpCodes.Ldfld,
                indexField),            // i
            new(OpCodes.Ldloc_S, VRef),
            new(OpCodes.Ldfld,
                localsField),
            new(OpCodes.Ldfld,
                scrapToSpawnField),     // ScrapToSpawn
            new(OpCodes.Ldloc_S,        // 'array'
                ArrayRef),
            new(OpCodes.Ldloc_S, VRef),
            new(OpCodes.Ldfld,
                localsField),
            new(OpCodes.Ldfld,
                usedSpawnsField),       // usedSpawns
            new(OpCodes.Ldloc_S,        // randomScrapSpawn
                RandomScrapSpawnRef),
            new(OpCodes.Call,           // HandleFailedXlSpawn()
                AccessTools.Method(typeof(SpawnScrapInLevelPatch), nameof(HandleFailedXlSpawn))),

            new(OpCodes.Pop),           // pop excess stack content
            new(OpCodes.Br,             // repeat iteration
                repeatSpawnTarget)
        ]);

        return matcher.InstructionEnumeration();
    }
}
