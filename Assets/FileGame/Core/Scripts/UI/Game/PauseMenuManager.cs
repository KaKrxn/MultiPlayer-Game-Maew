using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private SettingPanelUI settingPanelUI;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "[BB] MainMenu Scene";

    private bool IsOpen => pauseMenuPanel != null && pauseMenuPanel.activeSelf;

    // Cached references to local player components — found once on first open
    private CoreInputHandler m_InputHandler;
    private TopDownCamera    m_TopDownCamera;

    private void Awake()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingPanelUI != null)
            settingPanelUI.ClosePanel();

        BindButtons();
        LockCursorForGameplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePauseMenu();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void TogglePauseMenu()
    {
        if (IsOpen)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        SetPlayerFrozen(true);
        UnlockCursorForMenu();
    }

    public void ClosePauseMenu()
    {
        if (settingPanelUI != null)
            settingPanelUI.ClosePanel();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        SetPlayerFrozen(false);
        LockCursorForGameplay();
    }

    public void OnClickResume()
    {
        ClosePauseMenu();
    }

    public void OnClickSettings()
    {
        if (settingPanelUI != null)
            settingPanelUI.TogglePanel();
    }

    public void OnClickMainMenu()
    {
        GoToMainMenu();
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void GoToMainMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void BindButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnClickResume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnClickSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnClickMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnClickQuit);
    }

    private void UnbindButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnClickResume);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnClickSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnClickMainMenu);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnClickQuit);
    }

    private void UnlockCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursorForGameplay()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─── Freeze / Unfreeze ───────────────────────────────────────────────────

    /// <summary>
    /// Freeze or unfreeze the local player's input and camera.
    /// Network is NOT paused — other players continue normally.
    /// </summary>
    private void SetPlayerFrozen(bool frozen)
    {
        FetchLocalPlayerComponents();

        if (m_InputHandler != null)
            m_InputHandler.SetInputEnabled(!frozen);

        if (m_TopDownCamera != null)
            m_TopDownCamera.enabled = !frozen;
    }

    /// <summary>
    /// Find and cache CoreInputHandler and TopDownCamera from the local player object.
    /// Runs only once (or retries if player not spawned yet).
    /// </summary>
    private void FetchLocalPlayerComponents()
    {
        if (m_InputHandler != null && m_TopDownCamera != null)
            return;

        // Find local player via NetworkManager
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
            return;

        if (m_InputHandler == null)
            m_InputHandler = localPlayer.GetComponentInChildren<CoreInputHandler>();

        // Camera is usually not on the player prefab — find it in scene
        if (m_TopDownCamera == null)
            m_TopDownCamera = FindObjectOfType<TopDownCamera>();
    }
}
