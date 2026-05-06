using UnityEngine;

/// <summary>
/// Simple top-down camera that smoothly follows the local player.
/// Automatically finds the local player in multiplayer via NetworkObject ownership.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Distance Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -7);
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private float tiltAngle = 55f;

    private void Start()
    {
        // Set the tilt angle once at start
        transform.rotation = Quaternion.Euler(tiltAngle, 0, 0);
    }

    // LateUpdate ensures camera moves after player movement, preventing jitter
    private void LateUpdate()
    {
        if (target == null)
        {
            // In multiplayer, find the local player automatically
            FindLocalPlayer();
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    private void FindLocalPlayer()
    {
        // Find the player object that is owned by the local client
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var netObj = p.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                target = p.transform;
                break;
            }
        }
    }
}