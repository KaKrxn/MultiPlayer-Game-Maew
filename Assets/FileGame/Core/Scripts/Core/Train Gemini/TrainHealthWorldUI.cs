using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainHealthWorldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrainUpgradeSystem trainSystem;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Visual Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private Color healthyColor = new Color(0.35f, 1f, 0.35f, 1f);
    [SerializeField] private Color damagedColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float sliderLerpSpeed = 12f;

    private Camera _camera;

    private void Awake()
    {
        if (trainSystem == null)
            trainSystem = GetComponentInParent<TrainUpgradeSystem>();

        if (worldCanvas == null)
            worldCanvas = GetComponentInChildren<Canvas>(true);

        if (canvasGroup == null && worldCanvas != null)
            canvasGroup = worldCanvas.GetComponent<CanvasGroup>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        if (worldCanvas != null)
            worldCanvas.renderMode = RenderMode.WorldSpace;
    }

    private void LateUpdate()
    {
        if (trainSystem == null)
        {
            SetVisible(false);
            return;
        }

        UpdatePosition();
        UpdateHealthView();
    }

    private void UpdatePosition()
    {
        transform.position = trainSystem.transform.position + worldOffset;

        if (_camera == null)
            _camera = Camera.main;

        if (_camera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);
    }

    private void UpdateHealthView()
    {
        float maxHealth = Mathf.Max(0f, trainSystem.MaxHealth);
        float normalized = maxHealth <= 0f ? 0f : Mathf.Clamp01(trainSystem.CurrentHealth / maxHealth);

        SetVisible(maxHealth > 0f);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = Mathf.Lerp(healthSlider.value, normalized, 1f - Mathf.Exp(-sliderLerpSpeed * Time.deltaTime));
        }

        if (fillImage != null)
        {
            fillImage.color = normalized <= 0.25f
                ? criticalColor
                : normalized <= 0.55f
                    ? damagedColor
                    : healthyColor;
        }

        if (healthText != null)
            healthText.text = $"{trainSystem.CurrentHealth:0}/{maxHealth:0}";
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
