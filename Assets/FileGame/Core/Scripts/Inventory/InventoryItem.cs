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
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (eventData.pointerEnter == null || eventData.pointerEnter.GetComponent<InventorySlot>() == null)
        {
            ReturnToOriginalParent();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
        {
            Debug.LogError("Failed to get inventory manager to drop item!");
            return;
        }

        inventoryManager.DropItem(this);
    }

    public void SetAvailable()
    {
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void ReturnToOriginalParent()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
        }

        SetAvailable();
    }
}
