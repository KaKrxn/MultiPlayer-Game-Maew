using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public InventoryItem Item { get; private set; }

    public bool IsEmpty => Item == null;

    public void SetItem(InventoryItem itemToSet)
    {
        Item = itemToSet;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (inventoryItem == null) return;

        if (!IsEmpty && Item != inventoryItem)
        {
            inventoryItem.ReturnToOriginalParent();
            return;
        }

        if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
        {
            Debug.LogError("Couldn't get inventory manager for slot: " + name);
            inventoryItem.ReturnToOriginalParent();
            return;
        }

        if (!inventoryManager.ItemMoved(inventoryItem, this))
        {
            inventoryItem.ReturnToOriginalParent();
            return;
        }

        eventData.pointerDrag.transform.SetParent(transform);
        inventoryItem.SetAvailable();
    }
}
