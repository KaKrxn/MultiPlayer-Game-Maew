using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using FileGame.Core;

namespace Blocks.Gameplay.Core
{
    public class InteractPromptUI : MonoBehaviour
    {
        [Header("Prefab Roots")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform genericPromptRoot;
        [SerializeField] private RectTransform itemPromptRoot;
        [SerializeField] private RectTransform scrapPromptRoot;
        [SerializeField] private RectTransform trainPromptRoot;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Vector3 offsetFromTarget = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float floatingSpeed = 2f;
        [SerializeField] private float floatingHeight = 0.1f;
        [SerializeField] private float uiSmoothTime = 0.1f;
        [SerializeField] private float sliderLerpSpeed = 10f;
        [SerializeField] private float screenEdgePadding = 40f;
        [SerializeField] private int overlaySortingOrder = 300;
        [SerializeField] private Sprite defaultSprite;

        private InteractionAddon m_LocalInteractionAddon;
        private IInteractable m_CurrentInteractable;
        private Transform m_CurrentTarget;
        private Camera m_MainCamera;
        private TMP_FontAsset m_FontAsset;
        private RectTransform m_RectTransform;
        private Canvas m_RuntimeCanvas;
        private RectTransform m_RuntimeCanvasRect;
        private Vector3 m_CurrentVelocity = Vector3.zero;
        private Vector2 m_ScreenVelocity = Vector2.zero;
        private bool m_UsingScreenSpaceCanvas;
        private bool m_IsShowing;
        private bool m_IsHoldingCurrentInteractable;
        private bool m_IsEatingItem;
        private float m_CurrentHoldProgress;

        private readonly PromptBlockBindings m_GenericPrompt = new PromptBlockBindings();
        private readonly PromptBlockBindings m_ItemPrompt = new PromptBlockBindings();
        private readonly PromptBlockBindings m_ScrapPrompt = new PromptBlockBindings();
        private readonly PromptBlockBindings m_TrainPrompt = new PromptBlockBindings();
        private readonly StatPanelBindings m_ItemStats = new StatPanelBindings();
        private readonly ScrapPanelBindings m_ScrapPanel = new ScrapPanelBindings();
        private readonly TrainPanelBindings m_TrainPanel = new TrainPanelBindings();

        private static Sprite s_DefaultSprite;
        private static Canvas s_SharedOverlayCanvas;

        private void Start()
        {
            m_MainCamera = Camera.main;
            m_RectTransform = transform as RectTransform;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            InitializeCanvasContext();
            AutoBindHierarchy();
            HideAllPanels();

            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;

            PlayerItemUseSystem.OnItemEatingProgress += HandleItemEatingProgress;
            StartCoroutine(FindLocalPlayerAddon());
        }

        private IEnumerator FindLocalPlayerAddon()
        {
            while (NetworkManager.Singleton == null ||
                   !NetworkManager.Singleton.IsConnectedClient ||
                   NetworkManager.Singleton.LocalClient.PlayerObject == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            m_LocalInteractionAddon = playerObject.GetComponentInChildren<InteractionAddon>();

            if (m_LocalInteractionAddon != null)
            {
                m_LocalInteractionAddon.OnFocusChanged += HandleFocusChanged;
                m_LocalInteractionAddon.OnHoldStateChanged += HandleHoldStateChanged;
            }
            else
            {
                Debug.LogWarning("[InteractPromptUI] Failed to find local InteractionAddon on the player.");
            }
        }

        private void OnDestroy()
        {
            if (m_LocalInteractionAddon != null)
            {
                m_LocalInteractionAddon.OnFocusChanged -= HandleFocusChanged;
                m_LocalInteractionAddon.OnHoldStateChanged -= HandleHoldStateChanged;
            }
            PlayerItemUseSystem.OnItemEatingProgress -= HandleItemEatingProgress;
        }

        private void Update()
        {
            if (m_MainCamera == null)
            {
                m_MainCamera = Camera.main;
            }

            if (!m_IsShowing || (m_CurrentTarget == null && !m_IsEatingItem))
            {
                return;
            }

            RefreshPromptContents();

            Vector3 targetPosition;
            if (m_IsEatingItem && m_CurrentTarget == null)
            {
                // If eating from hotbar and no world target, show slightly in front of camera
                if (m_MainCamera == null) return;
                targetPosition = m_MainCamera.transform.position + m_MainCamera.transform.forward * 1.5f;
            }
            else if (m_CurrentTarget != null)
            {
                targetPosition = m_CurrentTarget.position + offsetFromTarget;
            }
            else
            {
                return;
            }
            
            targetPosition.y += Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;

            if (m_UsingScreenSpaceCanvas)
            {
                UpdateScreenSpacePosition(targetPosition);
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_CurrentVelocity, uiSmoothTime);

                if (m_MainCamera != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - m_MainCamera.transform.position);
                }
            }
        }

