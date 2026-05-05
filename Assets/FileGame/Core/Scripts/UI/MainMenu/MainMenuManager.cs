using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string roomSelectSceneName = "RoomSelect";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("Player Name")]
    [SerializeField] private PlayerNameInputPopup nameInputPopup;
    [SerializeField] private Button renameButton;

    private void Awake()
    {
        if (nameInputPopup == null)
            nameInputPopup = FindObjectOfType<PlayerNameInputPopup>(true);

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
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            SceneManager.LoadScene(settingsSceneName);
        }
        else
        {
            Debug.Log("Settings scene name is empty.");
        }
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
