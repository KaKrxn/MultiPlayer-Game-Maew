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
    [SerializeField] private Sprite defaultSprite;

    [Header("Dynamic UI References")]
    [SerializeField] private GameObject genericPanel;
    [SerializeField] private GameObject scrapPanel;
    [SerializeField] private TMP_Text scrapAmountText;


    private static Sprite s_DefaultSprite;

    public ItemInstanceData InstanceData { get; internal set; }
    public ItemData Data => InstanceData.itemData;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        itemImage = GetComponent<Image>();

        // Unpack dynamic root panels in case not formally mapped
        Transform genPanel = transform.Find("Generic");
        if (genPanel != null && genericPanel == null) genericPanel = genPanel.gameObject;

        Transform scrPanel = transform.Find("Scrap");
        if (scrPanel != null)
        {
            if (scrapPanel == null) scrapPanel = scrPanel.gameObject;
            Transform wtext = scrPanel.Find("WText (TMP)");
            if (wtext != null && scrapAmountText == null) scrapAmountText = wtext.GetComponent<TMP_Text>();
        }

        if (amountText == null)
        {
            Transform amountTransform = transform.Find("amount");
            if (amountTransform != null)
            {
                amountText = amountTransform.GetComponent<TMP_Text>();
            }
        }

        DisableChildRaycasts();
        EnsureStatUI();
    }

    private void DisableChildRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        Graphic rootGraphic = GetComponent<Graphic>();

        foreach (var g in graphics)
        {
            if (g != rootGraphic)
            {
                g.raycastTarget = false;
            }
        }
    }

    public void Init(ItemData data, int amount)
    {
        float defaultWeight = data != null && data.usesAmountValue ? 0f : 0.1f;
        Init(new ItemInstanceData(data, 100, defaultWeight, amount));
    }

    public void Init(ItemInstanceData itemInstance)
    {
        SetInstanceData(itemInstance);
    }

    public void SetInstanceData(ItemInstanceData itemInstance)
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

        UpdateStatUI();
    }

    private void UpdateStatUI()
    {
        if (!InstanceData.IsValid) return;

        bool isScrap = InstanceData.itemData != null && InstanceData.itemData.promptDisplayType == ItemPromptDisplayType.ScrapMetal;

        if (genericPanel != null) genericPanel.SetActive(!isScrap);
        if (scrapPanel != null) scrapPanel.SetActive(isScrap);

        if (isScrap)
        {
            if (scrapAmountText != null)
            {
                scrapAmountText.text = InstanceData.AmountDisplayText;
                scrapAmountText.gameObject.SetActive(InstanceData.ShouldShowAmountText);
            }

            // Hide the shared amountText (generic) if it overlaps or is used for Generic
            if (amountText != null && amountText != scrapAmountText)
                amountText.gameObject.SetActive(false);
            
            return;
        }

        // Generic logic
        bool showStats = !InstanceData.UsesAmountValue;

        // statsRoot should be specifically for durability/weight bars, not the whole item!
        // We find it dynamically to avoid deactivating parents
        Transform statsRoot = transform.Find("ItemStats");

        if (statsRoot != null)
        {
            statsRoot.gameObject.SetActive(showStats);
        }

        if (amountText != null)
        {
            amountText.text = InstanceData.AmountDisplayText;
            amountText.gameObject.SetActive(InstanceData.ShouldShowAmountText);
        }

        if (!showStats)
        {
            return;
        }

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
            weightText.gameObject.SetActive(InstanceData.weightKg > 0.001f);
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
        if (defaultSprite != null) return defaultSprite;

        if (s_DefaultSprite == null)
        {
            string[] paths = { "UI/Skin/UISprite.psd", "UI/Skin/Background.psd", "UI/Skin/UISprite.png", "UI/Skin/Background.png" };
            foreach (string path in paths)
            {
                try
                {
                    s_DefaultSprite = Resources.GetBuiltinResource<Sprite>(path);
                    if (s_DefaultSprite != null) break;
                }
                catch { }
            }

            if (s_DefaultSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                s_DefaultSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
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

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            canvas = rootCanvas;
        }

        if (canvas != null)
        {
            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    /// <summary>
    /// Set by InventoryManager when a successful move/swap happens via OnDrop.
    /// Tells OnEndDrag not to snap back to the old parent.
    /// </summary>
    private bool _wasMovedByManager = false;

    /// <summary>
    /// Called by InventoryManager after a successful move/swap so that
    /// OnEndDrag knows not to revert the reparenting.
    /// </summary>
    public void MarkAsMoved()
    {
        _wasMovedByManager = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (_wasMovedByManager)
        {
            // InventoryManager already handled the move via SetItem — just reset the flag
            _wasMovedByManager = false;
        }
        else
        {
            // Check if dropped outside UI (no raycast target hit)
            bool droppedOutside = eventData.pointerCurrentRaycast.gameObject == null;
            
            if (droppedOutside && InstanceHandler.TryGetInstance(out InventoryManager invManager))
            {
                invManager.DropItem(this);
                return; // Dropped into world, don't snap back
            }

            // Drop was cancelled (no valid slot target) — snap back to original parent
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
            Debug.LogError("Failed to get inventory manager for quick move!");
            return;
        }

        inventoryManager.QuickMoveItem(this);
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
            rectTransform.localScale = Vector3.one;
        }
    }

    public void RefreshStatDisplay()
    {
        UpdateStatUI();
    }
}
