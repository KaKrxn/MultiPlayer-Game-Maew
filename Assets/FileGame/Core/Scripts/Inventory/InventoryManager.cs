using UnityEngine;
using Unity.Collections;
using FileGame.Core;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;

public class InventoryManager : MonoBehaviour, IItemContainer
{
    public ContainerKind Kind => ContainerKind.MainInventory;

    [Header("UI Setup")]
    [Tooltip("ลาก GameObject Root (เช่น Inventory หรือ BG) มาใส่ที่นี่")]
    [SerializeField] private GameObject mainInventoryUI;
    
    [Tooltip("ลาก GameObject 'CharacterPanel' มาใส่ที่นี่ (เปิดตอนกด Tab)")]
    [SerializeField] private GameObject characterPanel;
    
    [Tooltip("ลาก GameObject 'SeconInv' มาใส่ที่นี่ (เปิดตอนเปิดตู้เซฟ)")]
    [SerializeField] private GameObject secondInvPanel;

    [Header("Items Setup")]
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

    public static event Action<int, InventoryItemData> OnSelectedQuickSlotChanged;

    private InventoryNetworkHandler _networkHandler;
    public bool IsOpen => mainInventoryUI != null && mainInventoryUI.activeSelf;
    public bool isVaultOpen = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InstanceHandler.RegisterInstance(this);

        allSlots.AddRange(quickSlots);
        allSlots.AddRange(mainSlots);

