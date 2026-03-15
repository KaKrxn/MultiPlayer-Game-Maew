using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;
using System.Diagnostics;


public class InventoryManager : MonoBehaviour
{

    [SerializeField] private CanvasGroup canvasGroup;

    public GameObject itemPrefab;
    public List<InventorySlot> slots = new List<InventorySlot>();
    [PurrReadOnly, SerializeField] private InventoryItemData[] _inventoryData;

    private void Awake()
    {
        // สมมติว่าลงทะเบียน Instance
        InstanceHandler.RegisterInstance(this);
        _inventoryData = new InventoryItemData[slots.Count];
        canvasGroup.blocksRaycasts = false; // ปิดการรับ Input ของ Inventory ตอนเริ่มเกม
        canvasGroup.alpha = 0; // ซ่อน Inventory ตอนเริ่มเกม
        ToggleInventory(false);
        ToggleCursor(true); // แสดง Cursor ตอนเริ่มเกม
    }


    private void Update() {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = canvasGroup.alpha > 0;
            ToggleInventory(!isOpen);
        }
    }

    private void ToggleInventory(bool toggle)
    {
        canvasGroup.alpha = toggle ? 1f : 0f;

        // เปิด/ปิดการรับ input ของ UI
        canvasGroup.blocksRaycasts = toggle;
        canvasGroup.interactable = toggle;

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
            if (string.IsNullOrEmpty(data.itemName))
                continue;

            if (data.itemName != item.ItemName)
                continue;

            data.amount++;
            data.inventoryItem.Init(item.ItemName, item.ItemPicture, data.amount);
            _inventoryData[i] = data;

            return true;
        }

        return false;
    }

    public void AddNewItem(Item item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty)
            {
                var newItem = Instantiate(itemPrefab, slot.transform).GetComponent<InventoryItem>();

                newItem.Init(item.ItemName, item.ItemPicture, 1);

                InventoryItemData itemData = new InventoryItemData
                {
                    itemName = item.ItemName,
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
        var newSlotIndex = slots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(_inventoryData, x => x.inventoryItem == item);

        if (oldSlotIndex == -1 || newSlotIndex == -1)
        {
            UnityEngine.Debug.LogError("Invalid slot index for item move");
            return;
        }

        var temp = _inventoryData[newSlotIndex];
        _inventoryData[newSlotIndex] = _inventoryData[oldSlotIndex];
        _inventoryData[oldSlotIndex] = temp;
    }


    [System.Serializable]
    public struct InventoryItemData
    {
        public string itemName;
        public InventoryItem inventoryItem;
        public int amount;
    }
}