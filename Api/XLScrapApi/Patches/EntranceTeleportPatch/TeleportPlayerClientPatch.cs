using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.EntranceTeleportPatch;

[HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerClientRpc))]
public class TeleportPlayerClientPatch
{
    private static bool ShouldSkipTeleport(int playerObj)
    {
        var heldItem = StartOfRound.Instance.allPlayerScripts[playerObj].currentlyHeldObjectServer;
        return heldItem is XLHolderItem;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> StopEntranceTeleportWithXl(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // Match for the teleport player call and make a label after this
        matcher.MatchForward(false, [
            new(OpCodes.Callvirt,       // ShouldSkipTeleport()
                AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer)))
        ]);
        matcher.Advance(1);
        var skipTeleportTarget = generator.DefineLabel();
        matcher.AddLabelsAt(matcher.Pos, [skipTeleportTarget]);

        // Add the skip teleport condition before the instruction using the label created
        matcher.MatchBack(false, [new(OpCodes.Ldelem_Ref)]);
        matcher.MatchBack(false, [new(OpCodes.Ldarg_0)]);
        matcher.InsertAndAdvance([
            new(OpCodes.Ldarg_1),       // playerObj
            new(OpCodes.Call,           // ShouldSkipTeleport()
                AccessTools.Method(typeof(TeleportPlayerClientPatch), nameof(ShouldSkipTeleport))),
            new(OpCodes.Brtrue,         // if true, skip teleport
                skipTeleportTarget),
        ]);

        return matcher.InstructionEnumeration();
    }
}
