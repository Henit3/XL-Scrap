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
            string configValueForItem = GetConfigValueForItem(extendedItem.Item.itemName);
            if (!string.IsNullOrEmpty(configValueForItem))
            {
                List<StringWithRarity> collection = ConfigParsing(configValueForItem);
                extendedItem.LevelMatchingProperties.levelTags.AddRange(collection);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(collection);
                Logger.LogInfo($"Updated matching properties for {extendedItem.Item}.");
            }
        }
    }

    private static string GetConfigValueForItem(string itemName)
    {
        switch (itemName)
        {
            case "CRT TV":
                return Config.CrtTvSpawnWeight.Value;
            case "L Couch":
                return Config.LCouchSpawnWeight.Value;
            case "XL Holder":
                return null;
			default:
				Logger.LogInfo("No configuration found for item type: " + itemName);
				return null;
		}
	}

	private static List<StringWithRarity> ConfigParsing(string configMoonRarity)
    {
        var list = new List<StringWithRarity>();
        foreach (string item in configMoonRarity?.Split(",") ?? [])
        {
            var array = item.Trim().Split(":");
            if (array.Length == 2)
            {
                var text = array[0];
                if (int.TryParse(array[1], out var result))
                {
                    list.Add(new StringWithRarity(text, result));
                    Logger.LogInfo($"Registered spawn rate for {text} to {result}");
                }
            }
        }
        return list;
    }

    private static void OnLethalBundleLoaded(AssetBundle assetBundle)
    {
        _ = assetBundle == null;
    }
}
