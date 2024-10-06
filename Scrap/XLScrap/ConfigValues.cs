using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace XlScrap;

public class ConfigValues
{
    public ConfigEntry<string> CrtTvSpawnWeight { get; private set; }
    public ConfigEntry<string> CrtTvValueRange { get; private set; }
    public ConfigEntry<string> CouchSpawnWeight { get; private set; }
    public ConfigEntry<string> CouchValueRange { get; private set; }
    public ConfigEntry<string> CoffinSpawnWeight { get; private set; }
    public ConfigEntry<string> CoffinValueRange { get; private set; }
    public ConfigEntry<string> LCouchSpawnWeight { get; private set; }
    public ConfigEntry<string> LCouchValueRange { get; private set; }
    public ConfigEntry<bool> SensibleSpawns { get; private set; }

    public ConfigValues(ConfigFile configFile)
    {
        BindSpawnWeights(configFile);
        BindScrapValues(configFile);
        BindMisc(configFile);

        ClearUnusedEntries(configFile);
    }

    private void BindSpawnWeights(ConfigFile configFile)
    {
        const string Category = "Spawn Weights";

        CrtTvSpawnWeight = configFile.Bind(Category,
            "CRT TV",
            "Vanilla:50, Custom:50",
            "Spawn Weight of CRT TV specified as comma-separated \"key:weight\" pairs"
        );

        CouchSpawnWeight = configFile.Bind(Category,
            "Couch",
            "Vanilla:50, Custom:50",
            "Spawn Weight of Couch specified as comma-separated \"key:weight\" pairs"
        );

        CoffinSpawnWeight = configFile.Bind(Category,
            "Coffin",
            "Vanilla:0, Custom:0",
            "Spawn Weight of Coffin specified as comma-separated \"key:weight\" pairs"
        );

        LCouchSpawnWeight = configFile.Bind(Category,
            "L Couch",
            "Vanilla:0, Custom:0",
            "Spawn Weight of L Couch (UNFINISHED) specified as comma-separated \"key:weight\" pairs"
        );
    }
    
    private void BindScrapValues(ConfigFile configFile)
    {
        const string Category = "Scrap Values";

        CrtTvValueRange = configFile.Bind(Category,
            "CRT TV",
            "160-240",
            "Scrap Value of CRT TV specified as a \"min-max\" range"
        );

        CouchValueRange = configFile.Bind(Category,
            "Couch",
            "180-260",
            "Scrap Value of Couch specified as a \"min-max\" range"
        );

        CoffinValueRange = configFile.Bind(Category,
            "Coffin",
            "280-360",
            "Scrap Value of Coffin specified as a \"min-max\" range"
        );

        LCouchValueRange = configFile.Bind(Category,
            "L Couch",
            "280-360",
            "Scrap Value of L Couch (UNFINISHED) specified as a \"min-max\" range"
        );
    }
    
    private void BindMisc(ConfigFile configFile)
    {
        const string Category = "Miscellaneous";

        SensibleSpawns = configFile.Bind(Category,
            "Sensible Spawn Positions",
            true,
            "Whether these items should make use of two handed spawn positions"
        );
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
