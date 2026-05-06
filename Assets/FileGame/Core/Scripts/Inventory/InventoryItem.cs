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
    [SerializeField] private Slider durabilitySlider;
    [SerializeField] private TMP_Text durabilityLabel;
    [SerializeField] private TMP_Text weightText;

    private static Sprite s_DefaultSprite;

    public ItemInstanceData InstanceData { get; internal set; }
    public ItemData Data => InstanceData.itemData;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        itemImage = GetComponent<Image>();

        if (amountText == null)
        {
            Transform amountTransform = transform.Find("amount");
            if (amountTransform != null)
            {
                amountText = amountTransform.GetComponent<TMP_Text>();
            }
        }

        EnsureStatUI();
    }

    public void Init(ItemData data, int amount)
    {
        Init(new ItemInstanceData(data, 100, 0.1f));
    }

    public void Init(ItemInstanceData itemInstance)
    {
        InstanceData = itemInstance;
        EnsureStatUI();

        if (itemImage == null)
        {
            Debug.LogError("itemImage is NULL on " + gameObject.name);
            return;
        }

        if (itemInstance.itemData != null)
        {
            itemImage.sprite = itemInstance.itemData.itemPicture;
        }

        if (amountText != null)
        {
            amountText.text = string.Empty;
            amountText.gameObject.SetActive(false);
        }

        UpdateStatUI();
    }

    private void UpdateStatUI()
    {
        if (!InstanceData.IsValid) return;

        if (durabilitySlider != null)
        {
            durabilitySlider.minValue = 0f;
            durabilitySlider.maxValue = 100f;
            durabilitySlider.value = InstanceData.durabilityPercent;
        }

        if (durabilityLabel != null)
        {
            durabilityLabel.text = $"{InstanceData.durabilityPercent}%";
        }

        if (weightText != null)
        {
            weightText.text = $"{InstanceData.weightKg:0.0}kg";
        }
    }

    private void EnsureStatUI()
    {
        if (durabilitySlider != null && durabilityLabel != null && weightText != null)
        {
            return;
        }

        RectTransform statsRoot = transform.Find("ItemStats") as RectTransform;
        if (statsRoot == null)
        {
            GameObject root = new GameObject("ItemStats", typeof(RectTransform));
            statsRoot = root.GetComponent<RectTransform>();
            statsRoot.SetParent(transform, false);
            statsRoot.anchorMin = new Vector2(0f, 0f);
            statsRoot.anchorMax = new Vector2(1f, 0f);
            statsRoot.pivot = new Vector2(0.5f, 0f);
            statsRoot.anchoredPosition = new Vector2(0f, 2f);
            statsRoot.sizeDelta = new Vector2(0f, 30f);
        }

        TMP_FontAsset fontAsset = amountText != null ? amountText.font : null;

        if (durabilityLabel == null)
        {
            durabilityLabel = CreateText("DurabilityValue", statsRoot, fontAsset, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0.4f, 1f), new Vector2(0f, 1f), new Vector2(6f, -2f), new Vector2(-12f, 14f));
        }

        if (weightText == null)
        {
            weightText = CreateText("WeightValue", statsRoot, fontAsset, TextAlignmentOptions.TopRight, new Vector2(0.4f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -2f), new Vector2(-12f, 14f));
        }

        if (durabilitySlider == null)
        {
            durabilitySlider = CreateSlider("DurabilitySlider", statsRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(-12f, 10f), new Color(0.16f, 0.16f, 0.16f, 0.9f), new Color(0.35f, 0.8f, 0.35f, 1f));
        }
    }

    private TMP_Text CreateText(string objectName, RectTransform parent, TMP_FontAsset fontAsset, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.pivot = pivot;
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = sizeDelta;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.fontSize = 12f;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;

        return text;
    }

    private Slider CreateSlider(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color backgroundColor, Color fillColor)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.SetParent(parent, false);
        sliderRect.anchorMin = anchorMin;
        sliderRect.anchorMax = anchorMax;
        sliderRect.pivot = pivot;
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = sizeDelta;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;

        Image background = sliderObject.AddComponent<Image>();
        background.sprite = GetDefaultSprite();
        background.type = Image.Type.Sliced;
        background.color = backgroundColor;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.SetParent(sliderRect, false);
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = GetDefaultSprite();
        fillImage.type = Image.Type.Sliced;
        fillImage.color = fillColor;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return slider;
    }

    private Sprite GetDefaultSprite()
    {
        if (s_DefaultSprite == null)
        {
            s_DefaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        return s_DefaultSprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        if (canvas != null)
        {
            transform.SetParent(canvas.transform);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

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
            Debug.LogError("Failed to get inventory manager to drop item!");
            return;
        }

        inventoryManager.DropItem(this);
    }

    public void SetAvailable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void SetInstanceData(ItemInstanceData data)
    {
        InstanceData = data;
        UpdateStatUI();
    }

    public void RefreshStatDisplay()
    {
        UpdateStatUI();
    }
}
