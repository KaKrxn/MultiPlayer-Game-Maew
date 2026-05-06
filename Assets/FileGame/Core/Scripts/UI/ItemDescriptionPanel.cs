using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FileGame.Core.UI
{
    /// <summary>
    /// Displays dynamic key prompts based on the currently selected hotbar item.
    /// Listens to InventoryManager events.
    /// </summary>
    public class ItemDescriptionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Transform promptContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject keyPromptPrefab; // Should contain a TMP_Text for key and TMP_Text for action

        private void OnEnable()
        {
            InventoryManager.OnSelectedQuickSlotChanged += HandleQuickSlotChanged;
            
            // Initial update
            if (InventoryManager.instance != null)
            {
                var currentItem = InventoryManager.instance.GetSelectedQuickSlotItem();
                UpdatePanel(currentItem);
            }
        }

        private void OnDisable()
        {
            InventoryManager.OnSelectedQuickSlotChanged -= HandleQuickSlotChanged;
        }

        private void HandleQuickSlotChanged(int slotIndex, InventoryManager.InventoryItemData itemData)
        {
            UpdatePanel(itemData);
        }

        public void UpdatePanel(InventoryManager.InventoryItemData slotData)
        {
            // Clear existing prompts
            foreach (Transform child in promptContainer)
            {
                Destroy(child.gameObject);
            }

            if (!slotData.itemInstance.IsValid)
            {
                if (itemNameText != null) itemNameText.text = "";
                return;
            }

            var itemData = slotData.itemInstance.itemData;
            
            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
            }

            // Build prompts based on ItemType
            switch (itemData.itemType)
            {
                case ItemType.Consumable:
                    CreatePrompt("LMB", "Eat");
                    CreatePrompt("G", "Drop");
                    break;
                
                case ItemType.Weapon:
                    CreatePrompt("LMB", "Attack");
                    CreatePrompt("G", "Drop");
                    break;
                    
                case ItemType.Material:
                    CreatePrompt("G", "Drop");
                    break;
                    
                case ItemType.Tool:
                    CreatePrompt("LMB", "Use");
                    CreatePrompt("G", "Drop");
                    break;
                    
                case ItemType.Generic:
                default:
                    // Example Generic action
                    CreatePrompt("G", "Drop");
                    break;
            }
        }

        private void CreatePrompt(string keyRaw, string actionTextRaw)
        {
            if (keyPromptPrefab == null || promptContainer == null) return;

            GameObject promptObj = Instantiate(keyPromptPrefab, promptContainer);
            
            // Find text components (assuming they are named "KeyText" and "ActionText" 
            // or we grab them by index order if names aren't reliable)
            TMP_Text[] texts = promptObj.GetComponentsInChildren<TMP_Text>();
            
            if (texts.Length >= 2)
            {
                // First text is usually the key (e.g. [G])
                texts[0].text = $"{keyRaw}";
                // Second text is action
                texts[1].text = actionTextRaw;
            }
            else if (texts.Length == 1)
            {
                // Fallback if only 1 text component
                texts[0].text = $"{keyRaw} {actionTextRaw}";
            }
        }
    }
}
