using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace FileGame.Core
{
    /// <summary>
    /// Handles hold-to-eat food consumption.
    /// Client tracks hold input and sends consume request via InventoryNetworkHandler.
    /// Server validates and applies effects + inventory removal.
    /// </summary>
    public class PlayerItemUseSystem : NetworkBehaviour
    {
        private CoreInputHandler inputHandler;
        private CoreStatsHandler statsHandler;
        
        private bool isPrimaryActionHeld = false;
        private float holdTimer = 0f;
        public float holdDurationNeeded = GameConstants.DefaultHoldDuration;

        public float HoldProgress => holdDurationNeeded > 0 ? Mathf.Clamp01(holdTimer / holdDurationNeeded) : 0f;
        public bool IsHolding => isPrimaryActionHeld && IsCurrentItemConsumable();

        // Used by UI
        public static event System.Action<float, bool> OnItemEatingProgress;

        private void Awake()
        {
            inputHandler = GetComponent<CoreInputHandler>();
            statsHandler = GetComponent<CoreStatsHandler>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            bool primaryPressed = Input.GetMouseButton(0);
            bool inventoryOpen = InventoryManager.instance != null && InventoryManager.instance.IsOpen;

            if (primaryPressed && !inventoryOpen)
            {
                if (InventoryManager.instance == null) return;

                if (IsCurrentItemConsumable())
                {
                    isPrimaryActionHeld = true;
                    holdTimer += Time.deltaTime;
                    OnItemEatingProgress?.Invoke(HoldProgress, true);
                    
                    if (holdTimer >= holdDurationNeeded)
                    {
                        ConsumeCurrentItem();
                        isPrimaryActionHeld = false;
                        holdTimer = 0f;
                        OnItemEatingProgress?.Invoke(0f, false);
                    }
                }
                else
                {
                    ResetHoldState();
                }
            }
            else
            {
                ResetHoldState();
            }
        }

        private void ResetHoldState()
        {
            if (isPrimaryActionHeld)
            {
                OnItemEatingProgress?.Invoke(0f, false);
            }
            isPrimaryActionHeld = false;
            holdTimer = 0f;
        }

        private bool IsCurrentItemConsumable()
        {
            if (InventoryManager.instance == null) return false;
            
            int selectedIndex = InventoryManager.instance.selectedQuickSlotIndex;
            var slotData = InventoryManager.instance.GetQuickSlotItemData(selectedIndex);
            
            return slotData.itemInstance.IsValid && 
                   slotData.itemInstance.itemData != null && 
                   slotData.itemInstance.itemData.isConsumable;
        }

        private void ConsumeCurrentItem()
        {
            if (InventoryManager.instance == null) return;

            int selectedIndex = InventoryManager.instance.selectedQuickSlotIndex;
            
            // 🛑 [เพิ่มใหม่] ดึงข้อมูลไอเทมมาเพื่อทำการ Debug ดูชื่อ
            var slotData = InventoryManager.instance.GetQuickSlotItemData(selectedIndex);
            string itemName = "Unknown Item";
            
            if (slotData.itemInstance.IsValid && slotData.itemInstance.itemData != null)
            {
                itemName = slotData.itemInstance.itemData.name; // หรือเปลี่ยนเป็น .ItemName ตามที่คุณตั้งไว้ในโค้ด Data
            }

            // 🛑 [เพิ่มใหม่] ปริ้นท์บอกว่าใช้ไอเทมอะไร สล็อตไหน
            Debug.Log($"🍲 [ItemUseSystem] กดใช้งาน/กินไอเทมสำเร็จ: {itemName} (QuickSlot ช่องที่: {selectedIndex})");


            if (itemName == "Bandage")
            {
                PlayerAction.instance.LocalRevive();
            }

            // Route through InventoryNetworkHandler which handles both
            // the survival effect and the inventory slot removal on server
            var networkHandler = GetComponent<InventoryNetworkHandler>();
            if (networkHandler != null)
            {
                networkHandler.RequestConsumeItemServerRpc(selectedIndex, 1);
            }
            else
            {
                // Fallback: local consumption
                InventoryManager.instance.ConsumeCurrentQuickSlotItem();
            }
        }
    }
}