        _inventoryData = new InventoryItemData[allSlots.Count];

        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i] != null) allSlots[i].Bind(this, i);
        }
    }

    private void Start()
    {
        isVaultOpen = false;

        ToggleInventory(false);
        ToggleCursor(true);
        SelectQuickSlot(0);
    }

    private void Update()
    {
        if (_networkHandler == null && PlayerLocation.localPlayerMovement != null)
        {
            _networkHandler = PlayerLocation.localPlayerMovement.GetComponent<InventoryNetworkHandler>();
        }

        if (Input.GetKeyDown(inventoryKey))
        {
            if (isVaultOpen)
            {
                // ถ้าตู้เปิดอยู่ กด Tab คือการปิดทั้งหมด
                CloseVault();
            }
            else
            {
                // สลับเปิด/ปิด กระเป๋าปกติ
                if (mainInventoryUI != null)
                {
                    bool isOpen = mainInventoryUI.activeSelf;
                    ToggleInventory(!isOpen);
                }
            }
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

    // ── ระบบจัดการเปิด/ปิด UI ตามเงื่อนไข ─────────────────────────

    public void OpenVault(VaultInventory vault)
    {
        isVaultOpen = true;

        // จัดการเปิด/ปิด Panel ให้ตรงกับโหมดตู้เซฟ
        if (characterPanel != null) characterPanel.SetActive(false);
        if (secondInvPanel != null) secondInvPanel.SetActive(true);

        // โชว์ UI กระเป๋าหลักขึ้นมา
        if (mainInventoryUI != null)
        {
            mainInventoryUI.SetActive(true);
        }
        ToggleCursor(true);
    }

    public void CloseVault()
    {
        if (!isVaultOpen) return;

        isVaultOpen = false;
        ToggleInventory(false);
    }

    public void SetInventoryOpen(bool open)
    {
        ToggleInventory(open);
    }

    private void ToggleInventory(bool toggle)
    {
        // หากสั่งปิด ให้เคลียร์สถานะตู้เซฟด้วย
        if (!toggle)
        {
            isVaultOpen = false;
        }

        // หากเป็นการ "เปิด" และไม่ได้อยู่ในโหมดตู้เซฟ ให้เซ็ตหน้าจอเป็นโหมดกระเป๋าปกติ
        if (toggle && !isVaultOpen)
        {
            if (characterPanel != null) characterPanel.SetActive(true);
            if (secondInvPanel != null) secondInvPanel.SetActive(false);
        }

        // เปิด/ปิด ตัว Root UI
        if (mainInventoryUI != null)
        {
            mainInventoryUI.SetActive(toggle);
            var modelView = mainInventoryUI.GetComponentInChildren<FileGame.Core.UI.CharacterModelView>(true);
            if (modelView != null) modelView.Refresh(toggle);
        }

        ToggleCursor(toggle);
    }

    // ──────────────────────────────────────────────────

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
        for (int i = 0; i < _inventoryData.Length; i++) { if (_inventoryData[i].inventoryItem == null) return true; }
        return false;
    }

    public bool CanAcceptItem(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return false;
        if (itemInstance.IsStackable && GetAvailableStackSpace(itemInstance) >= itemInstance.stackCount) return true;
        return HasFreeSlot();
    }

    public void AddItem(ItemData itemType)
    {
        if (itemType == null) return;
        float defaultWeight = itemType.usesAmountValue ? 0f : 0.1f;
        AddItem(new ItemInstanceData(itemType, 100, defaultWeight, 1));
    }

    public void AddItem(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return;
        if (_networkHandler != null)
        {
            _networkHandler.RequestAddItemServerRpc(new FixedString32Bytes(itemInstance.itemData.itemName), itemInstance.durabilityPercent, itemInstance.weightKg, itemInstance.stackCount);
            return;
        }
        AddItemLocal(itemInstance);
    }

    private void AddItemLocal(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsValid) return;
        int remaining = itemInstance.stackCount;
        if (itemInstance.IsStackable) remaining = StackIntoExistingSlots(itemInstance, remaining);

        while (remaining > 0)
        {
            int amountToAdd = itemInstance.IsStackable ? Mathf.Min(remaining, itemInstance.itemData.maxStack) : 1;
            ItemInstanceData itemToInsert = new ItemInstanceData(itemInstance.itemData, itemInstance.durabilityPercent, itemInstance.weightKg, amountToAdd);
            if (!AddNewItem(itemToInsert)) return;
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
            _inventoryData[i] = new InventoryItemData { itemInstance = itemInstance, inventoryItem = newItem };
            slot.SetItem(newItem);

            if (i == selectedQuickSlotIndex && i < quickSlots.Count) OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, _inventoryData[i]);
            return true;
        }
        return false;
    }

    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = allSlots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);

        if (oldSlotIndex == -1 || newSlotIndex == -1) return;
        if (oldSlotIndex == newSlotIndex) return;

        // Optimistic local swap (เพื่อให้ _inventoryData ตรงกับตำแหน่ง visual ทันที)
        // ถ้ามี networkHandler จะส่ง RPC ตามไป server จะยืนยันผ่าน NetworkList อีกที
        item.MarkAsMoved();
        SwapSlotsLocal(oldSlotIndex, newSlotIndex);

        if (_networkHandler != null)
        {
            _networkHandler.RequestMoveItemServerRpc(oldSlotIndex, newSlotIndex);
        }
    }

    private void SwapSlotsLocal(int oldSlotIndex, int newSlotIndex)
    {
        var temp = _inventoryData[newSlotIndex];
        _inventoryData[newSlotIndex] = _inventoryData[oldSlotIndex];
        _inventoryData[oldSlotIndex] = temp;

        allSlots[oldSlotIndex].ClearItem();
        allSlots[newSlotIndex].ClearItem();

        // จัดการไอเทมช่องใหม่
        var itemNew = _inventoryData[newSlotIndex].inventoryItem;
        if (itemNew != null)
        {
            itemNew.transform.SetParent(allSlots[newSlotIndex].transform, false);
            itemNew.transform.localPosition = Vector3.zero;
            itemNew.transform.localScale = Vector3.one; // ป้องกันไอเทมตัวจิ๋ว/หาย
            allSlots[newSlotIndex].SetItem(itemNew);
        }

        // จัดการไอเทมช่องเก่า
        var itemOld = _inventoryData[oldSlotIndex].inventoryItem;
        if (itemOld != null)
        {
            itemOld.transform.SetParent(allSlots[oldSlotIndex].transform, false);
            itemOld.transform.localPosition = Vector3.zero;
            itemOld.transform.localScale = Vector3.one; // ป้องกันไอเทมตัวจิ๋ว/หาย
            allSlots[oldSlotIndex].SetItem(itemOld);
        }

        if (newSlotIndex == selectedQuickSlotIndex || oldSlotIndex == selectedQuickSlotIndex)
            OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, GetQuickSlotItemData(selectedQuickSlotIndex));
    }

    public void DropItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;
            if (!data.itemInstance.IsValid || PlayerLocation.localPlayerMovement == null) return;

            if (_networkHandler != null)
            {
                _networkHandler.RequestDropItemServerRpc(i);
                return;
            }

            if (PlayerDropItem.Instance == null || data.itemInstance.itemData.dropPrefab == null) return;

            Vector3 spawnPosition = PlayerLocation.localPlayerMovement.transform.position + PlayerLocation.localPlayerMovement.transform.forward + Vector3.up;
            PlayerDropItem.Instance.RequestSpawnItemServerRpc(data.itemInstance.itemData.itemName, data.itemInstance.durabilityPercent, data.itemInstance.weightKg, data.itemInstance.stackCount, spawnPosition, Quaternion.identity);

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

    public InventoryItemData GetQuickSlotItemData(int index) { if (index < 0 || index >= quickSlots.Count || index >= _inventoryData.Length) return default; return _inventoryData[index]; }
    public InventoryItemData GetSelectedQuickSlotItem() { return GetQuickSlotItemData(selectedQuickSlotIndex); }

    public void ConsumeCurrentQuickSlotItem()
    {
        int index = selectedQuickSlotIndex;
        if (index < 0 || index >= _inventoryData.Length || index >= quickSlots.Count) return;
        var data = _inventoryData[index];
        if (data.inventoryItem == null) return;

        if (_networkHandler != null) { _networkHandler.RequestConsumeItemServerRpc(index, 1); return; }
        RemoveAmountFromIndex(index, 1);
    }

    public int GetSelectedQuickSlotItemCount(ItemData targetItem)
    {
        if (targetItem == null) return 0;
        int index = selectedQuickSlotIndex;
        if (index < 0 || index >= _inventoryData.Length || index >= quickSlots.Count) return 0;
        var data = _inventoryData[index];
        if (data.inventoryItem == null || !data.itemInstance.IsValid || !IsSameItemData(data.itemInstance.itemData, targetItem)) return 0;
        return Mathf.Max(1, data.itemInstance.stackCount);
    }

    public bool HasSelectedQuickSlotItemAmount(ItemData targetItem, int amount) { return amount > 0 && GetSelectedQuickSlotItemCount(targetItem) >= amount; }

    public bool ConsumeSelectedQuickSlotItemAmount(ItemData targetItem, int amount)
    {
        if (targetItem == null || amount <= 0 || !HasSelectedQuickSlotItemAmount(targetItem, amount)) return false;
        int index = selectedQuickSlotIndex;
        if (index < 0 || index >= _inventoryData.Length || index >= quickSlots.Count) return false;

        if (_networkHandler != null)
        {
            _networkHandler.RequestRemoveItemFromSlotServerRpc(index, amount, new FixedString32Bytes(targetItem.itemName));
            return true;
        }
        RemoveAmountFromIndex(index, amount);
        return true;
    }

    private static bool IsSameItemData(ItemData left, ItemData right) { if (left == null || right == null) return false; return left == right || left.itemName == right.itemName; }
    public bool ConsumeItem(ItemData targetItem) { return ConsumeItemAmount(targetItem, 1); }

    public int GetTotalItemCount(ItemData targetItem)
    {
        if (targetItem == null) return 0;
        int total = 0;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            if (_inventoryData[i].inventoryItem == null || _inventoryData[i].itemInstance.itemData != targetItem) continue;
            total += Mathf.Max(1, _inventoryData[i].itemInstance.stackCount);
        }
        return total;
    }

    public bool HasItemAmount(ItemData targetItem, int amount) { return amount > 0 && GetTotalItemCount(targetItem) >= amount; }

    public bool ConsumeItemAmount(ItemData targetItem, int amount)
    {
        if (targetItem == null || amount <= 0 || !HasItemAmount(targetItem, amount)) return false;
        if (_networkHandler != null)
        {
            _networkHandler.RequestConsumeItemByNameServerRpc(new FixedString32Bytes(targetItem.itemName), amount);
            return true;
        }

        int remaining = amount;
        for (int i = 0; i < _inventoryData.Length && remaining > 0; i++)
        {
            if (_inventoryData[i].inventoryItem == null || _inventoryData[i].itemInstance.itemData != targetItem) continue;
            int removeAmount = Mathf.Min(remaining, Mathf.Max(1, _inventoryData[i].itemInstance.stackCount));
            RemoveAmountFromIndex(i, removeAmount);
            remaining -= removeAmount;
        }
        return remaining <= 0;
    }

    public void ApplyServerSlotUpdate(int index, NetworkInventorySlotData serverData)
    {
        if (index < 0 || index >= _inventoryData.Length || index >= allSlots.Count) return;

        if (serverData.isEmpty) ClearSlot(index);
        else
        {
            string itemName = serverData.itemName.ToString();
            ItemData itemData = ResolveItemDataByName(itemName);
            if (itemData == null) return;

            ItemInstanceData newInstance = new ItemInstanceData(itemData, serverData.durability, serverData.weight, serverData.stackCount);
            var currentData = _inventoryData[index];

            if (currentData.inventoryItem != null)
            {
                currentData.itemInstance = newInstance;
                currentData.inventoryItem.SetInstanceData(newInstance);
                _inventoryData[index] = currentData;
            }
            else AddNewItem(newInstance, index);
        }

        if (index == selectedQuickSlotIndex && index < quickSlots.Count) OnSelectedQuickSlotChanged?.Invoke(selectedQuickSlotIndex, _inventoryData[index]);
    }

    private bool AddNewItem(ItemInstanceData itemInstance, int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= allSlots.Count || itemPrefab == null) return false;
        var slot = allSlots[targetSlot];
        if (slot == null) return false;
        if (!slot.IsEmpty) ClearSlot(targetSlot);

        var newItem = slot.GetComponentInChildren<InventoryItem>();
        if (newItem == null)
        {
            var newItemObj = Instantiate(itemPrefab, slot.transform);
            newItem = newItemObj.GetComponent<InventoryItem>();
        }
        if (newItem == null) return false;

        newItem.Init(itemInstance);
        _inventoryData[targetSlot] = new InventoryItemData { itemInstance = itemInstance, inventoryItem = newItem };
        slot.SetItem(newItem);
        return true;
    }

    private ItemData ResolveItemDataByName(string itemName)
    {
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems) { if (item != null && item.itemName == itemName) return item; }
        return null;
    }

    private int GetAvailableStackSpace(ItemInstanceData itemInstance)
    {
        if (!itemInstance.IsStackable) return 0;
        int totalSpace = 0;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.CanStackWith(itemInstance)) continue;
            totalSpace += Mathf.Max(0, data.itemInstance.itemData.maxStack - data.itemInstance.stackCount);
        }
        return totalSpace;
    }

    private int StackIntoExistingSlots(ItemInstanceData itemInstance, int amountToStack)
    {
        if (!itemInstance.IsStackable || amountToStack <= 0) return amountToStack;
        for (int i = 0; i < _inventoryData.Length && amountToStack > 0; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.CanStackWith(itemInstance)) continue;
            int maxSpace = Mathf.Max(0, data.itemInstance.itemData.maxStack - data.itemInstance.stackCount);
            if (maxSpace <= 0) continue;

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
        if (index < 0 || index >= _inventoryData.Length) return;
        var data = _inventoryData[index];
        if (data.inventoryItem == null || !data.itemInstance.IsValid) return;

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
        if (index < 0 || index >= _inventoryData.Length) return;
        InventoryItem inventoryItem = _inventoryData[index].inventoryItem;
        _inventoryData[index] = default;
        if (index < allSlots.Count) allSlots[index].SetItem(null);
        if (inventoryItem != null) Destroy(inventoryItem.gameObject);
    }

    public bool TryConsumeJacketDurability(int dmgAmount)
    {
        bool found = false;
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem == null || !data.itemInstance.IsValid || data.itemInstance.itemData.itemType != ItemType.Clothing) continue;

            found = true;
            int newDur = Mathf.Clamp(data.itemInstance.durabilityPercent - dmgAmount, 0, 100);
            var updated = data.itemInstance;
            updated.durabilityPercent = newDur;
            _inventoryData[i] = new InventoryItemData { itemInstance = updated, inventoryItem = data.inventoryItem };
            data.inventoryItem.SetInstanceData(updated);

            if (newDur <= 0) { DeductItem(data.inventoryItem); continue; }
            break;
        }
        return found;
    }

    public void RefreshItemDurabilityUI(int slotIndex) { if (slotIndex < 0 || slotIndex >= _inventoryData.Length) return; _inventoryData[slotIndex].inventoryItem?.RefreshStatDisplay(); }

    public bool HasJacketInInventory()
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var d = _inventoryData[i];
            if (d.inventoryItem != null && d.itemInstance.IsValid && d.itemInstance.itemData.itemType == ItemType.Clothing && d.itemInstance.durabilityPercent > 0) return true;
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