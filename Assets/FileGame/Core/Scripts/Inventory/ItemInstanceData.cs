using UnityEngine;

[System.Serializable]
public struct ItemInstanceData
{
    public ItemData itemData;
    [Range(0, 100)] public int durabilityPercent;
    [Min(0f)] public float weightKg;

    public bool IsValid => itemData != null;
    public float WeightDebuffPercent => Mathf.Max(0f, weightKg * 10f);

    public ItemInstanceData(ItemData itemData, int durabilityPercent, float weightKg)
    {
        this.itemData = itemData;
        this.durabilityPercent = Mathf.Clamp(durabilityPercent, 0, 100);
        this.weightKg = WeightedRandomUtility.RoundWeight(weightKg);
    }
}
