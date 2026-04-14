using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BiomeLootRegistry", menuName = "Loot/Biome Loot Registry")]
public class BiomeLootRegistry : ScriptableObject
{
    [System.Serializable]
    public struct BiomeMapping
    {
        public string biomeName;
        public LootTableData lootTable;
    }

    public List<BiomeMapping> mappings = new List<BiomeMapping>();

    public LootTableData GetLootTableForBiome(string biomeName)
    {
        var mapping = mappings.Find(x => x.biomeName == biomeName);
        return mapping.lootTable;
    }
}
