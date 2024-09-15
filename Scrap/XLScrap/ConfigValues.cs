using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace XlScrap;

public class ConfigValues
{
    public ConfigEntry<string> CrtTvSpawnWeight { get; private set; }
    public ConfigEntry<string> CouchSpawnWeight { get; private set; }
    public ConfigEntry<string> LCouchSpawnWeight { get; private set; }

    public ConfigValues(ConfigFile configFile)
    {
        CrtTvSpawnWeight = configFile.Bind(
            "Spawn Weights",
            "CRT TV",
            "Vanilla:50, Custom:50",
            "Spawn Weight of CRT TV specified as comma-separated \"key:weight\" pairs"
        );

        CouchSpawnWeight = configFile.Bind(
            "Spawn Weights",
            "Couch",
            "Vanilla:50, Custom:50",
            "Spawn Weight of Couch specified as comma-separated \"key:weight\" pairs"
        );

        LCouchSpawnWeight = configFile.Bind(
            "Spawn Weights",
            "L Couch",
            "Vanilla:0, Custom:0",
            "Spawn Weight of L Couch (UNFINISHED) specified as comma-separated \"key:weight\" pairs"
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