        private void HandleFocusChanged(IInteractable interactable)
        {
            StopAllCoroutines();
            m_CurrentInteractable = interactable;
            m_IsHoldingCurrentInteractable = false;
            m_CurrentHoldProgress = 0f;
            ResetAnimatedValues();

            if (interactable != null && !string.IsNullOrEmpty(interactable.InteractionPromptText))
            {
                m_CurrentTarget = interactable is Component component ? component.transform : null;

                RefreshPromptContents();

                if (!m_IsShowing && m_CurrentTarget != null && !m_UsingScreenSpaceCanvas)
                {
                    transform.position = m_CurrentTarget.position + offsetFromTarget;
                }

                m_IsShowing = m_CurrentTarget != null;
                StartCoroutine(ShowAnimation());
                return;
            }

            m_CurrentTarget = null;
            m_IsShowing = false;
            StartCoroutine(HideAnimation());
        }

        private void HandleHoldStateChanged(IInteractable interactable, float progress, bool isHolding)
        {
            if (interactable != m_CurrentInteractable)
            {
                return;
            }

            m_IsHoldingCurrentInteractable = isHolding;
            m_CurrentHoldProgress = Mathf.Clamp01(progress);
            RefreshPromptContents();
        }

        private void HandleItemEatingProgress(float progress, bool isEating)
        {
            m_IsEatingItem = isEating;
            
            // Only update if we aren't already world-interacting (Interaction has priority)
            if (!m_IsHoldingCurrentInteractable)
            {
                m_CurrentHoldProgress = progress;
                
                if (isEating && !m_IsShowing)
                {
                    m_IsShowing = true;
                    StopAllCoroutines();
                    StartCoroutine(ShowAnimation());
                }
                else if (!isEating && m_IsShowing && m_CurrentInteractable == null)
                {
                    m_IsShowing = false;
                    StopAllCoroutines();
                    StartCoroutine(HideAnimation());
                }
                
                RefreshPromptContents();
            }
        }

        private void RefreshPromptContents()
        {
            if (m_CurrentInteractable == null && !m_IsEatingItem)
            {
                return;
            }

            InteractionPromptContext context = new InteractionPromptContext(
                GetLocalInteractor(),
                (m_CurrentInteractable != null && m_CurrentInteractable is IHoldInteractable) || m_IsEatingItem,
                m_IsHoldingCurrentInteractable || m_IsEatingItem,
                m_CurrentHoldProgress);

            InteractionPromptViewData viewData = BuildFallbackView(context);
            if (m_CurrentInteractable != null && m_CurrentInteractable is IInteractionPromptViewProvider provider &&
                provider.TryBuildPromptView(context, out InteractionPromptViewData providedView))
            {
                viewData = providedView;
            }

            BindView(viewData);
        }

        private InteractionPromptViewData BuildFallbackView(InteractionPromptContext context)
        {
            if (m_IsEatingItem && m_CurrentInteractable == null)
            {
                return new InteractionPromptViewData("Hold LMB", "Eating...", InteractionPromptVariant.Generic)
                {
                    ShowHoldProgress = true,
                    HoldProgress = context.HoldProgress
                };
            }

            string keyText = context.IsHoldInteractable ? "Hold E" : "E";
            if (m_CurrentInteractable is IInteractionPromptDetailsProvider detailsProvider)
            {
                string customKeyText = detailsProvider.GetPromptKeyText(context.Interactor);
                if (!string.IsNullOrWhiteSpace(customKeyText))
                {
                    keyText = customKeyText;
                }
            }

            return new InteractionPromptViewData(keyText, m_CurrentInteractable.InteractionPromptText, InteractionPromptVariant.Generic)
            {
                ShowHoldProgress = context.IsHoldInteractable,
                HoldProgress = context.HoldProgress
            };
        }

