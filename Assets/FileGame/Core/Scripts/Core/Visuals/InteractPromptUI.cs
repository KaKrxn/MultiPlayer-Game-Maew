using UnityEngine;
using TMPro;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    public class InteractPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text keyPressText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform itemStatsRoot;
        [SerializeField] private Slider durabilitySlider;
        [SerializeField] private Slider weightSlider;
        [SerializeField] private TMP_Text durabilityValueText;
        [SerializeField] private TMP_Text weightValueText;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Vector3 offsetFromTarget = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float floatingSpeed = 2f;
        [SerializeField] private float floatingHeight = 0.1f;
        [SerializeField] private float uiSmoothTime = 0.1f;

        private InteractionAddon m_LocalInteractionAddon;
        private Transform m_CurrentTarget;
        private Camera m_MainCamera;
        private Vector3 m_CurrentVelocity = Vector3.zero;
        private bool m_IsShowing = false;
        private bool m_ShouldShowItemStats = false;
        private bool m_HideItemStatsAfterAnimation = false;
        private float m_TargetDurabilityValue = 0f;
        private float m_TargetWeightValue = 0f;

        private static Sprite s_DefaultSprite;

        private void Start()
        {
            m_MainCamera = Camera.main;

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (keyPressText == null)
            {
                Transform keyPressTransform = transform.Find("KeyPress");
                if (keyPressTransform != null) keyPressText = keyPressTransform.GetComponent<TMP_Text>();
            }

            if (descText == null)
            {
                Transform descTransform = transform.Find("DescText");
                if (descTransform != null) descText = descTransform.GetComponent<TMP_Text>();
            }

            EnsureItemStatUI();
            ApplyAlwaysOnTop(keyPressText);
            ApplyAlwaysOnTop(descText);
            ApplyAlwaysOnTop(durabilityValueText);
            ApplyAlwaysOnTop(weightValueText);

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
            UpdateItemStatValues(0f, 0f);
            SetItemStatsVisible(false);

            StartCoroutine(FindLocalPlayerAddon());
        }

        private IEnumerator FindLocalPlayerAddon()
        {
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            m_LocalInteractionAddon = playerObject.GetComponentInChildren<InteractionAddon>();

            if (m_LocalInteractionAddon != null)
            {
                m_LocalInteractionAddon.OnFocusChanged += HandleFocusChanged;
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
            }
        }

        private void Update()
        {
            if (m_MainCamera == null) m_MainCamera = Camera.main;

            if (m_IsShowing && m_CurrentTarget != null)
            {
                Vector3 targetPosition = m_CurrentTarget.position + offsetFromTarget;
                float floatOffset = Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;
                targetPosition.y += floatOffset;

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

            if (interactable != null && !string.IsNullOrEmpty(interactable.InteractionPromptText))
            {
                if (interactable is Component comp)
                {
                    m_CurrentTarget = comp.transform;
                }
                else
                {
                    m_CurrentTarget = null;
                }

                if (keyPressText != null) keyPressText.text = "E";
                if (descText != null) descText.text = interactable.InteractionPromptText;

                ConfigureItemStats(interactable as Item);

                if (!m_IsShowing && m_CurrentTarget != null)
                {
                    transform.position = m_CurrentTarget.position + offsetFromTarget;
                }

                m_IsShowing = true;
                StartCoroutine(ShowAnimation());
            }
            else
            {
                m_IsShowing = false;
                m_ShouldShowItemStats = false;
                m_HideItemStatsAfterAnimation = true;
                StartCoroutine(HideAnimation());
            }
        }

        private void ConfigureItemStats(Item item)
        {
            if (item != null)
            {
                EnsureItemStatUI();
                SetItemStatsVisible(true);
                m_ShouldShowItemStats = true;
                m_HideItemStatsAfterAnimation = false;
                m_TargetDurabilityValue = item.Durability;
                m_TargetWeightValue = Mathf.Clamp(item.WeightDebuffPercent, 0f, 100f);

                if (durabilityValueText != null)
                {
                    durabilityValueText.text = item.DurabilityDisplayText;
                }

                if (weightValueText != null)
                {
                    weightValueText.text = item.WeightDisplayText;
                }
            }
            else
            {
                m_TargetDurabilityValue = 0f;
                m_TargetWeightValue = 0f;
                m_ShouldShowItemStats = false;

                if (itemStatsRoot != null && itemStatsRoot.gameObject.activeSelf)
                {
                    m_HideItemStatsAfterAnimation = true;
                    SetItemStatsVisible(true);
                }
                else
                {
                    m_HideItemStatsAfterAnimation = false;
                    SetItemStatsVisible(false);
                }
            }
        }

        private IEnumerator ShowAnimation()
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float startScale = transform.localScale.x;
            float startDurability = durabilitySlider != null ? durabilitySlider.value : 0f;
            float startWeight = weightSlider != null ? weightSlider.value : 0f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curve = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, curve);
                UpdateItemStatValues(
                    Mathf.Lerp(startDurability, m_ShouldShowItemStats ? m_TargetDurabilityValue : 0f, curve),
                    Mathf.Lerp(startWeight, m_ShouldShowItemStats ? m_TargetWeightValue : 0f, curve));
                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
            UpdateItemStatValues(m_ShouldShowItemStats ? m_TargetDurabilityValue : 0f, m_ShouldShowItemStats ? m_TargetWeightValue : 0f);

            if (!m_ShouldShowItemStats && m_HideItemStatsAfterAnimation)
            {
                SetItemStatsVisible(false);
                m_HideItemStatsAfterAnimation = false;
            }
        }

        private IEnumerator HideAnimation()
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float startScale = transform.localScale.x;
            float startDurability = durabilitySlider != null ? durabilitySlider.value : 0f;
            float startWeight = weightSlider != null ? weightSlider.value : 0f;
            float elapsed = 0f;

            if (itemStatsRoot != null && !itemStatsRoot.gameObject.activeSelf)
            {
                SetItemStatsVisible(true);
            }

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curve = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.zero, curve);
                UpdateItemStatValues(
                    Mathf.Lerp(startDurability, 0f, curve),
                    Mathf.Lerp(startWeight, 0f, curve));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
            UpdateItemStatValues(0f, 0f);
            SetItemStatsVisible(false);
            m_HideItemStatsAfterAnimation = false;
        }

        private void EnsureItemStatUI()
        {
            if (itemStatsRoot != null && durabilitySlider != null && weightSlider != null && durabilityValueText != null && weightValueText != null)
            {
                return;
            }

            if (itemStatsRoot == null)
            {
                itemStatsRoot = transform.Find("ItemStats") as RectTransform;
            }

            if (itemStatsRoot == null)
            {
                GameObject statsRootObject = new GameObject("ItemStats", typeof(RectTransform));
                itemStatsRoot = statsRootObject.GetComponent<RectTransform>();
                itemStatsRoot.SetParent(transform, false);
                itemStatsRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;

                VerticalLayoutGroup rootLayout = itemStatsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                rootLayout.childAlignment = TextAnchor.MiddleLeft;
                rootLayout.childControlHeight = true;
                rootLayout.childControlWidth = true;
                rootLayout.childForceExpandHeight = false;
                rootLayout.childForceExpandWidth = true;
                rootLayout.spacing = 3f;

                ContentSizeFitter fitter = itemStatsRoot.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            TMP_FontAsset fontAsset = descText != null ? descText.font : (keyPressText != null ? keyPressText.font : null);

            if (durabilitySlider == null || durabilityValueText == null)
            {
                CreateStatRow("DurabilityRow", "D", new Color(0.35f, 0.8f, 0.35f, 1f), out durabilitySlider, out durabilityValueText, fontAsset);
            }

            if (weightSlider == null || weightValueText == null)
            {
                CreateStatRow("WeightRow", "W", new Color(0.95f, 0.62f, 0.18f, 1f), out weightSlider, out weightValueText, fontAsset);
            }
        }

        private void CreateStatRow(string rowName, string labelText, Color fillColor, out Slider slider, out TMP_Text valueText, TMP_FontAsset fontAsset)
        {
            GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.SetParent(itemStatsRoot, false);

            HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.spacing = 6f;

            ContentSizeFitter rowFitter = rowObject.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = CreatePromptText("Label", rowRect, fontAsset, labelText, TextAlignmentOptions.MidlineLeft, 16f);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 18f;
            ApplyAlwaysOnTop(label);

            slider = CreatePromptSlider("Slider", rowRect, fillColor);
            LayoutElement sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = 90f;

            valueText = CreatePromptText("Value", rowRect, fontAsset, string.Empty, TextAlignmentOptions.MidlineRight, 16f);
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 48f;
            ApplyAlwaysOnTop(valueText);
        }

        private TMP_Text CreatePromptText(string objectName, RectTransform parent, TMP_FontAsset fontAsset, string textValue, TextAlignmentOptions alignment, float size)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.sizeDelta = new Vector2(40f, 18f);

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = textValue;
            return text;
        }

        private Slider CreatePromptSlider(string objectName, RectTransform parent, Color fillColor)
        {
            GameObject sliderObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.SetParent(parent, false);
            sliderRect.sizeDelta = new Vector2(90f, 14f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;

            Image background = sliderObject.AddComponent<Image>();
            background.sprite = GetDefaultSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

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
            fillRect.anchorMax = Vector2.one;
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

        private void SetItemStatsVisible(bool visible)
        {
            if (itemStatsRoot != null)
            {
                itemStatsRoot.gameObject.SetActive(visible);
            }
        }

        private void UpdateItemStatValues(float durabilityValue, float weightValue)
        {
            if (durabilitySlider != null)
            {
                durabilitySlider.minValue = 0f;
                durabilitySlider.maxValue = 100f;
                durabilitySlider.value = Mathf.Clamp(durabilityValue, 0f, 100f);
            }

            if (weightSlider != null)
            {
                weightSlider.minValue = 0f;
                weightSlider.maxValue = 100f;
                weightSlider.value = Mathf.Clamp(weightValue, 0f, 100f);
            }
        }

        private void ApplyAlwaysOnTop(TMP_Text text)
        {
            if (text == null) return;

            Material mat = text.fontMaterial;
            if (mat != null)
            {
                mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        private Sprite GetDefaultSprite()
        {
            if (s_DefaultSprite == null)
            {
                s_DefaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            }

            return s_DefaultSprite;
        }
    }
}
