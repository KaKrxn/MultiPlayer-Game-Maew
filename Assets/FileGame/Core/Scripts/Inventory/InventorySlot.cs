using UnityEngine;
using UnityEngine.EventSystems;
using PurrNet.Utils;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public InventoryItem Item { get; private set; }

    public bool IsEmpty => transform.childCount == 0;

    public void SetItem(InventoryItem itemToSet)
    {
        Item = itemToSet;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            eventData.pointerDrag.transform.SetParent(transform);

            var inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
            inventoryItem.SetAvailable();

            if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
            {
                Debug.LogError("Couldn't get inventory manager for slot: " + name);
                return;
            }

            inventoryManager.ItemMoved(inventoryItem, this);
        }
    }
}