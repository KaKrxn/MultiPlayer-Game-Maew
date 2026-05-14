using UnityEngine;
using Unity.Netcode;

public enum ItemType
{
    Generic,
    Weapon,
    Material,
    Consumable,
    Tool,
    Clothing
}

[System.Serializable]
public class LootItemData
{
    public ItemData itemData;
    public int dropWeight = 10;
}
