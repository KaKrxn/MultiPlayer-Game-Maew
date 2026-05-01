using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string roomSelectSceneName = "RoomSelect";
    [SerializeField] private string settingsSceneName = "Settings";

    public void OnClickPlay()
    {
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
}