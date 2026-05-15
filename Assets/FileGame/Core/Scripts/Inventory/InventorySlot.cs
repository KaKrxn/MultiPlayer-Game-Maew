using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public InventoryItem Item { get; private set; }

    // Container ที่ slot นี้สังกัด (Main Inventory หรือ Vault) และ index ภายใน Data array
    public IItemContainer Container { get; private set; }
    public int Index { get; private set; } = -1;

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

    public void Bind(IItemContainer container, int index)
    {
        Container = container;
        Index = index;
    }

    public void SetItem(InventoryItem itemToSet)
    {
        Item = itemToSet;
    }

    /// <summary>
    /// ล้าง reference ของ item ในช่องนี้ (ไม่ทำลายตัว GameObject)
    /// </summary>
    public void ClearItem()
    {
        Item = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggedItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (draggedItem == null) return;

        // หา slot ต้นทางจาก parent เดิมที่ InventoryItem บันทึกไว้ก่อนเริ่มลาก
        var sourceParent = draggedItem.OriginalParent;
        var sourceSlot = sourceParent != null ? sourceParent.GetComponent<InventorySlot>() : null;
        if (sourceSlot == null) return;

        // ปล่อยที่ slot ตัวเอง -> กลับที่เดิม
        if (sourceSlot == this)
        {
            draggedItem.transform.SetParent(transform);
            draggedItem.SetAvailable();
            return;
        }

        // ถ้าช่องปลายทางมีของอยู่ ให้สลับ (ย้ายของเดิมกลับไปต้นทาง)
        if (!IsEmpty)
        {
            for (int c = transform.childCount - 1; c >= 0; c--)
            {
                var existing = transform.GetChild(c);
                existing.SetParent(sourceParent);
                var existingItem = existing.GetComponent<InventoryItem>();
                if (existingItem != null) existingItem.SetAvailable();
            }
        }

        // ย้ายของที่ลากเข้ามาที่ช่องนี้ (optimistic visual; server callback จะรีเฟรช state จริง)
        draggedItem.transform.SetParent(transform);
        draggedItem.SetAvailable();
        draggedItem.MarkAsMoved();

        // ส่งคำสั่งย้ายให้ Router เพื่อ dispatch ไปยัง Server RPC ที่ถูกต้อง
        InventoryRouter.HandleItemMove(sourceSlot, this, draggedItem);
    }
}
