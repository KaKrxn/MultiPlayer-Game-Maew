using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneEntryManager : MonoBehaviour
{
    [SerializeField] private string roomSelectSceneName = "RoomSelect";

    private void Start()
    {
        if (SessionFlowContext.CurrentSession == null)
        {
            Debug.LogWarning("No active session found. Returning to RoomSelect.");
            SceneManager.LoadScene(roomSelectSceneName);
            return;
        }

        RoomRuntimeState.StartGame(RoomRuntimeState.IsLocked);
        Debug.Log("Entered GameScene.");
    }
}