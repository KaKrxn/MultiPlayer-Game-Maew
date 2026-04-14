using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class WeightedRandomUtility
{
    /// <summary>
    /// Selects a random loot item based on its weight.
    /// </summary>
    public static LootItemData GetRandomItem(List<LootItemData> items)
    {
        if (items == null || items.Count == 0) return null;

        int totalWeight = items.Sum(x => x.dropWeight);
        if (totalWeight <= 0) return null;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeightSum = 0;

        foreach (var item in items)
        {
            currentWeightSum += item.dropWeight;
            if (randomValue < currentWeightSum)
            {
                return item;
            }
        }

        return items[items.Count - 1]; // Fallback
    }

    /// <summary>
    /// Calculates a random durability for an item data.
    /// </summary>
    public static int CalculateDurability(LootItemData data)
    {
        return Random.Range(data.minDurability, data.maxDurability + 1);
    }
}
