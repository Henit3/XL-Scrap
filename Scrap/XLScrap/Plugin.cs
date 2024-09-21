using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace XlScrap;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(XLScrapApi.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static new ConfigValues Config;
    internal new static ManualLogSource Logger;
    private static readonly Harmony Harmony = new(PluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} ({PluginInfo.PLUGIN_VERSION}) is loading...");

        Config = new ConfigValues(base.Config);

        AssetBundleLoader.AddOnExtendedModLoadedListener(OnExtendedModRegistered, extendedModModName: "XlScrap");
        AssetBundleLoader.AddOnLethalBundleLoadedListener(OnLethalBundleLoaded, "xl_scrap.lethalbundle");

        NetcodePatcher();

        Logger.LogInfo($"Patching...");
        Harmony.PatchAll();
        Logger.LogInfo($"Patching complete!");

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} ({PluginInfo.PLUGIN_VERSION}) is loaded!");
    }

    private static void NetcodePatcher()
    {
        Type[] types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null).ToArray();
        }
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

    private static void OnExtendedModRegistered(ExtendedMod extendedMod)
    {
        if (extendedMod == null) return;
        Plugin.Logger.LogInfo($"OnExtendedModRegistered");

        foreach (ExtendedItem extendedItem in extendedMod.ExtendedItems)
        {
            if (extendedItem.Item.itemName == "XL Holder") continue;

            var configSpawnWeightForItem = GetConfigSpawnWeightForItem(extendedItem.Item.itemName);
            if (configSpawnWeightForItem != null)
            {
                extendedItem.LevelMatchingProperties.levelTags.AddRange(configSpawnWeightForItem);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(configSpawnWeightForItem);
                Logger.LogInfo($"Updated matching properties for {extendedItem.Item}.");
            }

            var configValueRange = GetConfigValueRangeForItem(extendedItem.Item.itemName);
            if (configValueRange is var (configMin, configMax))
            {
                extendedItem.Item.minValue = (int)(configMin / 0.4);
                extendedItem.Item.maxValue = (int)(configMax / 0.4);
                Logger.LogInfo($"Updated value range properties for {extendedItem.Item}.");
            }
        }
    }

    private static List<StringWithRarity> GetConfigSpawnWeightForItem(string itemName)
    {
        var configSpawnWeight = itemName switch
        {
            "CRT TV" => Config.CrtTvSpawnWeight.Value,
            "Couch" => Config.CouchSpawnWeight.Value,
            "L Couch" => Config.LCouchSpawnWeight.Value,
            _ => null
        };
        if (configSpawnWeight == null)
        {
            Logger.LogWarning("No spawn weight configuration found for item type: " + itemName);
            return null;
        }

        var list = new List<StringWithRarity>();
        foreach (string item in configSpawnWeight.Split(','))
        {
            var array = item.Trim().Split(':');
            if (array.Length != 2) continue;

            if (!int.TryParse(array[1], out var result)) continue;
            var text = array[0];

            list.Add(new(text, result));
            Logger.LogInfo($"Registered spawn rate for {text} to {result}");
        }
        return list;
	}

    private static (int Min, int Max)? GetConfigValueRangeForItem(string itemName)
    {
        var configValueRange = itemName switch
        {
            "CRT TV" => Config.CrtTvValueRange.Value,
            "Couch" => Config.CouchValueRange.Value,
            "L Couch" => Config.LCouchValueRange.Value,
            _ => null
        };
        if (configValueRange == null)
        {
            Logger.LogWarning("No value range configuration found for item type: " + itemName);
            return null;
        }

        var values = configValueRange.Split('-');
        if (values.Length != 2
            || !int.TryParse(values[0].Trim(), out var min)
            || !int.TryParse(values[1].Trim(), out var max)
            || min > max)
        {
            return null;
        }

        return (min, max);
	}

    private static void OnLethalBundleLoaded(AssetBundle assetBundle)
    {
        _ = assetBundle == null;
    }
}
