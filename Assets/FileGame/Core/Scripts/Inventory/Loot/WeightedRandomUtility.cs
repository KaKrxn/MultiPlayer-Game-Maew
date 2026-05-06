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
        if (data == null) return 0;

        int minDurability = Mathf.Min(data.minDurability, data.maxDurability);
        int maxDurability = Mathf.Max(data.minDurability, data.maxDurability);
        float t = Mathf.Pow(Random.value, 1.8f);
        float value = Mathf.Lerp(minDurability, maxDurability, t);

        return Mathf.RoundToInt(Mathf.Clamp(value, minDurability, maxDurability));
    }

    /// <summary>
    /// Calculates a random weight for an item data, biased toward heavier rolls.
    /// </summary>
    public static float CalculateWeight(LootItemData data)
    {
        if (data == null) return 0f;

        float minWeightKg = Mathf.Min(data.minWeightKg, data.maxWeightKg);
        float maxWeightKg = Mathf.Max(data.minWeightKg, data.maxWeightKg);
        float t = 1f - Mathf.Pow(Random.value, 2.4f);
        float value = Mathf.Lerp(minWeightKg, maxWeightKg, t);

        return RoundWeight(Mathf.Clamp(value, minWeightKg, maxWeightKg));
    }

    public static float RoundWeight(float weightKg)
    {
        return Mathf.Round(Mathf.Max(0f, weightKg) * 10f) / 10f;
    }
}
