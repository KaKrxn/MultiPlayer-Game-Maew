using UnityEngine;
using UnityEngine.EventSystems;
using PurrNet.Utils;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public InventoryItem Item { get; private set; }

    public bool IsEmpty => Item == null;

    private void Awake()
    {
        UnityEngine.UI.Graphic[] graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        UnityEngine.UI.Graphic rootGraphic = GetComponent<UnityEngine.UI.Graphic>();

        foreach (var g in graphics)
        {
            if (g != rootGraphic)
            {
                g.raycastTarget = false;
            }
        }
    }

    public void SetItem(InventoryItem itemToSet)
    {
        Item = itemToSet;

        if (itemToSet != null)
        {
            itemToSet.transform.SetParent(transform, false);
            itemToSet.SetAvailable();
        }
    }

    /// <summary>
    /// Nulls out the slot's item reference without destroying the item.
    /// Used by InventoryManager before reassigning items during swaps.
    /// </summary>
    public void ClearItem()
    {
        Item = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            var inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
            if (inventoryItem == null) return;

            if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
            {
                Debug.LogError("Couldn't get inventory manager for slot: " + name);
                return;
            }

            inventoryManager.ItemMoved(inventoryItem, this);
        }
    }
}
