using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using XLScrapApi.Util;

namespace XLScrapApi.Models;

public partial class XLMainItem : PhysicsProp
{
    public Vector3 GetPositionOffsetFromPoints(IList<Vector3> anchors, IList<Vector3> points)
    {
        // Position offset by the average anchor diff
        return transform.position
            + Enumerable.Range(0, anchors.Count())
                .Select(i => points[i] - (transform.position + anchors[i]))
                .Average();
    }

    public void SetPositionWithHolders(Vector3 destVector)
    {
        transform.position = destVector;

        if (HolderItems == null) return;
        var holderPositions = XLPositionUtils.GetHolderPositionsAt(Anchors, destVector);

        for (var i = 0; i < HolderItems.Length; i++)
        {
            HolderItems[i].transform.position = holderPositions[i];
        }
    }

    /* STUB:
     * Implement shift action properly by shifting XlMain and holder by player movement direction
     * Could potentially also involve rotations around other holders
     */
    /*public void Shift(int id, PlayerControllerB player)
    {
        return;

        // Check input action fetch attempt value

        // Could disable lateUpdate of holder items here when pulling?

        // Get player move direction (could fetch from input actions or update)
        // Input action fetch attempt?
        var movement = player.playerActions.Movement.Move.ReadValue<Vector3>();

        // Shift main, holders and holder players in that direction
        var shift = movement.normalized * 0.5f;

        transform.localPosition += shift;
        foreach (var holder in HolderItems)
        {
            holder.transform.localPosition += shift;
            if (holder.playerHeldBy != null)
            {
                holder.playerHeldBy.transform.localPosition += shift;
            }
        }

        // Drain stamina of puller
        player.sprintMeter -= Time.deltaTime * 0.1f;
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void SetOnCounterServerRpc(Vector3 counterPos) => SetOnCounterClientRpc(counterPos);
    [ClientRpc]
    public void SetOnCounterClientRpc(Vector3 counterPos)
    {
        var desk = FindFirstObjectByType<DepositItemsDesk>();
        if (desk != null)
        {
            transform.SetParent(desk.deskObjectsContainer.transform, worldPositionStays: true);
        }
        transform.localPosition = counterPos;
        targetFloorPosition = counterPos;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportXlScrapServerRpc(NetworkObjectReference teleportNetRef, int playerObj)
    {
        if (!teleportNetRef.TryGet(out var teleportNetObj, null)) return;
        var teleport = teleportNetObj.gameObject.GetComponent<EntranceTeleport>();

        IsTeleporting = true;

        // Try teleport and correct to a valid position
        Plugin.Logger.LogDebug($"Attempting to teleport XL Scrap: {transform.position}");

        // Fire exits are bugged in vanilla so we need to flip when entering through these
        var awayFromExit = teleport.exitPoint.forward.normalized;
        if (teleport.isEntranceToBuilding && teleport.entranceId > 0) awayFromExit *= -1;

        var teleportPosition = teleport.exitPoint.position
            + new Vector3(0f, 2f, 0f)
            + (awayFromExit * (Anchors.Max(x => x.magnitude) + 1f));
        var success = XLSpawner.CorrectToValidPosition(this, teleportPosition);

        if (!success)
        {
            IsTeleporting = false;
            Plugin.Logger.LogWarning($"Failed to teleport XL Scrap:");
            HUDManager.Instance.DisplayStatusEffect("Insufficient space detected for XL Scrap on other side");
            return;
        }

        TeleportWithXlScrapClientRpc(
            playerObj,
            teleportNetRef,
            transform.position,
            HolderItems.Select(x => x.transform.position).ToArray()
        );
    }

    [ClientRpc]
    public void TeleportWithXlScrapClientRpc(int playerObj,
        NetworkObjectReference teleportNetRef,
        Vector3 xlMainPos, Vector3[] xlHoldersPos)
    {
        if (!teleportNetRef.TryGet(out var teleportNetObj, null)) return;
        var teleport = teleportNetObj.gameObject.GetComponent<EntranceTeleport>();

        transform.position = xlMainPos;

        if (HolderItems == null || HolderItems.Length < xlHoldersPos.Length) return;

        // Could set IsTeleporting to false if all holders have no parent?
        for (var i = 0; i < HolderItems.Length; i++)
        {
            var holder = HolderItems[i];

            holder.DropOnClient();
            
            holder.transform.position = xlHoldersPos[i];
            holder.startFallingPosition = holder.transform.localPosition;
            holder.FallToGround();
        }

        // Update overrides base player teleportation so do it again
        StartOfRound.Instance.allPlayerScripts[playerObj].TeleportPlayer(
            teleport.exitPoint.transform.position,
            withRotation: true,
            teleport.exitPoint.eulerAngles.y
        );

        IsTeleporting = false;

        if (playerObj != (int)GameNetworkManager.Instance.localPlayerController.playerClientId) return;

        var localPlayer = GameNetworkManager.Instance.localPlayerController;
        localPlayer.isInElevator = false;
        localPlayer.isInHangarShipRoom = false;
        localPlayer.isInsideFactory = teleport.isEntranceToBuilding;
        foreach (var item in localPlayer.ItemSlots)
        {
            if (item == null) continue;
            item.isInFactory = teleport.isEntranceToBuilding;
        }
    }
}
