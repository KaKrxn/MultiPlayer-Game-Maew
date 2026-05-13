using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private CanvasGroup mainInventoryGroup;
    [SerializeField] private CanvasGroup secondaryInventoryGroup;

    public GameObject itemPrefab;

    [Header("UI Slots Setup")]
    public List<InventorySlot> quickSlots = new List<InventorySlot>();
    public List<InventorySlot> mainSlots = new List<InventorySlot>();
    public List<InventorySlot> vaultSlots = new List<InventorySlot>();

    [HideInInspector] public List<InventorySlot> allSlots = new List<InventorySlot>();

    [PurrReadOnly, SerializeField] private InventoryItemData[] _inventoryData;
    [PurrReadOnly, SerializeField] private InventoryItemData[] _vaultInventoryData;

    public static InventoryManager instance;

    public VaultInventory ActiveVault { get; private set; }

    public List<InventoryItemData> inventoryData = new List<InventoryItemData>();
    public List<Item> allItems = new List<Item>();

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
        _vaultInventoryData = new InventoryItemData[vaultSlots.Count];
    }

    private void Start()
    {
        ToggleInventory(false);
        ToggleSecondaryInventory(false);
        ToggleCursor(true);
        SelectQuickSlot(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = mainInventoryGroup != null && mainInventoryGroup.alpha > 0;

            if (ActiveVault != null && isOpen)
            {
                CloseVault();
            }
            else
            {
                ToggleInventory(!isOpen);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(4);

        if (Input.GetKeyDown(KeyCode.G))
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

    public void OpenVault(VaultInventory vault)
    {
        if (vault == null) return;

        CloseVault();

        ActiveVault = vault;
        _vaultInventoryData = ActiveVault.GetData(vaultSlots.Count);

        LoadVaultItems();
        ToggleInventory(true);
        ToggleSecondaryInventory(true);
    }

    public void CloseVault()
    {
        if (ActiveVault != null)
        {
            SaveVaultItems();
        }

        ClearVaultUI();
        ActiveVault = null;
        _vaultInventoryData = new InventoryItemData[vaultSlots.Count];
        ToggleSecondaryInventory(false);
    }

    public void DropItemFromIndex(int index)
    {
        if (index < 0 || index >= _inventoryData.Length) return;

        var data = _inventoryData[index];
        if (data.inventoryItem != null && data.amount > 0)
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

    private void ToggleSecondaryInventory(bool toggle)
    {
        if (secondaryInventoryGroup == null) return;

        secondaryInventoryGroup.alpha = toggle ? 1f : 0f;
        secondaryInventoryGroup.blocksRaycasts = toggle;
        secondaryInventoryGroup.interactable = toggle;
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

    public void AddItem(Item item)
    {
        if (!TryStackItem(item))
        {
            AddNewItem(item);
        }
    }

    private bool TryStackItem(Item item)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (string.IsNullOrEmpty(data.itemName)) continue;
            if (data.itemName != item.ItemName) continue;

            data.amount++;
            data.inventoryItem.Init(item.ItemName, item.ItemPicture, data.amount);
            _inventoryData[i] = data;

            return true;
        }
        return false;
    }

    public void AddNewItem(Item item)
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
            {
                var newItem = Instantiate(itemPrefab, slot.transform).GetComponent<InventoryItem>();
                newItem.Init(item.ItemName, item.ItemPicture, 1);

                InventoryItemData itemData = new InventoryItemData
                {
                    itemName = item.ItemName,
                    ItemPicture = item.ItemPicture,
                    inventoryItem = newItem,
                    amount = 1
                };

                _inventoryData[i] = itemData;
                slot.SetItem(newItem);
                break;
            }
        }
    }

    public bool ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        if (!TryGetItemLocation(item, out var oldData, out int oldIndex, out InventorySlot oldSlot)) return false;
        if (!TryGetSlotLocation(newSlot, out var newData, out int newIndex)) return false;

        if (oldData == newData && oldIndex == newIndex) return true;
        if (newData[newIndex].inventoryItem != null) return false;

        newData[newIndex] = oldData[oldIndex];
        oldData[oldIndex] = default;

        oldSlot.SetItem(null);
        newSlot.SetItem(item);

        return true;
    }

    public void DropItem(InventoryItem inventoryItem)
    {
        if (!TryGetItemLocation(inventoryItem, out var dataSource, out int index, out InventorySlot slot)) return;

        var data = dataSource[index];
        var itemToSpawn = allItems.Find(x => x.ItemName == data.itemName);
        if (itemToSpawn == null) return;

        Vector3 spawnPosition = PlayerLocation.localPlayerMovement.transform.position
            + PlayerLocation.localPlayerMovement.transform.forward
            + Vector3.up;

        if (PlayerDropItem.Instance != null)
        {
            PlayerDropItem.Instance.RequestSpawnItemServerRpc(data.itemName, spawnPosition, Quaternion.identity);
        }

        DeductItem(dataSource, index, slot, inventoryItem);
    }

    private void DeductItem(InventoryItem inventoryItem)
    {
        if (!TryGetItemLocation(inventoryItem, out var dataSource, out int index, out InventorySlot slot)) return;
        DeductItem(dataSource, index, slot, inventoryItem);
    }

    private void DeductItem(InventoryItemData[] dataSource, int index, InventorySlot slot, InventoryItem inventoryItem)
    {
        var data = dataSource[index];
        data.amount--;

        if (data.amount <= 0)
        {
            dataSource[index] = default;
            slot.SetItem(null);
            Destroy(inventoryItem.gameObject);
        }
        else
        {
            data.inventoryItem.Init(data.itemName, data.ItemPicture, data.amount);
            dataSource[index] = data;
        }
    }

    public bool ConsumeItem(string targetItemName)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            if (_inventoryData[i].itemName == targetItemName && _inventoryData[i].amount > 0)
            {
                DeductItem(_inventoryData[i].inventoryItem);
                return true;
            }
        }
        return false;
    }

    private bool TryGetItemLocation(InventoryItem item, out InventoryItemData[] dataSource, out int index, out InventorySlot slot)
    {
        index = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);
        if (index != -1)
        {
            dataSource = _inventoryData;
            slot = allSlots[index];
            return true;
        }

        index = Array.FindIndex(_vaultInventoryData, x => x.inventoryItem == item);
        if (index != -1)
        {
            dataSource = _vaultInventoryData;
            slot = vaultSlots[index];
            return true;
        }

        dataSource = null;
        slot = null;
        return false;
    }

    private bool TryGetSlotLocation(InventorySlot targetSlot, out InventoryItemData[] dataSource, out int index)
    {
        index = allSlots.IndexOf(targetSlot);
        if (index != -1)
        {
            dataSource = _inventoryData;
            return true;
        }

        index = vaultSlots.IndexOf(targetSlot);
        if (index != -1 && ActiveVault != null)
        {
            dataSource = _vaultInventoryData;
            return true;
        }

        dataSource = null;
        return false;
    }

    private void LoadVaultItems()
    {
        ClearVaultUI();

        for (int i = 0; i < vaultSlots.Count && i < _vaultInventoryData.Length; i++)
        {
            var data = _vaultInventoryData[i];
            if (string.IsNullOrEmpty(data.itemName) || data.amount <= 0)
            {
                vaultSlots[i].SetItem(null);
                continue;
            }

            var newItem = Instantiate(itemPrefab, vaultSlots[i].transform).GetComponent<InventoryItem>();
            newItem.Init(data.itemName, data.ItemPicture, data.amount);

            data.inventoryItem = newItem;
            _vaultInventoryData[i] = data;
            vaultSlots[i].SetItem(newItem);
        }
    }

    private void SaveVaultItems()
    {
        if (ActiveVault == null || _vaultInventoryData == null) return;

        for (int i = 0; i < _vaultInventoryData.Length; i++)
        {
            var data = _vaultInventoryData[i];
            data.inventoryItem = null;
            _vaultInventoryData[i] = data;
        }

        ActiveVault.SetData(_vaultInventoryData);
    }

    private void ClearVaultUI()
    {
        foreach (var slot in vaultSlots)
        {
            if (slot == null) continue;

            for (int i = slot.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(slot.transform.GetChild(i).gameObject);
            }

            slot.SetItem(null);
        }
    }

    [System.Serializable]
    public struct InventoryItemData
    {
        public string itemName;
        public Sprite ItemPicture;
        public InventoryItem inventoryItem;
        public int amount;
    }
}