        private void BindView(InteractionPromptViewData viewData)
        {
            HideAllPanels();

            switch (viewData.Variant)
            {
                case InteractionPromptVariant.Item:
                    SetActive(itemPromptRoot, true);
                    BindPromptBlock(m_ItemPrompt, viewData);
                    BindItemPanel(viewData.ItemData);
                    break;

                case InteractionPromptVariant.ScrapMetalItem:
                    SetActive(scrapPromptRoot, true);
                    BindPromptBlock(m_ScrapPrompt, viewData);
                    BindScrapPanel(viewData.ItemData);
                    break;

                case InteractionPromptVariant.TrainUpgrade:
                    SetActive(trainPromptRoot, true);
                    BindPromptBlock(m_TrainPrompt, viewData);
                    BindTrainPanel(viewData.TrainData);
                    break;

                default:
                    SetActive(genericPromptRoot, true);
                    BindPromptBlock(m_GenericPrompt, viewData);
                    break;
            }
        }

        private void BindPromptBlock(PromptBlockBindings bindings, InteractionPromptViewData viewData)
        {
            if (bindings.KeyText != null)
            {
                bindings.KeyText.text = BuildDisplayKeyText(viewData);
            }

            if (bindings.DescriptionText != null)
            {
                bindings.DescriptionText.text = viewData.DescriptionText;
            }
        }

        private void BindItemPanel(InteractionPromptItemData itemData)
        {
            if (!itemData.IsValid)
            {
                SetActive(m_ItemStats.Root, false);
                return;
            }

            if (m_ItemStats.Icon != null)
            {
                m_ItemStats.Icon.sprite = itemData.Icon;
                m_ItemStats.Icon.enabled = itemData.Icon != null;
            }

            SetActive(m_ItemStats.Root, true);
            SetActive(m_ItemStats.DurabilityRow, itemData.ShowDurability);
            SetActive(m_ItemStats.WeightRow, itemData.ShowWeight);

            SetSliderValue(m_ItemStats.DurabilitySlider, itemData.DurabilityNormalized);
            SetSliderValue(m_ItemStats.WeightSlider, itemData.WeightNormalized);

            if (m_ItemStats.DurabilityValueText != null)
            {
                m_ItemStats.DurabilityValueText.text = itemData.DurabilityText;
            }

            if (m_ItemStats.WeightValueText != null)
            {
                m_ItemStats.WeightValueText.text = itemData.WeightText;
            }
        }

        private void BindScrapPanel(InteractionPromptItemData itemData)
        {
            if (m_ScrapPanel.Icon != null)
            {
                m_ScrapPanel.Icon.sprite = itemData.Icon;
                m_ScrapPanel.Icon.enabled = itemData.Icon != null;
            }

            if (m_ScrapPanel.AmountText != null)
            {
                m_ScrapPanel.AmountText.text = itemData.ShowAmount ? itemData.AmountText : string.Empty;
            }
        }

        private void BindTrainPanel(InteractionPromptTrainData trainData)
        {
            if (!trainData.IsValid)
            {
                return;
            }

            SetSliderValue(m_TrainPanel.HealthSlider, trainData.HealthNormalized);
            SetSliderValue(m_TrainPanel.FuelSlider, trainData.FuelNormalized);

            if (m_TrainPanel.HealthValueText != null)
            {
                m_TrainPanel.HealthValueText.text = $"Health {trainData.HealthText}";
            }

            if (m_TrainPanel.FuelValueText != null)
            {
                m_TrainPanel.FuelValueText.text = $"Fuel {trainData.FuelText}";
            }

            EnsureCompareRows();

            if (m_TrainPanel.CurrentLevelText != null)
            {
                m_TrainPanel.CurrentLevelText.text = trainData.CurrentLevelLabel;
            }

            if (m_TrainPanel.NextLevelText != null)
            {
                m_TrainPanel.NextLevelText.text = trainData.NextLevelLabel;
            }

            BindCompareRows(m_TrainPanel.CurrentCompareRows, trainData.CompareRows, false);
            BindCompareRows(m_TrainPanel.NextCompareRows, trainData.CompareRows, true);

            InteractionPromptRequirementData requirement = trainData.Requirements != null && trainData.Requirements.Length > 0
                ? trainData.Requirements[0]
                : default(InteractionPromptRequirementData);

            bool showRequirement = requirement.IsValid;
            SetActive(m_TrainPanel.RequirementRoot, showRequirement);
            if (showRequirement)
            {
                if (m_TrainPanel.RequirementIcon != null)
                {
                    m_TrainPanel.RequirementIcon.sprite = requirement.Icon;
                    m_TrainPanel.RequirementIcon.enabled = requirement.Icon != null;
                }

                if (m_TrainPanel.RequirementAmountText != null)
                {
                    m_TrainPanel.RequirementAmountText.text = requirement.AmountText;
                    m_TrainPanel.RequirementAmountText.color = requirement.AmountColor;
                }
            }
        }

