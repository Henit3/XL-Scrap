using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using XLScrapApi.Models;

namespace XLScrapApi.Patches.DepositItemsDeskPatch;

[HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.PlaceItemOnCounter))]
public class PlaceItemOnCounterPatch
{
    private static bool HandleSellXl(DepositItemsDesk desk, PlayerControllerB player, Vector3 counterPos)
    {
        if (player.currentlyHeldObjectServer is not XLHolderItem holder
            || holder.MainItem == null) return false;

        holder.MainItem.transform.SetParent(desk.deskObjectsContainer.transform, worldPositionStays: true);
        holder.MainItem.SetOnCounterServerRpc(counterPos);

        return true;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddHandleSellXl(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // Match for the DiscardHeldObject call and skip if a XL Holder since this will be done by the server destroy call
        matcher.End();
        var earlyReturnTarget = generator.DefineLabel();
        matcher.AddLabelsAt(matcher.Pos, [earlyReturnTarget]);

        matcher.MatchBack(false, [new(OpCodes.Ldarg_1)]);

        matcher.InsertAndAdvance([
            new(OpCodes.Ldarg_0),       // this
            new(OpCodes.Ldarg_1),       // playerWhoTriggered
            new(OpCodes.Ldloc_0),       // placePosition
            new(OpCodes.Call,           // ShouldSkipHolderDrop()
                AccessTools.Method(typeof(PlaceItemOnCounterPatch), nameof(HandleSellXl))),
            new(OpCodes.Brtrue,
                earlyReturnTarget)      // if true, return early
        ]);

        return matcher.InstructionEnumeration();
    }
}
