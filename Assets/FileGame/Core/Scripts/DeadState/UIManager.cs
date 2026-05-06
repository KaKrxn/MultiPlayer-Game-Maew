using Unity.Netcode;
using UnityEngine;

/// <summary>
/// UI button handler for debug death and revive actions.
/// </summary>
public class UIManager : MonoBehaviour
{
    public void OnClickDie()
    {
        Debug.Log("[UI] Die button clicked.");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerAction player))
                {
                    Debug.Log("[UI] Found local player. Sending death request to PlayerAction...");
                    player.LocalDie();
                }
                else
                {
                    Debug.LogError("[UI] PlayerObject found but missing PlayerAction component!");
                }
            }
            else
            {
                Debug.LogError("[UI] No PlayerObject (player has not spawned yet)!");
            }
        }
        else
        {
            Debug.LogError("[UI] Not connected to Server/Host!");
        }
    }

    public void OnClickRevive()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerAction player))
            {
                player.LocalRevive();
            }
        }
    }
}