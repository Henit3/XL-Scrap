using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace XLScrapApi.Patches.StartOfRoundPatch;

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
public class SyncShipUnlockablesClientPatch
{
    private static bool IsNotHolder(Component component)
    {
        // Effectively (component is not XLHolderItem), but PhysicsProp item can also be returned here
        var result = component.name != "Holder(Clone)";
        return result;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> StopHolderPosSync(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // Match for conditional before usage of elevatorTransform (where positions are synced)
        matcher.MatchForward(false, [new(OpCodes.Ldfld,
            AccessTools.Field(typeof(StartOfRound), nameof(StartOfRound.elevatorTransform)))]);
        matcher.MatchBack(false, [new(OpCodes.Brfalse)]);

        // Add condition to not sync holder items
        // if (... && array1[index5] is not XLHolder) { ... }
        matcher.InsertAndAdvance([
            new(OpCodes.Ldloc_0),       // array1
            new(OpCodes.Ldloc_S, 7),    // index5
            new(OpCodes.Ldelem_Ref),    // []
            new(OpCodes.Call,           // IsNotHolder()
                AccessTools.Method(typeof(SyncShipUnlockablesClientPatch), nameof(IsNotHolder))),
            new(OpCodes.And)            // &&
        ]);

        return matcher.InstructionEnumeration();
    }
}
