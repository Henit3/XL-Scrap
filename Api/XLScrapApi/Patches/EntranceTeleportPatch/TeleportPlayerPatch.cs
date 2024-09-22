using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Netcode;

namespace XLScrapApi.Patches.EntranceTeleportPatch;

[HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayer))]
public class TeleportPlayerPatch : TeleportPlayerBase
{
    private static bool HandleXlTeleport(EntranceTeleport instance, PlayerControllerB player)
    {
        if (!IsTeleportingWithXl(player, out var xlItem)) return false;

        xlItem.TeleportXlScrapServerRpc(instance.GetComponent<NetworkObject>(), (int)player.playerClientId);
        return true;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> StopEntranceTeleportWithXl(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // Match when we have met conditions for teleportation
        matcher.MatchForward(false, [new(OpCodes.Ldarg_0)]);
        matcher.Advance(1);
        matcher.MatchForward(false, [new(OpCodes.Ldarg_0)]);
        matcher.MatchBack(false, [new(OpCodes.Stloc_1)]);

        matcher.Advance(1);
        var normalTeleportTarget = generator.DefineLabel();
        matcher.AddLabelsAt(matcher.Pos, [normalTeleportTarget]);

        // if (HandleXlTeleport(this, GameNetworkManager.Instance.localPlayerController)) return;
        matcher.InsertAndAdvance([
            new(OpCodes.Ldarg_0),       // this
            new(OpCodes.Call,           // GameNetworkManager.Instance
                AccessTools.PropertyGetter(typeof(GameNetworkManager), nameof(GameNetworkManager.Instance))),
            new(OpCodes.Ldfld,          // .localPlayerController
                AccessTools.Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController))),
            new(OpCodes.Call,           // HandleXlTeleport()
                AccessTools.Method(typeof(TeleportPlayerPatch), nameof(HandleXlTeleport))),
            new(OpCodes.Brfalse,        // if false, skip to the actual part
                normalTeleportTarget),
            new(OpCodes.Ret)            // return
        ]);

        return matcher.InstructionEnumeration();
    }
}
