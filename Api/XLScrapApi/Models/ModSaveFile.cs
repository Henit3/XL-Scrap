using LethalModDataLib.Attributes;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace XLScrapApi.Models;

public class ModSaveFile : ModDataContainer
{
    [ModDataIgnore(IgnoreFlags.None)]
    public static ModSaveFile Instance;

    [ModDataIgnore(IgnoreFlags.None)]
    public HashSet<int> SpawnedKeys;
    public Dictionary<Vector3, List<(Vector3, Vector3)>> XlHolderLocations;

    public static void Init()
    {
        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) return;

        Instance = new();
        Instance.LoadXlScrap();
    }

    public ModSaveFile() : base()
    {
        XlHolderLocations = [];
        SpawnedKeys = [];
    }

    public void Reset()
    {
        XlHolderLocations = [];
        SpawnedKeys = [];
    }

    public void SaveXlScrap(IList<XLMainItem> xlMains)
    {
        XlHolderLocations = xlMains
            .Where(m => m.HolderItems?.Any() ?? false)
            .ToDictionary(
                m => m.transform.position,
                m => m.HolderItems.Select(h => (h.transform.position, h.transform.localPosition)).ToList()
            );

        Save();
    }

    public void LoadXlScrap(bool force = false)
    {
        if (!force && (XlHolderLocations?.Any() ?? false)) return;

        Load();
    }
}
