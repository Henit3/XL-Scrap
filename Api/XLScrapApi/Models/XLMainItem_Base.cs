using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using XLScrapApi.Util;

namespace XLScrapApi.Models;

[DefaultExecutionOrder(ExecutionOrder)]
public partial class XLMainItem : PhysicsProp
{
    // Ensures that the order of Update() calls are uniform between an XlMain item in relation to its holders
    public const int ExecutionOrder = 100;

    private static Item _holderItemPrefab;
    private static Item HolderItemPrefab
    {
        get
        {
            if (_holderItemPrefab != null) return _holderItemPrefab;

            _holderItemPrefab = StartOfRound.Instance.allItemsList.itemsList
                .FirstOrDefault(i => i.spawnPrefab != null
                    && i.spawnPrefab.GetComponent<XLHolderItem>() != null);

            return _holderItemPrefab;
        }
    }
    
    private NetworkList<NetworkObjectReference> holderNetRefs = new();
    public XLHolderItem[] HolderItems { get; set; }

    // Unsure if this actually makes a difference
    public bool IsTeleporting { get; private set; }

    public Vector3 PositionOffset;
    public Vector3[] Anchors;

    private bool[] groundedInShip;
    private bool first = true;

    public override void Start()
    {
        base.Start();

        EnsureRequiredXlPropsSet();

        // Ignore Start() being called before its network object is spawned (e.g. when spawned by Imperium)
        if (!IsSpawned) return;

        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        {
            // Sync on initial connection for pre-existing items (i.e. when in orbit)
            if (HolderItems != null) InitialiseHoldersClient();
            return;
        }

        if (LoadHoldersFromSaveServer()) return;

        // Create holders if they cannot be loaded using save data
        InitialiseHoldersServer();
    }

    public Vector3 GetAnchor(int id) => transform.position - PositionOffset + Anchors[id];

    private void EnsureRequiredXlPropsSet()
    {
        grabbable = false;
        grabbableToEnemies = false;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        if (!Plugin.Config.XlItemCollision.Value) SetCollision(false);
    }

    private void SetCollision(bool value)
    {
        var colliders = gameObject.GetComponentsInChildren<Collider>();
        if (colliders == null) return;
        foreach (var collider in colliders) collider.enabled = value;
    }

    private void InitialiseHoldersServer()
    {
        for (var itemId = 0; itemId < Anchors.Length; itemId++)
        {
            var holderObj = Object.Instantiate(
                HolderItemPrefab.spawnPrefab,
                transform.position - PositionOffset + Anchors[itemId],
                Quaternion.identity,
                StartOfRound.Instance.propsContainer
            );
            
            var holderNetObj = holderObj.GetComponent<NetworkObject>();
            holderNetObj.Spawn(false);
            holderNetRefs.Add(holderNetObj);
        }

        InitialiseHoldersClientRpc();
    }

    private bool LoadHoldersFromSaveServer()
    {
        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) return false;

        if (!StartOfRound.Instance.inShipPhase)
        {
            Plugin.Logger.LogDebug("LoadHoldersFromSaveServer called from outside orbit");
            return false;
        }

        if (!(ModSaveFile.Instance?.XlHolderLocations?.Any() ?? false))
        {
            Plugin.Logger.LogDebug("LoadHoldersFromSaveServer called with empty save data");
            return false;
        }

        // Use the closest holder that hasn't already been used
        var index = -1;
        var closestIndex = -1;
        Vector3? closestMainPos = null;
        var closestMainPosDist = 0f;
        foreach (var (mainPos, _) in ModSaveFile.Instance.XlHolderLocations)
        {
            index++;
            if (ModSaveFile.Instance.SpawnedKeys.Contains(index)) continue;

            var distance = Vector3.Distance(transform.position - PositionOffset, mainPos);
            if (closestMainPos == null || distance < closestMainPosDist)
            {
                closestMainPos = mainPos;
                closestMainPosDist = distance;
                closestIndex = index;
            }
        }
        if (closestMainPos == null || closestIndex == -1)
        {
            Plugin.Logger.LogDebug("LoadHoldersFromSaveServer couldn't find matching info in save data");
            return false;
        }
        ModSaveFile.Instance.SpawnedKeys.Add(closestIndex);

        var holderPositions = ModSaveFile.Instance.XlHolderLocations[closestMainPos.Value].ToArray();

