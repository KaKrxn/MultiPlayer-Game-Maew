using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Transform originalParent;

    private Image itemImage;

    [SerializeField] private TMP_Text amountText;

    

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // หา Canvas ที่ใกล้ที่สุดเพื่อใช้คำนวณ Scale Factor เวลาลาก
        canvas = GetComponentInParent<Canvas>();
        itemImage = GetComponent<Image>();
    }

    public void Init(string name, Sprite picture, int amount)
    {
        if (itemImage == null)
        {
            Debug.LogError("itemImage is NULL on " + gameObject.name);
            return;
        }

        if (amountText == null)
        {
            Debug.LogError("amountText is NULL on " + gameObject.name);
            return;
        }

        itemImage.sprite = picture;
        amountText.text = amount.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        canvasGroup.blocksRaycasts = false; // ปิดเพื่อให้ Mouse ทะลุไปโดน Slot ข้างหลังได้
        transform.SetParent(canvas.transform); // ย้ายมาอยู่ชั้นบนสุดของ Canvas เวลาลาก
    }

    public void OnDrag(PointerEventData eventData)
    {
        // คำนวณตำแหน่งตามเมาส์ โดยหารด้วย scaleFactor เพื่อให้ตำแหน่งตรงกับเมาส์พอดี
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // ถ้าไม่ได้ปล่อยลงใน Slot (หรือ PointerEnter ไม่ใช่ Slot) ให้กลับไปที่เดิม
        if (eventData.pointerEnter == null || eventData.pointerEnter.GetComponent<InventorySlot>() == null)
        {
            transform.SetParent(originalParent);
            SetAvailable();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
        {
            Debug.LogError($"Failed to get inventory manager to drop item!");
            return;
        }

        inventoryManager.DropItem(this);
    }

    // ฟังก์ชันช่วยสำหรับรีเซ็ตตำแหน่งเมื่อวางสำเร็จ (เรียกจาก InventorySlot)
    public void SetAvailable()
    {
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    
}