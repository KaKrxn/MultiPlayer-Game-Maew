using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    public const string MasterVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";
    public const string DisplayModeKey = "DisplayMode";
    public const string VSyncKey = "VSync";
    public const string GraphicsQualityKey = "GraphicsQuality";

    private const string MusicTag = "Music";

    public static SettingManager Instance { get; private set; }

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;
    public int DisplayMode { get; private set; } = 0;
    public int VSync { get; private set; } = 1;
    public int GraphicsQuality { get; private set; } = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAll();
    }

    public void LoadSettings()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        DisplayMode = Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, 0), 0, 1);
        VSync = Mathf.Clamp(PlayerPrefs.GetInt(VSyncKey, 1), 0, 1);
        GraphicsQuality = Mathf.Clamp(PlayerPrefs.GetInt(GraphicsQualityKey, 1), 0, 2);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.Save();
        ApplyAudio();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyAudio();
    }

    public void SetDisplayMode(int value)
    {
        DisplayMode = Mathf.Clamp(value, 0, 1);
        PlayerPrefs.SetInt(DisplayModeKey, DisplayMode);
        PlayerPrefs.Save();
        ApplyDisplayMode();
    }

    public void SetVSync(bool enabled)
    {
        SetVSync(enabled ? 1 : 0);
    }

    public void SetVSync(int value)
    {
        VSync = value != 0 ? 1 : 0;
        PlayerPrefs.SetInt(VSyncKey, VSync);
        PlayerPrefs.Save();
        ApplyVSync();
    }

    public void SetGraphicsQuality(int value)
    {
        GraphicsQuality = Mathf.Clamp(value, 0, 2);
        PlayerPrefs.SetInt(GraphicsQualityKey, GraphicsQuality);
        PlayerPrefs.Save();
        ApplyGraphicsQuality();
    }

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyDisplayMode();
        ApplyVSync();
        ApplyGraphicsQuality();
    }

    public void ApplyAudio()
    {
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            if (source == null)
                continue;

            source.volume = IsMusicSource(source) ? MusicVolume : MasterVolume;
        }
    }

    private void ApplyDisplayMode()
    {
        FullScreenMode mode = DisplayMode == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        int width  = DisplayMode == 0 ? Screen.currentResolution.width  : Mathf.Max(Screen.width,  1280);
        int height = DisplayMode == 0 ? Screen.currentResolution.height : Mathf.Max(Screen.height, 720);
        Screen.SetResolution(width, height, mode);
    }

    private void ApplyVSync()
    {
        QualitySettings.vSyncCount = VSync == 1 ? 1 : 0;
    }

    private void ApplyGraphicsQuality()
    {
        int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (qualityCount <= 0)
            return;

        int qualityIndex = Mathf.Clamp(GraphicsQuality, 0, qualityCount - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);
    }

    private bool IsMusicSource(AudioSource source)
    {
        return source.gameObject != null && source.gameObject.tag == MusicTag;
    }
}
