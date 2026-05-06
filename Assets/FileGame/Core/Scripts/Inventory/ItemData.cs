using UnityEngine;

[System.Serializable]
public enum ItemPromptDisplayType
{
    Generic,
    ScrapMetal
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemPicture;
    [TextArea(2, 4)]
    public string description;
    public ItemType itemType = ItemType.Generic;

    [Header("Inventory Settings")]
    public int maxStack = 99;
    public bool isConsumable = false;
    public bool usesAmountValue = false;
    [Range(0f, 1f)]
    public float toxicChance = 0f;

    [Header("Prompt UI")]
    public ItemPromptDisplayType promptDisplayType = ItemPromptDisplayType.Generic;

    [Header("Item Type")]
    public ItemType itemType = ItemType.Generic;

    [Header("World Representation")]
    [Tooltip("The actual Prefab dropped into the world")]
    public GameObject dropPrefab;
}
