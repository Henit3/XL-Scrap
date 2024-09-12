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

    private void ShiftPositionWithHolders(Vector3 shiftVector)
    {
        transform.position += shiftVector;
        if (HolderItems == null) return;

        foreach (var holder in HolderItems)
        {
            holder.transform.position += shiftVector;
        }
    }

    public void SetPositionWithHolders(Vector3 destVector)
    {
        transform.position = destVector;
        if (HolderItems == null) return;

        for (var i = 0; i < HolderItems.Length; i++)
        {
            HolderItems[i].transform.position = transform.position + Anchors[i];
        }
    }

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

    public Vector3 GetPositionFromHolders(IList<Vector3> holdersPos)
    {
        return Enumerable.Range(0, holdersPos.Count)
            .Select(i => holdersPos[i] - Anchors[i])
            .Average();
    }

    // If we fail, we don't teleport the player since we stop this
    // We may want to teleport them still in this case (use an RPC to sync across all clients)
    public void TeleportXlScrapServer(EntranceTeleport teleport, int playerObj)
    {
        IsTeleporting = true;

        // Try teleport and correct to a valid position
        var originalPosition = transform.position;
        SetPositionWithHolders(teleport.exitPoint.transform.position
            + new Vector3(0f, 2f, 0f)
            + (teleport.exitPoint.transform.forward.normalized * Anchors.Max(x => x.magnitude)));
        var success = CorrectToValidPosition();

        if (!success)
        {
            // If unable to teleport the item, reset back to the original location
            SetPositionWithHolders(originalPosition);
            IsTeleporting = false;
            return;
        }

        TeleportWithXlScrapClientRpc(
            playerObj,
            teleport.exitPoint.transform.position,
            teleport.exitPoint.eulerAngles.y,
            transform.position,
            HolderItems.Select(x => x.transform.position).ToArray()
        );
    }

    [ClientRpc]
    public void TeleportWithXlScrapClientRpc(int playerObj,
        Vector3 teleportPos, float teleportRot,
        Vector3 xlMainPos, Vector3[] xlHoldersPos)
    {
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
        StartOfRound.Instance.allPlayerScripts[playerObj]
            .TeleportPlayer(teleportPos, withRotation: true, teleportRot);

        IsTeleporting = false;
    }
}
