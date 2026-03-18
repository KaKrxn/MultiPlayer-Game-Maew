using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Utils;
using System.Diagnostics;
using System.Numerics;


public class InventoryManager : MonoBehaviour
{

    [SerializeField] private CanvasGroup canvasGroup;

    public GameObject itemPrefab;
    public List<InventorySlot> slots = new List<InventorySlot>();
    [PurrReadOnly, SerializeField] private InventoryItemData[] _inventoryData;

    public static InventoryManager instance;

    public List<InventoryItemData> inventoryData = new List<InventoryItemData>();
    public List<Item> allItems = new List<Item>();

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
        _inventoryData = new InventoryItemData[slots.Count];
    }

    private void Start()
    {
        ToggleInventory(false); 
        ToggleCursor(true);
    }


    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = canvasGroup.alpha > 0;
            ToggleInventory(!isOpen);
        }
    }

    private void ToggleInventory(bool toggle)
    {
        canvasGroup.alpha = toggle ? 1f : 0f;

        // สองบรรทัดนี้คือตัวตัดสินว่าคลิกได้ไหม
        canvasGroup.blocksRaycasts = toggle; // ต้องเป็น True ตอนเปิด
        canvasGroup.interactable = toggle;   // ต้องเป็น True ตอนเปิด

        ToggleCursor(toggle);
    }

    private void ToggleCursor(bool toggle)
    {
        if (toggle)
        {
            Cursor.lockState = CursorLockMode.None; // ปลดล็อก
            Cursor.visible = true;                  // โชว์ตัว
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined; 
            Cursor.visible = true; 
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


    public void DropItem(InventoryItem inventoryItem)
    {
        for (int i = 0; i < _inventoryData.Length; i++)
        {
            var data = _inventoryData[i];
            if (data.inventoryItem != inventoryItem)
                continue;

            var itemToSpawn = allItems.Find(x => x.ItemName == data.itemName);
            if (itemToSpawn == null)
            {
                UnityEngine.Debug.LogError($"Item to spawn with name {data.itemName} not found!", this);
                return;
            }

            UnityEngine.Vector3 spawnPosition = PlayerLocation.localPlayerMovement.transform.position + PlayerLocation.localPlayerMovement.transform.forward + (UnityEngine.Vector3.up);
            // var item = Instantiate(itemToSpawn, spawnPosition, UnityEngine.Quaternion.identity);
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
            if (data.inventoryItem != inventoryItem)
                continue;

            data.amount--;
            if (data.amount <= 0)
            {
                _inventoryData[i] = default;
                slots[i].SetItem(null);
                Destroy(inventoryItem.gameObject);
            }
            else
            {
                data.inventoryItem.Init(data.itemName, data.ItemPicture, data.amount);
                _inventoryData[i] = data;
            }
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