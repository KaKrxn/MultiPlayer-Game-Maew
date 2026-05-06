using UnityEngine;
using Unity.Collections;
using FileGame.Core;
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

    /// <summary>
    /// Fired when the selected quick slot changes. Args: slotIndex, itemData at that slot.
    /// </summary>
    public static event Action<int, InventoryItemData> OnSelectedQuickSlotChanged;

    /// <summary>
    /// Reference to the network handler on the local player.
    /// </summary>
    private InventoryNetworkHandler _networkHandler;

    /// <summary>
    /// True if inventory UI is currently open.
    /// </summary>
    public bool IsOpen => mainInventoryGroup != null && mainInventoryGroup.alpha > 0f;

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
        // Lazy-find the network handler if not yet found
        if (_networkHandler == null)
        {
            if (PlayerLocation.localPlayerMovement != null)
            {
                _networkHandler = PlayerLocation.localPlayerMovement.GetComponent<InventoryNetworkHandler>();
            }
        }

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
                    ? new Vector3(GameConstants.SelectedSlotScale, GameConstants.SelectedSlotScale, GameConstants.SelectedSlotScale)
                    : Vector3.one;
            }
        }

        // Fire event so other systems (ItemDescriptionPanel, etc.) can react
        OnSelectedQuickSlotChanged?.Invoke(index, GetQuickSlotItemData(index));
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
            
            // Explicitly refresh the 3D character view since OnEnable/OnDisable 
            // won't fire when toggling alpha
            var modelView = mainInventoryGroup.GetComponentInChildren<FileGame.Core.UI.CharacterModelView>(true);
            if (modelView != null)
            {
                modelView.Refresh(toggle);
            }
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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        InstanceHandler.UnregisterInstance<InventoryManager>();
    }

    // ── Server-Delegated Public Methods ─────────────────────────

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

    public bool CanAcceptItem(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid)
        {
            return false;
        }

        if (itemInstance.IsStackable && GetAvailableStackSpace(itemInstance) >= itemInstance.stackCount)
        {
            return true;
        }

        return HasFreeSlot();
    }

    public void AddItem(ItemData itemType)
    {
        if (itemType == null) return;
        float defaultWeight = itemType.usesAmountValue ? 0f : 0.1f;
        AddItem(new ItemInstanceData(itemType, 100, defaultWeight, 1));
    }

    /// <summary>
    /// Request to add an item. Routes through server if network handler is available.
    /// </summary>
    public void AddItem(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return;

        // Server-authoritative path
        if (_networkHandler != null)
        {
            _networkHandler.RequestAddItemServerRpc(
                new FixedString32Bytes(itemInstance.itemData.itemName),
                itemInstance.durabilityPercent,
                itemInstance.weightKg,
                itemInstance.stackCount);
            return;
        }

        // Fallback: local-only (editor testing without network)
        AddItemLocal(itemInstance);
    }

    /// <summary>
    /// Local-only add for fallback/offline mode.
    /// </summary>
    private void AddItemLocal(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return;
        int remaining = itemInstance.stackCount;

        if (itemInstance.IsStackable)
        {
            remaining = StackIntoExistingSlots(itemInstance, remaining);
        }

        while (remaining > 0)
        {
            int amountToAdd = itemInstance.IsStackable
                ? Mathf.Min(remaining, itemInstance.itemData.maxStack)
                : 1;

            ItemInstanceData itemToInsert = new ItemInstanceData(
                itemInstance.itemData,
                itemInstance.durabilityPercent,
                itemInstance.weightKg,
                amountToAdd);

            if (!AddNewItem(itemToInsert))
            {
                Debug.LogWarning($"InventoryManager: No room left to add '{itemInstance.itemData.itemName}'.");
                return;
            }

            remaining -= amountToAdd;
        }
    }

    private bool AddNewItem(ItemInstanceData itemInstance)
    {
        if (allSlots == null || allSlots.Count == 0 || itemPrefab == null) return false;

        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot == null || !slot.IsEmpty) continue;

            var newItem = slot.GetComponentInChildren<InventoryItem>();
            if (newItem == null)
            {
                var newItemObj = Instantiate(itemPrefab, slot.transform);
                newItem = newItemObj.GetComponent<InventoryItem>();
            }

            if (newItem == null) return false;

            newItem.Init(itemInstance);

            _inventoryData[i] = new InventoryItemData
            {
                itemInstance = itemInstance,
                inventoryItem = newItem,
            };

            slot.SetItem(newItem);

            // Fire event if this slot is the selected quick slot
            if (i == selectedQuickSlotIndex && i < quickSlots.Count)
            {
                OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, _inventoryData[i]);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when an item is drag-dropped to a new slot in the UI.
    /// Routes through server for validation.
    /// </summary>
    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = allSlots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);

        if (oldSlotIndex == -1 || newSlotIndex == -1) return;
        if (oldSlotIndex == newSlotIndex) return; // dropped on same slot, no-op

        // Server-authoritative path: send RPC only, let ApplyServerSlotUpdate handle the UI.
        // Do NOT do optimistic local swap — it causes double-application when the server
        // callback fires ApplyServerSlotUpdate on top of already-swapped data.
        if (_networkHandler != null)
        {
            _networkHandler.RequestMoveItemServerRpc(oldSlotIndex, newSlotIndex);
            return;
        }

        // Fallback: local swap (offline/editor testing)
        item.MarkAsMoved();
        SwapSlotsLocal(oldSlotIndex, newSlotIndex);
    }

    private void SwapSlotsLocal(int oldSlotIndex, int newSlotIndex)
    {
        var temp = _inventoryData[newSlotIndex];
        _inventoryData[newSlotIndex] = _inventoryData[oldSlotIndex];
        _inventoryData[oldSlotIndex] = temp;

        // Clear both slots before reassignment to prevent stale references
        allSlots[oldSlotIndex].ClearItem();
        allSlots[newSlotIndex].ClearItem();

        allSlots[newSlotIndex].SetItem(_inventoryData[newSlotIndex].inventoryItem);
        allSlots[oldSlotIndex].SetItem(_inventoryData[oldSlotIndex].inventoryItem);

        // Fire event if quick slot was affected
        if (newSlotIndex == selectedQuickSlotIndex || oldSlotIndex == selectedQuickSlotIndex)
        {
            OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, GetQuickSlotItemData(selectedQuickSlotIndex));
        }
    }

    /// <summary>
    /// Drop an item from inventory. Routes through server.
    /// </summary>
    public void DropItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;
            if (!data.itemInstance.IsValid || PlayerLocation.localPlayerMovement == null) return;

            // Server-authoritative path
            if (_networkHandler != null)
            {
                _networkHandler.RequestDropItemServerRpc(i);
                return;
            }

            // Fallback: local drop
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
                data.itemInstance.stackCount,
                spawnPosition,
                Quaternion.identity);

            RemoveAmountFromIndex(i, data.itemInstance.stackCount);
            break;
        }
    }

    private void DeductItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;

            RemoveAmountFromIndex(i, data.itemInstance.stackCount);
            break;
        }
    }

    // ── Query API ──────────────────────────────────────────────

    public InventoryItemData GetQuickSlotItemData(int index)
    {
        if (index < 0 || index >= quickSlots.Count || index >= _inventoryData.Length) return default;
        return _inventoryData[index];
    }

    /// <summary>
    /// Convenience: get the currently selected quick slot item data.
    /// </summary>
    public InventoryItemData GetSelectedQuickSlotItem()
    {
        return GetQuickSlotItemData(selectedQuickSlotIndex);
    }

    /// <summary>
    /// Consume the current quick slot item (1 from stack). Routes through server.
    /// </summary>
    public void ConsumeCurrentQuickSlotItem()
    {
        int index = selectedQuickSlotIndex;
        if (index < 0 || index >= _inventoryData.Length || index >= quickSlots.Count) return;
        var data = _inventoryData[index];
        if (data.inventoryItem == null) return;

        // Server-authoritative path
        if (_networkHandler != null)
        {
            _networkHandler.RequestConsumeItemServerRpc(index, 1);
            return;
        }

        // Fallback: local
        RemoveAmountFromIndex(index, 1);
    }

    public bool ConsumeItem(ItemData targetItem)
    {
        return ConsumeItemAmount(targetItem, 1);
    }

    public int GetTotalItemCount(ItemData targetItem)
    {
        if (targetItem == null) return 0;

        int total = 0;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            if (_inventoryData[i].inventoryItem == null || _inventoryData[i].itemInstance.itemData != targetItem)
            {
                continue;
            }

            total += Mathf.Max(1, _inventoryData[i].itemInstance.stackCount);
        }

        return total;
    }

    public bool HasItemAmount(ItemData targetItem, int amount)
    {
        return amount > 0 && GetTotalItemCount(targetItem) >= amount;
    }

    public bool ConsumeItemAmount(ItemData targetItem, int amount)
    {
        if (targetItem == null || amount <= 0 || !HasItemAmount(targetItem, amount))
        {
            return false;
        }

        // Server-authoritative path
        if (_networkHandler != null)
        {
            _networkHandler.RequestConsumeItemByNameServerRpc(
                new FixedString32Bytes(targetItem.itemName), amount);
            return true;
        }

        // Fallback: local
        int remaining = amount;
        for (int i = 0; i < _inventoryData.Length && remaining > 0; i++)
        {
            if (_inventoryData[i].inventoryItem == null || _inventoryData[i].itemInstance.itemData != targetItem)
            {
                continue;
            }

            int removeAmount = Mathf.Min(remaining, Mathf.Max(1, _inventoryData[i].itemInstance.stackCount));
            RemoveAmountFromIndex(i, removeAmount);
            remaining -= removeAmount;
        }

        return remaining <= 0;
    }

    // ── Server Sync ──────────────────────────────────────────────

    /// <summary>
    /// Called by InventoryNetworkHandler when the server updates a slot.
    /// Applies the change to the local UI.
    /// </summary>
    public void ApplyServerSlotUpdate(int index, NetworkInventorySlotData serverData)
    {
        if (index < 0 || index >= _inventoryData.Length || index >= allSlots.Count) return;

        if (serverData.isEmpty)
        {
            // Server says slot is empty — clear local UI
            ClearSlot(index);
        }
        else
        {
            // Resolve item data from name
            string itemName = serverData.itemName.ToString();
            ItemData itemData = ResolveItemDataByName(itemName);
            if (itemData == null)
            {
                Debug.LogWarning($"[Inventory] Client: Could not resolve ItemData for '{itemName}'");
                return;
            }

            ItemInstanceData newInstance = new ItemInstanceData(
                itemData, serverData.durability, serverData.weight, serverData.stackCount);

            var currentData = _inventoryData[index];

            if (currentData.inventoryItem != null)
            {
                // Update existing UI item
                currentData.itemInstance = newInstance;
                currentData.inventoryItem.SetInstanceData(newInstance);
                _inventoryData[index] = currentData;
            }
            else
            {
                // Create new UI item
                AddNewItem(newInstance, index);
            }
        }

        // Fire event if this affected the selected quick slot
        if (index == selectedQuickSlotIndex && index < quickSlots.Count)
        {
            OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, _inventoryData[index]);
        }
    }

    private bool AddNewItem(ItemInstanceData itemInstance, int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= allSlots.Count || itemPrefab == null) return false;

        var slot = allSlots[targetSlot];
        if (slot == null) return false;

        // Clear any existing item in the slot
        if (!slot.IsEmpty)
        {
            ClearSlot(targetSlot);
        }

        var newItem = slot.GetComponentInChildren<InventoryItem>();
        if (newItem == null)
        {
            var newItemObj = Instantiate(itemPrefab, slot.transform);
            newItem = newItemObj.GetComponent<InventoryItem>();
        }

        if (newItem == null) return false;

        newItem.Init(itemInstance);

        _inventoryData[targetSlot] = new InventoryItemData
        {
            itemInstance = itemInstance,
            inventoryItem = newItem,
        };

        slot.SetItem(newItem);
        return true;
    }

    private ItemData ResolveItemDataByName(string itemName)
    {
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.itemName == itemName) return item;
        }
        return null;
    }

    // ── Internal Helpers ──────────────────────────────────────────

    private int GetAvailableStackSpace(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsStackable)
        {
            return 0;
        }

        int totalSpace = 0;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.CanStackWith(itemInstance))
            {
                continue;
            }

            totalSpace += Mathf.Max(0, data.itemInstance.itemData.maxStack - data.itemInstance.stackCount);
        }

        return totalSpace;
    }

    private int StackIntoExistingSlots(ItemInstanceData itemInstance, int amountToStack)
    {
        if (!itemInstance.IsStackable || amountToStack <= 0)
        {
            return amountToStack;
        }

        for (int i = 0; i < _inventoryData.Length && amountToStack > 0; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.CanStackWith(itemInstance))
            {
                continue;
            }

            int maxSpace = Mathf.Max(0, data.itemInstance.itemData.maxStack - data.itemInstance.stackCount);
            if (maxSpace <= 0)
            {
                continue;
            }

            int amountAdded = Mathf.Min(maxSpace, amountToStack);
            data.itemInstance.stackCount += amountAdded;
            _inventoryData[i] = data;
            data.inventoryItem.SetInstanceData(data.itemInstance);
            amountToStack -= amountAdded;
        }

        return amountToStack;
    }

    private void RemoveAmountFromIndex(int index, int amount)
    {
        if (index < 0 || index >= _inventoryData.Length)
        {
            return;
        }

        var data = _inventoryData[index];
        if (data.inventoryItem == null || !data.itemInstance.IsValid)
        {
            return;
        }

        int stackCount = Mathf.Max(1, data.itemInstance.stackCount);
        int removeAmount = Mathf.Clamp(amount, 1, stackCount);

        if (data.itemInstance.IsStackable && stackCount > removeAmount)
        {
            data.itemInstance.stackCount -= removeAmount;
            _inventoryData[index] = data;
            data.inventoryItem.SetInstanceData(data.itemInstance);
            return;
        }

        ClearSlot(index);
    }

    private void ClearSlot(int index)
    {
        if (index < 0 || index >= _inventoryData.Length)
        {
            return;
        }

        InventoryItem inventoryItem = _inventoryData[index].inventoryItem;
        _inventoryData[index] = default;

        if (index < allSlots.Count)
        {
            allSlots[index].SetItem(null);
        }

        if (inventoryItem != null)
        {
            Destroy(inventoryItem.gameObject);
        }
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
