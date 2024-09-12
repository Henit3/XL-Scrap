using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace XlScrapApi;

public class ConfigValues
{
    public ConfigEntry<float> MaxHolderRadius { get; private set; }

    public ConfigValues(ConfigFile configFile)
    {
        MaxHolderRadius = configFile.Bind(
            "Spawn Weights",
            "Max Holder Radius",
            1.2f,
            "Maximum radius that holders can be from their anchor points. May be moved to be item-specific in the future."
        );

        ClearUnusedEntries(configFile);
    }

    private void ClearUnusedEntries(ConfigFile configFile)
    {
        var orphanedPropertyInfo = configFile
            .GetType()
            .GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic);

        var orphanedProperties = (Dictionary<ConfigDefinition, string>)orphanedPropertyInfo.GetValue(configFile, null);
        orphanedProperties.Clear();
        configFile.Save();
    }
}