        // Assign any loaded but unmappped holders the positions in the value
        var potentialHolders = FindObjectsOfType<XLHolderItem>()
            .Where(x => x.MainItem == null && !x.MainItemAssigned)
            .ToList();
        if (potentialHolders.Count < Anchors.Length)
        {
            Plugin.Logger.LogWarning("LoadHoldersFromSaveServer couldn't find enough holders to load in a XL main item");
            ModSaveFile.Instance.SpawnedKeys.Remove(closestIndex);
            return false;
        }

        // Order the potentialHolders to match with holderPositions
        var holders = new List<XLHolderItem>();
        foreach (var holderPosition in holderPositions)
        {
            var matchingHolder = potentialHolders.Aggregate((h1, h2) =>
                Vector3.Distance(holderPosition.Item1, h1.transform.position)
                < Vector3.Distance(holderPosition.Item1, h2.transform.position)
                    ? h1 : h2);
            potentialHolders.Remove(matchingHolder);
            holders.Add(matchingHolder);
            matchingHolder.MainItemAssigned = true;
        }

        holderNetRefs.Clear();
        foreach (var holderItem in holders)
        {
            holderNetRefs.Add(holderItem.GetComponent<NetworkObject>());
        }

        // Technically don't need RPC here but will do for the sake of late join mods
        InitialiseHoldersClientRpc();
        return true;
    }

    [ClientRpc]
    private void InitialiseHoldersClientRpc() => InitialiseHoldersClient();
    private void InitialiseHoldersClient()
    {
        if (!IsSpawned) return;
        if (holderNetRefs == null || holderNetRefs.Count == 0) return;

        HolderItems = new XLHolderItem[holderNetRefs.Count];
        groundedInShip = new bool[holderNetRefs.Count];

        for (var itemId = 0; itemId < holderNetRefs.Count; itemId++)
        {
            var holderNetRef = holderNetRefs[itemId];
            if (!holderNetRef.TryGet(out var holderNetObj, null)) continue;

            var holderItem = holderNetObj.gameObject.GetComponent<XLHolderItem>();
            holderItem.SetMainItem(this);
            holderItem.Id = itemId;
            HolderItems[itemId] = holderItem;
        }

        transform.position = XLPositionUtils.GetPositionFromHolders(Anchors, HolderItems.Select(x => x.transform.position).ToArray())
            + PositionOffset;
        var anchorCorrection = Quaternion.FromToRotation(Anchors[0], HolderItems[0].transform.position - transform.position - PositionOffset);
        RotateAnchors(anchorCorrection);
        var rotation = GetAnchorRotationOffset();
        RotateBase(rotation);
    }

    public override void Update()
    {
        base.Update();

        if (HolderItems == null || HolderItems.Length == 0)
        {
            if (holderNetRefs.Count == 0
                || (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) return;

            // It doesn't seem to sync with host on natural level generation?
            InitialiseHoldersClient();
        }
        if (HolderItems.Any(x => x == null)) return;

        if (IsTeleporting) return;

        transform.position = GetPositionOffsetFromPoints(Anchors, HolderItems.Select(x => x.transform.position).ToArray())
            + PositionOffset;
        var rotation = GetAnchorRotationOffset();
        RotateBase(rotation);
        // Janky physics occur when anchors and holders mismatch; transform.forward is reset on first update so need this
        if (first)
        {
            rotation = GetAnchorRotationOffset();
            first = false;
        }
        RotateAnchors(rotation);
    }

    public void HolderOnShipGround(XLHolderItem holder, bool value)
    {
        groundedInShip[holder.Id] = value;
        if (groundedInShip.Any(x => !x)) return;

        // If all holders for the associated XL Main are not held and are in ship, register main as in ship
        //OnBroughtToShip();
        var player = holder.playerHeldBy;
        player.SetItemInElevator(player.isInHangarShipRoom, player.isInElevator, this);
    }

    // Rotation sets forward to the first holder item (others should be restricted accordingly using anchors)
    private Quaternion GetAnchorRotationOffset()
    {
        var holderPos = HolderItems[0].transform.position;
        var vHolderMain = transform.position - PositionOffset - holderPos;
        return Quaternion.FromToRotation(transform.forward, vHolderMain);
    }

    private void RotateBase(Quaternion rotation)
    {
        transform.forward = rotation * transform.forward;
    }

    private void RotateAnchors(Quaternion rotation)
    {
        for (var i = 0; i < Anchors.Length; i++)
        {
            Anchors[i] = rotation * Anchors[i];
        }
    }
}
