using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace XLScrapApi.Models;

[DefaultExecutionOrder(XLMainItem.ExecutionOrder + 1)]
public class XLHolderItem : PhysicsProp
{
    public const float MaxRadius = 1.2f;

    public int Id { get; set; }
    public bool MainItemAssigned { get; set; }
    public XLMainItem MainItem { get; private set; }

    public Vector3 GetAnchor() => MainItem.GetAnchor(Id);

    public void SetMainItem(XLMainItem newMainItem)
    {
        MainItem = newMainItem;
        if (MainItem == null) return;
        MainItemAssigned = true;

        // Due to weight calculation: actual weight = (weight - 1) * 105lb
        itemProperties.weight = 1 + ((MainItem.itemProperties.weight - 1) / MainItem.HolderItems.Length);
        customGrabTooltip = $"Hold {MainItem.itemProperties.itemName}";
    }

    /* STUB:
     * If grabbed and activated by a player
     * Push or pull in the direction of the holding player's movement (default to nothing?)
     *   Maybe set a flag and use this on player movement as part of the update
     */
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        return;
        
        if (MainItem == null) return;
        //MainItem.Shift(Id, playerHeldBy);
    }

    public override void Update()
    {
        base.Update();

        // Null ref hit here on clients with already spawned XlMain items
        if (MainItem == null
            || MainItem.HolderItems == null
            || !MainItem.HolderItems.Any()
            || MainItem.HolderItems.Any(x => x == null))
        {
            return;
        }

        var vAnchor = GetAnchor();
        var vAnchorHolder = transform.position - vAnchor;

        if (playerHeldBy == null) return;

        var vHolderPlayer = playerHeldBy.transform.position - transform.position;
        var vHolderPlayerServer = playerHeldBy.serverPlayerPosition - transform.position;
        if (vAnchorHolder.magnitude > Plugin.Config.MaxHolderRadius.Value)
        {
            transform.position = vAnchor + (vAnchorHolder.normalized * Plugin.Config.MaxHolderRadius.Value);

            // Restrict player movement by moving them back into the circle
            playerHeldBy.transform.position = transform.position + vHolderPlayer;
            playerHeldBy.serverPlayerPosition = transform.position + vHolderPlayerServer;
        }
    }

    public override void OnBroughtToShip()
    {
        base.OnBroughtToShip();

        if (MainItem == null) return;
        MainItem.HolderOnShipGround(this, true);
    }

    public override void OnDestroy()
    {
        DropOnClient(isDeleted: true);

        base.OnDestroy();
    }

    public override void EquipItem()
    {
        base.EquipItem();

        if (MainItem == null || MainItem.hasBeenHeld) return;

        MainItem.hasBeenHeld = true;
        if (!isInShipRoom
            && !StartOfRound.Instance.inShipPhase
            && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
        {
            RoundManager.Instance.valueOfFoundScrapItems += MainItem.scrapValue;
        }
    }

    public override void GrabItem()
    {
        base.GrabItem();

        if (MainItem == null) return;
        MainItem.HolderOnShipGround(this, false);
    }

    public void DropOnClient(bool isDeleted = false)
    {
        if (playerHeldBy == null) return;

        AccessTools.Method(typeof(PlayerControllerB), "SetSpecialGrabAnimationBool")
            .Invoke(playerHeldBy, [false, this]);
        playerHeldBy.playerBodyAnimator.SetBool("cancelHolding", value: true);
        playerHeldBy.playerBodyAnimator.SetTrigger("Throw");

        if (playerHeldBy == StartOfRound.Instance.localPlayerController)
        {
            HUDManager.Instance.itemSlotIcons[playerHeldBy.currentItemSlot].enabled = false;
            HUDManager.Instance.holdingTwoHandedItem.enabled = false;
        }

        var floorYRot2 = (int)playerHeldBy.transform.localEulerAngles.y;
        // If deleted, we cannot reference this object itseld
        if (this != null && !isDeleted)
        {
            playerHeldBy.SetObjectAsNoLongerHeld(false, false, targetFloorPosition, this, floorYRot2);
        }
        else
        {
            playerHeldBy.twoHanded = false;
            playerHeldBy.twoHandedAnimation = false;
            playerHeldBy.carryWeight = Mathf.Clamp(playerHeldBy.carryWeight - (itemProperties.weight - 1f), 1f, 10f);
            playerHeldBy.isHoldingObject = false;
            //playerHeldBy.hasThrownObject = true;
            Traverse.Create(playerHeldBy).Field("hasThrownObject").SetValue(true);
        }
        playerHeldBy.currentlyHeldObjectServer = null;

        if (base.IsOwner) DiscardItem();
    }
}
