using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;
using System.Diagnostics;
using System.Numerics;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("ใส่ Canvas Group ของ MainInventoryPanel (คุมเฉพาะกระเป๋าหลัก)")]
    [SerializeField] private CanvasGroup mainInventoryGroup;

    public GameObject itemPrefab;

    [Header("UI Slots Setup")]
    public List<InventorySlot> quickSlots = new List<InventorySlot>();
    public List<InventorySlot> mainSlots = new List<InventorySlot>();

    [HideInInspector] public List<InventorySlot> allSlots = new List<InventorySlot>();

    [PurrReadOnly, SerializeField] private InventoryItemData[] _inventoryData;

    public static InventoryManager instance;

    public List<InventoryItemData> inventoryData = new List<InventoryItemData>();
    public List<Item> allItems = new List<Item>();

    // ตัวแปรจำว่าตอนนี้เลือก Quick Slot ช่องไหนอยู่ (เริ่มที่ช่อง 0)
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

        // เริ่มเกมมาให้เลือกช่องที่ 1 (Index 0) ไว้ก่อน
        SelectQuickSlot(0);
    }

    private void Update()
    {
        // เปิด/ปิด กระเป๋าหลัก
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = mainInventoryGroup.alpha > 0;
            ToggleInventory(!isOpen);
        }

        // ระบบเลื่อนเลือกช่อง Quick Slots (1-5)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(4);

        // ระบบโยนของทิ้งจาก Quick Slot ที่เลือกอยู่
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropItemFromIndex(selectedQuickSlotIndex);
        }
    }

    // ฟังก์ชันสำหรับเลือกและไฮไลต์ Quick Slot
    private void SelectQuickSlot(int index)
    {
        if (index < 0 || index >= quickSlots.Count) return;
        selectedQuickSlotIndex = index;

        // วนลูปเพื่อปรับ UI (ช่องที่เลือกจะขยายใหญ่ 1.15 เท่า, ช่องอื่นขนาดปกติ)
        for (int i = 0; i < quickSlots.Count; i++)
        {
            if (quickSlots[i] != null)
            {
                quickSlots[i].transform.localScale = (i == selectedQuickSlotIndex) ? new UnityEngine.Vector3(1.15f, 1.15f, 1.15f) : UnityEngine.Vector3.one;
            }
        }
    }

    // ฟังก์ชันสั่ง Drop ของโดยอิงจาก Index
    public void DropItemFromIndex(int index)
    {
        if (index < 0 || index >= _inventoryData.Length) return;

        var data = _inventoryData[index];
        // เช็คว่าช่องนั้นมีไอเทมอยู่จริงๆ ค่อยทิ้ง
        if (data.inventoryItem != null && data.amount > 0)
        {
            DropItem(data.inventoryItem);
        }
    }

    private void ToggleInventory(bool toggle)
    {
        // เปลี่ยนมาคุมตัว mainInventoryGroup แทน
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

    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = allSlots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);

        if (oldSlotIndex == -1 || newSlotIndex == -1) return;

        var temp = _inventoryData[newSlotIndex];
        _inventoryData[newSlotIndex] = _inventoryData[oldSlotIndex];
        _inventoryData[oldSlotIndex] = temp;
    }

    public void DropItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem) continue;

            var itemToSpawn = allItems.Find(x => x.ItemName == data.itemName);
            if (itemToSpawn == null) return;

            UnityEngine.Vector3 spawnPosition = PlayerLocation.localPlayerMovement.transform.position + PlayerLocation.localPlayerMovement.transform.forward + (UnityEngine.Vector3.up);

            if (PlayerDropItem.Instance != null)
            {
                PlayerDropItem.Instance.RequestSpawnItemServerRpc(data.itemName, spawnPosition, UnityEngine.Quaternion.identity);
            }

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

            data.amount--;
            if (data.amount <= 0)
            {
                _inventoryData[i] = default;
                allSlots[i].SetItem(null);
                Destroy(inventoryItem.gameObject);
            }
            else
            {
                data.inventoryItem.Init(data.itemName, data.ItemPicture, data.amount);
                _inventoryData[i] = data;
            }
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

    [System.Serializable]
    public struct InventoryItemData
    {
        public string itemName;
        public Sprite ItemPicture;
        public InventoryItem inventoryItem;
        public int amount;
    }
}