using UnityEngine;

[System.Serializable]
public struct ItemInstanceData
{
    public ItemData itemData;
    [Range(0, 100)] public int durabilityPercent;
    [Min(0f)] public float weightKg;
    [Min(1)] public int stackCount;

    public bool IsValid => itemData != null;
    public float WeightDebuffPercent => Mathf.Max(0f, weightKg * 10f);
    public bool UsesAmountValue => itemData != null && itemData.usesAmountValue;
    public bool IsStackable => itemData != null && itemData.maxStack > 1;
    public bool ShouldShowAmountText => UsesAmountValue || (IsStackable && stackCount > 1);
    public string AmountDisplayText => stackCount.ToString();

    public ItemInstanceData(ItemData itemData, int durabilityPercent, float weightKg, int stackCount = 1)
    {
        this.itemData = itemData;
        this.durabilityPercent = Mathf.Clamp(durabilityPercent, 0, 100);
        this.weightKg = WeightedRandomUtility.RoundWeight(weightKg);
        this.stackCount = Mathf.Max(1, stackCount);
    }

    public bool CanStackWith(ItemInstanceData other)
    {
        if (!IsValid || !other.IsValid || itemData != other.itemData || !IsStackable)
        {
            return false;
        }

        if (UsesAmountValue)
        {
            return true;
        }

        return durabilityPercent == other.durabilityPercent &&
               Mathf.Abs(weightKg - other.weightKg) < 0.001f;
    }
}
