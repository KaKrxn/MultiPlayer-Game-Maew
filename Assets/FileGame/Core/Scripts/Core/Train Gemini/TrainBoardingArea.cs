using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative train boarding area.
/// When a player enters the trigger, they are parented to the train anchor.
/// When they leave, the parent relationship is removed.
/// </summary>
public class TrainBoardingArea : NetworkBehaviour
{
    [Tooltip("Drag the PlayerAnchor child transform of the TrainRoot here")]
    public Transform playerAnchor;

    private void OnTriggerEnter(Collider other)
    {
        // Only server manages parent relationships
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();

            // Parent to the anchor (which has scale 1,1,1 to prevent CharacterController issues)
            if (playerNetObj != null && playerAnchor != null)
            {
                playerNetObj.TrySetParent(playerAnchor, true);
                Debug.Log($"[TrainBoarding] Player {playerNetObj.OwnerClientId} boarded the train.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.transform.parent == playerAnchor)
            {
                playerNetObj.TryRemoveParent();
                Debug.Log($"[TrainBoarding] Player {playerNetObj.OwnerClientId} left the train.");
            }
        }
    }
}