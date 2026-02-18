using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// UI สำหรับแสดงสถานะของรถไฟ (Speed, Fuel, Health)
/// ใส่ไว้บน Canvas หรือ UI GameObject
/// </summary>
public class TrainUI : NetworkBehaviour
{
    [Header("Train Reference")]
    [SerializeField] private TrainNetworkController trainController;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider fuelSlider;
    [SerializeField] private Slider healthSlider;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f; // อัปเดต UI ทุก 0.1 วินาที

    private float lastUpdateTime;

    private void Start()
    {
        // ถ้ายังไม่ได้ assign trainController → หาใน Scene
        if (trainController == null)
        {
            trainController = FindFirstObjectByType<TrainNetworkController>();
        }

        // Setup Sliders
        if (fuelSlider != null && trainController != null)
        {
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = trainController.MaxFuel;
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f; // จะอัปเดตจาก trainController.MaxHealth
        }
    }

    private void Update()
    {
        if (trainController == null) return;

        // อัปเดต UI ตาม interval
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateUI();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateUI()
    {
        // Speed
        if (speedText != null)
        {
            speedText.text = $"Speed: {trainController.CurrentSpeed.Value:F1} m/s";
        }

        // Fuel
        if (fuelText != null)
        {
            fuelText.text = $"Fuel: {trainController.Fuel.Value:F1} / {trainController.MaxFuel:F0}";
        }

        if (fuelSlider != null)
        {
            fuelSlider.value = trainController.Fuel.Value;
            fuelSlider.maxValue = trainController.MaxFuel;
        }

        // Health
        if (healthText != null)
        {
            healthText.text = $"Health: {trainController.Health.Value:F1} / {trainController.MaxHealth:F0}";
        }

        if (healthSlider != null)
        {
            healthSlider.value = trainController.Health.Value;
            healthSlider.maxValue = trainController.MaxHealth;
        }
    }

    /// <summary>
    /// Set Train Controller Reference
    /// </summary>
    public void SetTrainController(TrainNetworkController controller)
    {
        trainController = controller;
    }
}
