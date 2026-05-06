using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Client-side HUD displaying the train's fuel level as a smooth slider.
/// Auto-discovers the TrainFuelSystem at runtime via FindObjectOfType.
/// </summary>
public class TrainFuelHUD : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Drag the fuel gauge Slider here")]
    public Slider fuelSlider;

    [Header("Train Reference (Auto-Found)")]
    [Tooltip("Auto-discovered at runtime — no need to assign")]
    public TrainFuelSystem fuelSystem;

    [Tooltip("Smoothing speed for the fuel gauge animation")]
    public float smoothSpeed = 5f;

    private float _targetPercentage = 1f;

    private void Start()
    {
        if (fuelSlider != null)
        {
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 1f;
        }
    }

    private void OnDestroy()
    {
        if (fuelSystem != null)
        {
            fuelSystem.currentFuel.OnValueChanged -= OnFuelChanged;
        }
    }

    private void Update()
    {
        // Auto-discover fuel system if not assigned
        if (fuelSystem == null)
        {
            fuelSystem = FindObjectOfType<TrainFuelSystem>();

            if (fuelSystem == null) return;

            // Subscribe to value changes for accurate sync
            fuelSystem.currentFuel.OnValueChanged += OnFuelChanged;
            
            // Set initial values
            _targetPercentage = fuelSystem.currentFuel.Value / fuelSystem.maxFuel;
            if (fuelSlider != null) fuelSlider.value = _targetPercentage;

            Debug.Log("[TrainFuelHUD] Connected to Train Fuel System.");
        }

        if (fuelSlider == null) return;

        // Smooth lerp for the slider visual
        fuelSlider.value = Mathf.Lerp(fuelSlider.value, _targetPercentage, Time.deltaTime * smoothSpeed);
    }

    private void OnFuelChanged(float previousValue, float newValue)
    {
        if (fuelSystem == null) return;
        _targetPercentage = newValue / fuelSystem.maxFuel;
    }
}