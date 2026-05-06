using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private CanvasGroup mainInventoryGroup;

    public GameObject itemPrefab;

    [Header("Input Settings")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode dropKey = KeyCode.G;
    public KeyCode[] quickSlotKeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };

    [Header("UI Slots Setup")]
    public List<InventorySlot> quickSlots = new List<InventorySlot>();
    public List<InventorySlot> mainSlots = new List<InventorySlot>();

    [HideInInspector] public List<InventorySlot> allSlots = new List<InventorySlot>();

    [PurrReadOnly, SerializeField] private InventoryItemData[] _inventoryData;

    public static InventoryManager instance;

    public List<InventoryItemData> inventoryData = new List<InventoryItemData>();
    public int selectedQuickSlotIndex = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InstanceHandler.RegisterInstance(this);

        allSlots.AddRange(quickSlots);
        allSlots.AddRange(mainSlots);

        _inventoryData = new InventoryItemData[allSlots.Count];
    }

    private void Start()
    {
        ToggleInventory(false);
        ToggleCursor(true);
        SelectQuickSlot(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            bool isOpen = mainInventoryGroup.alpha > 0f;
            ToggleInventory(!isOpen);
        }

        for (int i = 0; i < quickSlotKeys.Length; i++)
        {
            if (i < quickSlots.Count && Input.GetKeyDown(quickSlotKeys[i]))
            {
                SelectQuickSlot(i);
            }
        }

        if (Input.GetKeyDown(dropKey))
        {
            DropItemFromIndex(selectedQuickSlotIndex);
        }
    }

    private void SelectQuickSlot(int index)
    {
        if (index < 0 || index >= quickSlots.Count) return;
        selectedQuickSlotIndex = index;

        for (int i = 0; i < quickSlots.Count; i++)
        {
            if (quickSlots[i] != null)
            {
                quickSlots[i].transform.localScale = (i == selectedQuickSlotIndex)
                    ? new Vector3(1.15f, 1.15f, 1.15f)
                    : Vector3.one;
            }
        }
    }

    public void DropItemFromIndex(int index)
    {
        if (index < 0 || index >= _inventoryData.Length) return;

        var data = _inventoryData[index];
        if (data.inventoryItem != null && data.itemInstance.IsValid)
        {
            DropItem(data.inventoryItem);
        }
    }

    private void ToggleInventory(bool toggle)
    {
        if (mainInventoryGroup != null)
        {
            mainInventoryGroup.alpha = toggle ? 1f : 0f;
            mainInventoryGroup.blocksRaycasts = toggle;
            mainInventoryGroup.interactable = toggle;
        }

        ToggleCursor(toggle);
    }

    private void ToggleCursor(bool toggle)
    {
        if (toggle)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        InstanceHandler.UnregisterInstance<InventoryManager>();
    }

    public bool HasFreeSlot()
    {
        if (_inventoryData == null) return false;

        for (int i = 0; i < _inventoryData.Length; i++)
        {
            if (_inventoryData[i].inventoryItem == null)
            {
                return true;
            }
        }

        return false;
    }

    public void AddItem(ItemData itemType)
    {
        if (itemType == null) return;
        AddItem(new ItemInstanceData(itemType, 100, 0.1f));
    }

    public void AddItem(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return;
        AddNewItem(itemInstance);
    }

    private void AddNewItem(ItemInstanceData itemInstance)
    {
        if (allSlots == null || allSlots.Count == 0 || itemPrefab == null) return;

        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null || !slot.IsEmpty) continue;

            var newItemObj = Instantiate(itemPrefab, slot.transform);
            var newItem = newItemObj.GetComponent<InventoryItem>();
            if (newItem == null) return;

            newItem.Init(itemInstance);

            _inventoryData[i] = new InventoryItemData
            {
                itemInstance = itemInstance,
                inventoryItem = newItem,
            };

            slot.SetItem(newItem);
            return;
        }
    }

    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = allSlots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);

        if (oldSlotIndex == -1 || newSlotIndex == -1) return;

        var temp = _inventoryData[newSlotIndex];
        _inventoryData[newSlotIndex] = _inventoryData[oldSlotIndex];
        _inventoryData[oldSlotIndex] = temp;

        allSlots[newSlotIndex].SetItem(_inventoryData[newSlotIndex].inventoryItem);
        allSlots[oldSlotIndex].SetItem(_inventoryData[oldSlotIndex].inventoryItem);

        Debug.Log($"InventoryManager: Moved item {item.gameObject.name} from slot {oldSlotIndex} to {newSlotIndex}");
    }

    public void DropItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;
            if (!data.itemInstance.IsValid || PlayerLocation.localPlayerMovement == null) return;

            if (PlayerDropItem.Instance == null || data.itemInstance.itemData.dropPrefab == null)
            {
                Debug.LogWarning($"InventoryManager: Cannot drop item '{data.itemInstance.itemData.itemName}' because its world prefab is missing.");
                return;
            }

            Vector3 spawnPosition = PlayerLocation.localPlayerMovement.transform.position
                + PlayerLocation.localPlayerMovement.transform.forward
                + Vector3.up;

            PlayerDropItem.Instance.RequestSpawnItemServerRpc(
                data.itemInstance.itemData.itemName,
                data.itemInstance.durabilityPercent,
                data.itemInstance.weightKg,
                spawnPosition,
                Quaternion.identity);

            DeductItem(inventoryItem);
            break;
        }
    }

    private void DeductItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;

            _inventoryData[i] = default;
            allSlots[i].SetItem(null);
            Destroy(inventoryItem.gameObject);
            break;
        }
    }

    public bool ConsumeItem(ItemData targetItem)
    {
        if (targetItem == null) return false;

        for (int i = 0; i < _inventoryData.Length; i++)
        {
            if (_inventoryData[i].itemInstance.itemData == targetItem && _inventoryData[i].inventoryItem != null)
            {
                DeductItem(_inventoryData[i].inventoryItem);
                return true;
            }
        }

        return false;
    }

    public bool TryConsumeJacketDurability(int dmgAmount)
    {
        bool found = false;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.IsValid) continue;
            if (data.itemInstance.itemData.itemType != ItemType.Clothing) continue;

            found = true;
            int newDur = Mathf.Clamp(data.itemInstance.durabilityPercent - dmgAmount, 0, 100);

            var updated = data.itemInstance;
            updated.durabilityPercent = newDur;
            _inventoryData[i] = new InventoryItemData
            {
                itemInstance = updated,
                inventoryItem = data.inventoryItem
            };
            data.inventoryItem.SetInstanceData(updated);

            if (newDur <= 0)
            {
                DeductItem(data.inventoryItem);
                continue;
            }

            break;
        }

        return found;
    }

    public void RefreshItemDurabilityUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventoryData.Length) return;
        _inventoryData[slotIndex].inventoryItem?.RefreshStatDisplay();
    }

    public bool HasJacketInInventory()
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var d = _inventoryData[i];
            if (d.inventoryItem == null || !d.itemInstance.IsValid) continue;
            if (d.itemInstance.itemData.itemType == ItemType.Clothing
                && d.itemInstance.durabilityPercent > 0) return true;
        }

        return false;
    }

    [System.Serializable]
    public struct InventoryItemData
    {
        public ItemInstanceData itemInstance;
        public InventoryItem inventoryItem;
    }
}
