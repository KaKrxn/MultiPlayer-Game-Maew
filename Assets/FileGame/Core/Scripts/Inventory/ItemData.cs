using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemPicture;
    [TextArea(2, 4)]
    public string description;

    [Header("Inventory Settings")]
    public int maxStack = 99;

    [Header("Item Type")]
    public ItemType itemType = ItemType.Generic;

    [Header("World Representation")]
    [Tooltip("The actual Prefab dropped into the world")]
    public GameObject dropPrefab;
}
