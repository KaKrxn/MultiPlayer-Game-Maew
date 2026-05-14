using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum ItemSizeRarity
{
    Small,
    Medium,
    Big,
    SuperHuge
}

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

    public static ItemSizeRarity RollSizeRarity()
    {
        float roll = Random.value;
        if (roll < 0.60f) return ItemSizeRarity.Small;       // 60%
        if (roll < 0.85f) return ItemSizeRarity.Medium;      // 25%
        if (roll < 0.95f) return ItemSizeRarity.Big;         // 10%
        return ItemSizeRarity.SuperHuge;                     // 5%
    }

    public static int CalculateDurability(ItemSizeRarity rarity)
    {
        return rarity switch
        {
            ItemSizeRarity.Small => Random.Range(10, 41),
            ItemSizeRarity.Medium => Random.Range(40, 71),
            ItemSizeRarity.Big => Random.Range(70, 91),
            ItemSizeRarity.SuperHuge => Random.Range(90, 101),
            _ => 100
        };
    }

    public static float CalculateWeight(ItemSizeRarity rarity)
    {
        float weight = rarity switch
        {
            ItemSizeRarity.Small => Random.Range(0.1f, 0.5f),
            ItemSizeRarity.Medium => Random.Range(0.5f, 1.5f),
            ItemSizeRarity.Big => Random.Range(1.5f, 3.0f),
            ItemSizeRarity.SuperHuge => Random.Range(3.0f, 5.0f),
            _ => 1.0f
        };
        return RoundWeight(weight);
    }

    public static ItemSizeRarity GetRarityFromWeight(float weightKg)
    {
        if (weightKg < 0.5f) return ItemSizeRarity.Small;
        if (weightKg < 1.5f) return ItemSizeRarity.Medium;
        if (weightKg < 3.0f) return ItemSizeRarity.Big;
        return ItemSizeRarity.SuperHuge;
    }

    public static float CalculateDynamicScale(float weightKg)
    {
        ItemSizeRarity inferredRarity = GetRarityFromWeight(weightKg);
        float baseScale = GetScaleMultiplier(inferredRarity);
        return baseScale * (1.0f + (weightKg * 0.5f));
    }
    
    public static float GetScaleMultiplier(ItemSizeRarity rarity)
    {
        return rarity switch
        {
            ItemSizeRarity.Small => 1.0f,
            ItemSizeRarity.Medium => 1.5f,
            ItemSizeRarity.Big => 2.0f,
            ItemSizeRarity.SuperHuge => 3.0f,
            _ => 1.0f
        };
    }

    public static float RoundWeight(float weightKg)
    {
        return Mathf.Round(Mathf.Max(0f, weightKg) * 10f) / 10f;
    }
}