        private void EnsureCompareRows()
        {
            if (m_TrainPanel.CurrentCompareRows.Count == 0)
            {
                m_TrainPanel.CurrentLevelText = EnsureCompareHeader(m_TrainPanel.CurrentCompareRoot, "Current");
                CreateCompareRows(m_TrainPanel.CurrentCompareRoot, m_TrainPanel.CurrentCompareRows, false);
            }

            if (m_TrainPanel.NextCompareRows.Count == 0)
            {
                m_TrainPanel.NextLevelText = EnsureCompareHeader(m_TrainPanel.NextCompareRoot, "Next");
                CreateCompareRows(m_TrainPanel.NextCompareRoot, m_TrainPanel.NextCompareRows, true);
            }
        }

        private TMP_Text EnsureCompareHeader(RectTransform panelRoot, string defaultText)
        {
            if (panelRoot == null)
            {
                return null;
            }

            TMP_Text header = FindDescendantText(panelRoot, "LevelText");
            if (header != null)
            {
                return header;
            }

            GameObject headerObject = new GameObject("LevelText", typeof(RectTransform));
            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.SetParent(panelRoot, false);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -8f);
            headerRect.sizeDelta = new Vector2(0f, 22f);

            TextMeshProUGUI headerText = headerObject.AddComponent<TextMeshProUGUI>();
            headerText.font = m_FontAsset;
            headerText.fontSize = 16f;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = Color.white;
            headerText.raycastTarget = false;
            headerText.text = defaultText;
            ApplyAlwaysOnTop(headerText);
            return headerText;
        }

