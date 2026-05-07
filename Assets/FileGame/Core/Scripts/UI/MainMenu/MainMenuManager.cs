using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string roomSelectSceneName = "RoomSelect";

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private SettingPanelUI settingPanelUI;

    [Header("Player Name")]
    [SerializeField] private PlayerNameInputPopup nameInputPopup;
    [SerializeField] private Button renameButton;

    private void Awake()
    {
        if (nameInputPopup == null)
            nameInputPopup = FindObjectOfType<PlayerNameInputPopup>(true);

        if (settingPanelUI == null && settingPanel != null)
            settingPanelUI = settingPanel.GetComponentInChildren<SettingPanelUI>(true);

        if (settingPanel == null && settingPanelUI != null)
            settingPanel = settingPanelUI.gameObject;

        if (renameButton != null)
            renameButton.onClick.AddListener(OnClickRename);
    }

    private void Start()
    {
        if (nameInputPopup != null)
            nameInputPopup.ShowIfNameMissing();
    }

    private void OnDestroy()
    {
        if (renameButton != null)
            renameButton.onClick.RemoveListener(OnClickRename);
    }

    public void OnClickPlay()
    {
        if (nameInputPopup != null && !nameInputPopup.HasSavedName())
        {
            nameInputPopup.Show(false);
            return;
        }

        SceneManager.LoadScene(roomSelectSceneName);
    }

    public void OnClickSettings()
    {
        if (settingPanelUI != null)
        {
            settingPanelUI.TogglePanel();
            return;
        }

        if (settingPanel != null)
        {
            bool shouldOpen = !settingPanel.activeSelf;
            settingPanel.SetActive(shouldOpen);

            if (shouldOpen)
            {
                SettingPanelUI panelUI = settingPanel.GetComponentInChildren<SettingPanelUI>(true);
                if (panelUI != null)
                    panelUI.Refresh();
            }

            return;
        }

        Debug.LogWarning("Setting panel is not assigned.");
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickRename()
    {
        if (nameInputPopup != null)
            nameInputPopup.ShowForRename();
    }
}
