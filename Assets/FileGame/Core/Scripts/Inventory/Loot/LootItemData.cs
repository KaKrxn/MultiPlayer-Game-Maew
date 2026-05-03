using UnityEngine;
using Unity.Netcode;

public enum ItemType
{
    Generic,
    Weapon,
    Material,
    Consumable,
    Tool
}

[System.Serializable]
public class LootItemData
{
    public string itemName;
    public ItemType itemType;
    public GameObject itemPrefab; // The GameObject containing NetworkObject and Item component
    
    [Range(0, 100)]
    public int minDurability = 50;
    [Range(0, 100)]
    public int maxDurability = 100;

    [Min(0f)]
    public float minWeightKg = 0.1f;
    [Min(0f)]
    public float maxWeightKg = 1f;
    
    public int dropWeight = 10;
}
