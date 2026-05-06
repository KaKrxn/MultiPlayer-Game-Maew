using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private Toggle vSyncToggle;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        ConfigureControls();
        BindEvents();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        GameObject target = panelRoot != null ? panelRoot : gameObject;
        bool shouldOpen = !target.activeSelf;
        target.SetActive(shouldOpen);

        if (shouldOpen)
            Refresh();
    }

    public void Refresh()
    {
        SettingManager manager = SettingManager.Instance;
        if (manager == null)
            return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(manager.MasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(manager.MusicVolume);

        if (displayModeDropdown != null)
            displayModeDropdown.SetValueWithoutNotify(manager.DisplayMode);

        if (vSyncToggle != null)
            vSyncToggle.SetIsOnWithoutNotify(manager.VSync == 1);

        if (qualityDropdown != null)
            qualityDropdown.SetValueWithoutNotify(manager.GraphicsQuality);
    }

    private void ConfigureControls()
    {
        ConfigureVolumeSlider(masterVolumeSlider);
        ConfigureVolumeSlider(musicVolumeSlider);
        ConfigureDropdown(displayModeDropdown, new List<string> { "Fullscreen", "Windowed" });
        ConfigureDropdown(qualityDropdown, new List<string> { "Low", "Medium", "High" });
    }

    private void ConfigureVolumeSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void ConfigureDropdown(TMP_Dropdown dropdown, List<string> options)
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    private void BindEvents()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (displayModeDropdown != null)
            displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);

        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void UnbindEvents()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (displayModeDropdown != null)
            displayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);

        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.RemoveListener(OnGraphicsQualityChanged);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.SetMusicVolume(value);
    }

    private void OnDisplayModeChanged(int value)
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.SetDisplayMode(value);
    }

    private void OnVSyncChanged(bool enabled)
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.SetVSync(enabled);
    }

    private void OnGraphicsQualityChanged(int value)
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.SetGraphicsQuality(value);
    }
}