        private void CreateCompareRows(RectTransform panelRoot, List<CompareRowBindings> targetRows, bool createUpgradeLayer)
        {
            if (panelRoot == null)
            {
                return;
            }

            const float startY = -34f;
            const float rowHeight = 22f;
            const float rowSpacing = 6f;

            for (int index = 0; index < 4; index++)
            {
                GameObject rowObject = new GameObject("StatRow_" + index, typeof(RectTransform));
                RectTransform rowRect = rowObject.GetComponent<RectTransform>();
                rowRect.SetParent(panelRoot, false);
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, startY - index * (rowHeight + rowSpacing));
                rowRect.sizeDelta = new Vector2(-10f, rowHeight);

                Image background = rowObject.AddComponent<Image>();
                background.sprite = GetDefaultSprite();
                background.type = Image.Type.Sliced;
                background.color = new Color(0.16f, 0.16f, 0.16f, 0.92f);

                RectTransform upgradeFillRect = null;
                Image upgradeFillImage = null;
                if (createUpgradeLayer)
                {
                    upgradeFillRect = CreateFillRect(rowRect, "UpgradeFill");
                    upgradeFillImage = upgradeFillRect.GetComponent<Image>();
                    upgradeFillImage.color = new Color(0.45f, 1f, 0.45f, 0.95f);
                }

                RectTransform currentFillRect = CreateFillRect(rowRect, "CurrentFill");
                Image currentFillImage = currentFillRect.GetComponent<Image>();
                currentFillImage.color = new Color(0.72f, 0.72f, 0.72f, 0.95f);

                TMP_Text labelText = CreateOverlayText(rowRect, "Label", TextAlignmentOptions.MidlineLeft, 13f, new Vector2(6f, 0f), new Vector2(70f, rowHeight));
                TMP_Text valueText = CreateOverlayText(rowRect, "Value", TextAlignmentOptions.MidlineRight, 13f, new Vector2(-6f, 0f), new Vector2(78f, rowHeight), new Vector2(1f, 0.5f));

                targetRows.Add(new CompareRowBindings
                {
                    Root = rowRect,
                    CurrentFillRect = currentFillRect,
                    CurrentFillImage = currentFillImage,
                    UpgradeFillRect = upgradeFillRect,
                    UpgradeFillImage = upgradeFillImage,
                    LabelText = labelText,
                    ValueText = valueText,
                    UsesUpgradeLayer = createUpgradeLayer
                });
            }
        }

        private RectTransform CreateFillRect(RectTransform parent, string objectName)
        {
            GameObject fillObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(parent, false);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(0f, -2f);

            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.sprite = GetDefaultSprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;
            return fillRect;
        }

        private TMP_Text CreateOverlayText(
            RectTransform parent,
            string objectName,
            TextAlignmentOptions alignment,
            float fontSize,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? anchorOverride = null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = anchorOverride ?? (alignment == TextAlignmentOptions.MidlineRight ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f));
            textRect.anchorMax = textRect.anchorMin;
            textRect.pivot = textRect.anchorMin;
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = m_FontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            ApplyAlwaysOnTop(text);
            return text;
        }

        private void BindCompareRows(List<CompareRowBindings> rows, InteractionPromptStatCompareRow[] compareRows, bool useNextValues)
        {
            if (compareRows == null)
            {
                return;
            }

            int count = Mathf.Min(rows.Count, compareRows.Length);
            for (int index = 0; index < count; index++)
            {
                InteractionPromptStatCompareRow rowData = compareRows[index];
                CompareRowBindings row = rows[index];
                Color rowColor = useNextValues ? rowData.DeltaColor : new Color(0.82f, 0.82f, 0.82f, 1f);

                if (row.LabelText != null)
                {
                    row.LabelText.text = rowData.Label;
                    row.LabelText.color = rowColor;
                }

                if (row.ValueText != null)
                {
                    row.ValueText.text = useNextValues ? rowData.NextValueText : rowData.CurrentValueText;
                    row.ValueText.color = rowColor;
                }

                if (row.UsesUpgradeLayer)
                {
                    if (row.UpgradeFillImage != null)
                    {
                        row.UpgradeFillImage.color = rowData.DeltaColor;
                    }

                    if (row.CurrentFillImage != null)
                    {
                        row.CurrentFillImage.color = new Color(0.72f, 0.72f, 0.72f, 0.95f);
                    }

                    SetFillRect(row.UpgradeFillRect, rowData.NextNormalized);
                    SetFillRect(row.CurrentFillRect, rowData.CurrentNormalized);
                    UpdateCompareFillOrder(row, rowData.CurrentNormalized, rowData.NextNormalized);
                }
                else
                {
                    if (row.CurrentFillImage != null)
                    {
                        row.CurrentFillImage.color = new Color(0.72f, 0.72f, 0.72f, 0.95f);
                    }

                    SetFillRect(row.CurrentFillRect, rowData.CurrentNormalized);
                }
            }
        }

        private void UpdateCompareFillOrder(CompareRowBindings row, float currentNormalized, float nextNormalized)
        {
            if (row == null || row.CurrentFillRect == null || row.UpgradeFillRect == null)
            {
                return;
            }

            RectTransform backFill = row.UpgradeFillRect;
            RectTransform frontFill = row.CurrentFillRect;

            if (nextNormalized < currentNormalized)
            {
                backFill = row.CurrentFillRect;
                frontFill = row.UpgradeFillRect;
            }

            backFill.SetSiblingIndex(0);
            frontFill.SetSiblingIndex(1);
        }

        private void InitializeCanvasContext()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                Canvas overlayCanvas = GetOrCreateOverlayCanvas();
                if (overlayCanvas != null)
                {
                    transform.SetParent(overlayCanvas.transform, false);
                    parentCanvas = overlayCanvas;
                }
            }

            m_RuntimeCanvas = parentCanvas;
            m_RuntimeCanvasRect = m_RuntimeCanvas != null ? m_RuntimeCanvas.transform as RectTransform : null;
            m_UsingScreenSpaceCanvas = m_RuntimeCanvas != null && m_RuntimeCanvas.renderMode != RenderMode.WorldSpace;
        }

        private Canvas GetOrCreateOverlayCanvas()
        {
            if (s_SharedOverlayCanvas != null)
            {
                return s_SharedOverlayCanvas;
            }

            GameObject canvasObject = new GameObject("InteractPromptOverlayCanvas", typeof(RectTransform));
            Canvas overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = overlaySortingOrder;
            overlayCanvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObject);

            s_SharedOverlayCanvas = overlayCanvas;
            return s_SharedOverlayCanvas;
        }

        private void UpdateScreenSpacePosition(Vector3 worldPosition)
        {
            if (m_MainCamera == null || m_RectTransform == null || m_RuntimeCanvasRect == null)
            {
                return;
            }

            Camera eventCamera = m_RuntimeCanvas != null && m_RuntimeCanvas.renderMode == RenderMode.ScreenSpaceCamera
                ? m_RuntimeCanvas.worldCamera
                : null;

            Vector3 screenPoint = m_MainCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            if (m_IsShowing && canvasGroup.alpha <= 0f)
            {
                canvasGroup.alpha = 1f;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RuntimeCanvasRect, screenPoint, eventCamera, out localPoint))
            {
                return;
            }

            Vector2 halfCanvas = m_RuntimeCanvasRect.rect.size * 0.5f;
            Vector2 halfPrompt = m_RectTransform.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + halfPrompt.x + screenEdgePadding, halfCanvas.x - halfPrompt.x - screenEdgePadding);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y + halfPrompt.y + screenEdgePadding, halfCanvas.y - halfPrompt.y - screenEdgePadding);

            m_RectTransform.anchoredPosition = Vector2.SmoothDamp(m_RectTransform.anchoredPosition, localPoint, ref m_ScreenVelocity, uiSmoothTime);
        }

        private void AutoBindHierarchy()
        {
            if (genericPromptRoot == null)
            {
                genericPromptRoot = FindDirectChildRect(transform, "InteractionText");
            }

            if (itemPromptRoot == null)
            {
                itemPromptRoot = FindDirectChildByContains(transform, "GenericDropItem");
            }

            if (scrapPromptRoot == null)
            {
                scrapPromptRoot = FindDirectChildByContains(transform, "ScrapDropItem");
            }

            if (trainPromptRoot == null)
            {
                trainPromptRoot = FindDirectChildRect(transform, "TrainUI");
            }

            TMP_Text fontReference = GetComponentInChildren<TMP_Text>(true);
            m_FontAsset = fontReference != null ? fontReference.font : null;

            BindPromptBlock(genericPromptRoot, m_GenericPrompt);
            BindPromptBlock(FindDescendantRect(itemPromptRoot, "InteractionText"), m_ItemPrompt);
            BindPromptBlock(FindDescendantRect(scrapPromptRoot, "InteractionText"), m_ScrapPrompt);
            BindPromptBlock(FindDescendantRect(trainPromptRoot, "InteractionText"), m_TrainPrompt);

            BindItemPanelReferences();
            BindScrapPanelReferences();
            BindTrainPanelReferences();
        }

        private void BindPromptBlock(RectTransform root, PromptBlockBindings bindings)
        {
            bindings.Root = root;
            bindings.KeyText = FindTextWithHint(root, "Key");
            bindings.DescriptionText = FindTextWithHint(root, "Desc");

            ApplyAlwaysOnTop(bindings.KeyText);
            ApplyAlwaysOnTop(bindings.DescriptionText);
        }

        private void BindItemPanelReferences()
        {
            m_ItemStats.Root = FindDescendantRect(itemPromptRoot, "Weight/Durability");
            m_ItemStats.Icon = FindNamedImage(itemPromptRoot, "Sprite (1)");
            m_ItemStats.DurabilitySlider = FindDescendantSlider(m_ItemStats.Root, "DSlider");
            m_ItemStats.WeightSlider = FindDescendantSlider(m_ItemStats.Root, "WSlider");
            m_ItemStats.DurabilityRow = m_ItemStats.DurabilitySlider != null ? m_ItemStats.DurabilitySlider.transform.parent.gameObject : null;
            m_ItemStats.WeightRow = m_ItemStats.WeightSlider != null ? m_ItemStats.WeightSlider.transform.parent.gameObject : null;
            m_ItemStats.DurabilityValueText = m_ItemStats.DurabilityRow != null ? m_ItemStats.DurabilityRow.GetComponentInChildren<TMP_Text>(true) : null;
            m_ItemStats.WeightValueText = m_ItemStats.WeightRow != null ? m_ItemStats.WeightRow.GetComponentInChildren<TMP_Text>(true) : null;

            ApplyAlwaysOnTop(m_ItemStats.DurabilityValueText);
            ApplyAlwaysOnTop(m_ItemStats.WeightValueText);
        }

        private void BindScrapPanelReferences()
        {
            m_ScrapPanel.Icon = FindNamedImage(scrapPromptRoot, "Sprite (1)");
            RectTransform amountRoot = FindDescendantRect(scrapPromptRoot, "Weight/Durability");
            if (amountRoot != null)
            {
                TMP_Text[] texts = amountRoot.GetComponentsInChildren<TMP_Text>(true);
                m_ScrapPanel.AmountText = texts.Length > 0 ? texts[0] : null;
            }

            ApplyAlwaysOnTop(m_ScrapPanel.AmountText);
        }

        private void BindTrainPanelReferences()
        {
            m_TrainPanel.HealthSlider = FindDescendantSlider(trainPromptRoot, "Slider");
            m_TrainPanel.FuelSlider = FindDescendantSlider(trainPromptRoot, "Slider (1)");
            m_TrainPanel.HealthValueText = m_TrainPanel.HealthSlider != null ? m_TrainPanel.HealthSlider.GetComponentInChildren<TMP_Text>(true) : null;
            m_TrainPanel.FuelValueText = m_TrainPanel.FuelSlider != null ? m_TrainPanel.FuelSlider.GetComponentInChildren<TMP_Text>(true) : null;
            m_TrainPanel.ComparePanelRoot = FindDescendantRect(trainPromptRoot, "ComparingPanel");
            m_TrainPanel.CurrentCompareRoot = FindDescendantRect(m_TrainPanel.ComparePanelRoot, "1stPanel");
            m_TrainPanel.NextCompareRoot = FindDescendantRect(m_TrainPanel.ComparePanelRoot, "1stPanel (1)");
            m_TrainPanel.RequirementRoot = FindDescendantRect(trainPromptRoot, "Requirement");

            if (m_TrainPanel.RequirementRoot != null)
            {
                Image[] images = m_TrainPanel.RequirementRoot.GetComponentsInChildren<Image>(true);
                m_TrainPanel.RequirementIcon = images.Length > 1 ? images[1] : null;
                TMP_Text[] texts = m_TrainPanel.RequirementRoot.GetComponentsInChildren<TMP_Text>(true);
                m_TrainPanel.RequirementAmountText = texts.Length > 0 ? texts[0] : null;
            }

            ApplyAlwaysOnTop(m_TrainPanel.HealthValueText);
            ApplyAlwaysOnTop(m_TrainPanel.FuelValueText);
            ApplyAlwaysOnTop(m_TrainPanel.RequirementAmountText);
        }

        private IEnumerator ShowAnimation()
        {
            float startAlpha = canvasGroup.alpha;
            float startScale = transform.localScale.x;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curve = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, curve);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private IEnumerator HideAnimation()
        {
            float startAlpha = canvasGroup.alpha;
            float startScale = transform.localScale.x;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curve = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.zero, curve);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
            HideAllPanels();
            ResetAnimatedValues();
        }

        private void HideAllPanels()
        {
            SetActive(genericPromptRoot, false);
            SetActive(itemPromptRoot, false);
            SetActive(scrapPromptRoot, false);
            SetActive(trainPromptRoot, false);
        }

        private void ResetAnimatedValues()
        {
            ResetSlider(m_ItemStats.DurabilitySlider);
            ResetSlider(m_ItemStats.WeightSlider);
            ResetSlider(m_TrainPanel.HealthSlider);
            ResetSlider(m_TrainPanel.FuelSlider);
            ResetCompareRows(m_TrainPanel.CurrentCompareRows);
            ResetCompareRows(m_TrainPanel.NextCompareRows);
        }

        private void ResetSlider(Slider slider)
        {
            if (slider != null)
            {
                slider.value = 0f;
            }
        }

        private void ResetCompareRows(List<CompareRowBindings> rows)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                SetFillInstant(rows[index].CurrentFillRect, 0f);
                SetFillInstant(rows[index].UpgradeFillRect, 0f);
            }
        }

        private void SetSliderValue(Slider slider, float normalizedValue)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Lerp(slider.value, Mathf.Clamp01(normalizedValue), 1f - Mathf.Exp(-sliderLerpSpeed * Time.deltaTime));
        }

        private void SetFillRect(RectTransform fillRect, float normalizedValue)
        {
            if (fillRect == null)
            {
                return;
            }

            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = Mathf.Lerp(anchorMax.x, Mathf.Clamp01(normalizedValue), 1f - Mathf.Exp(-sliderLerpSpeed * Time.deltaTime));
            anchorMax.y = 1f;
            fillRect.anchorMax = anchorMax;
        }

        private void SetFillInstant(RectTransform fillRect, float normalizedValue)
        {
            if (fillRect == null)
            {
                return;
            }

            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = Mathf.Clamp01(normalizedValue);
            anchorMax.y = 1f;
            fillRect.anchorMax = anchorMax;
        }

        private string BuildDisplayKeyText(InteractionPromptViewData viewData)
        {
            if (!viewData.ShowHoldProgress || !m_IsHoldingCurrentInteractable)
            {
                return viewData.KeyText;
            }

            return viewData.KeyText + " " + Mathf.RoundToInt(viewData.HoldProgress * 100f) + "%";
        }

        private void SetActive(RectTransform root, bool isActive)
        {
            if (root != null)
            {
                root.gameObject.SetActive(isActive);
            }
        }

        private void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }

        private RectTransform FindDirectChildRect(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private RectTransform FindDirectChildByContains(Transform parent, string partialName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name.Contains(partialName))
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private RectTransform FindDescendantRect(Transform root, string childName)
        {
            Transform child = FindDescendant(root, childName);
            return child as RectTransform;
        }

        private Slider FindDescendantSlider(Transform root, string objectName)
        {
            Transform child = FindDescendant(root, objectName);
            return child != null ? child.GetComponent<Slider>() : null;
        }

        private Image FindNamedImage(Transform root, string objectName)
        {
            Transform child = FindDescendant(root, objectName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindDescendant(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private TMP_Text FindTextWithHint(Transform root, string hint)
        {
            if (root == null)
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                TMP_Text text = texts[index];
                string objectName = text.gameObject.name;
                string parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;
                if (objectName.Contains(hint) || parentName.Contains(hint))
                {
                    return text;
                }
            }

            return texts.Length > 0 ? texts[0] : null;
        }

        private TMP_Text FindDescendantText(Transform root, string objectName)
        {
            Transform child = FindDescendant(root, objectName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private void ApplyAlwaysOnTop(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            Material material = text.fontMaterial;
            if (material != null)
            {
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        private Sprite GetDefaultSprite()
        {
            if (defaultSprite != null)
            {
                return defaultSprite;
            }

            if (s_DefaultSprite == null)
            {
                string[] paths =
                {
                    "UI/Skin/UISprite.psd",
                    "UI/Skin/Background.psd",
                    "UI/Skin/UISprite.png",
                    "UI/Skin/Background.png"
                };

                for (int index = 0; index < paths.Length; index++)
                {
                    try
                    {
                        s_DefaultSprite = Resources.GetBuiltinResource<Sprite>(paths[index]);
                        if (s_DefaultSprite != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                    }
                }

                if (s_DefaultSprite == null)
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.name = "Fallback_White";
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    s_DefaultSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                    s_DefaultSprite.name = "FallbackWhiteSprite";
                }
            }

            return s_DefaultSprite;
        }

        private GameObject GetLocalInteractor()
        {
            return NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.LocalClient != null &&
                   NetworkManager.Singleton.LocalClient.PlayerObject != null
                ? NetworkManager.Singleton.LocalClient.PlayerObject.gameObject
                : null;
        }

        private sealed class PromptBlockBindings
        {
            public RectTransform Root;
            public TMP_Text KeyText;
            public TMP_Text DescriptionText;
        }

        private sealed class StatPanelBindings
        {
            public RectTransform Root;
            public Image Icon;
            public Slider DurabilitySlider;
            public Slider WeightSlider;
            public GameObject DurabilityRow;
            public GameObject WeightRow;
            public TMP_Text DurabilityValueText;
            public TMP_Text WeightValueText;
        }

        private sealed class ScrapPanelBindings
        {
            public Image Icon;
            public TMP_Text AmountText;
        }

        private sealed class TrainPanelBindings
        {
            public Slider HealthSlider;
            public Slider FuelSlider;
            public TMP_Text HealthValueText;
            public TMP_Text FuelValueText;
            public RectTransform ComparePanelRoot;
            public RectTransform CurrentCompareRoot;
            public RectTransform NextCompareRoot;
            public TMP_Text CurrentLevelText;
            public TMP_Text NextLevelText;
            public readonly List<CompareRowBindings> CurrentCompareRows = new List<CompareRowBindings>();
            public readonly List<CompareRowBindings> NextCompareRows = new List<CompareRowBindings>();
            public RectTransform RequirementRoot;
            public Image RequirementIcon;
            public TMP_Text RequirementAmountText;
        }

        private sealed class CompareRowBindings
        {
            public RectTransform Root;
            public RectTransform CurrentFillRect;
            public Image CurrentFillImage;
            public RectTransform UpgradeFillRect;
            public Image UpgradeFillImage;
            public TMP_Text LabelText;
            public TMP_Text ValueText;
            public bool UsesUpgradeLayer;
        }
    }
}
