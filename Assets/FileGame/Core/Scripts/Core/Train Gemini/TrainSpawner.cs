using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

/// <summary>
/// Server-side spawner that creates the train at the designated spawn point
/// and initializes its first waypoint.
/// </summary>
public class TrainSpawner : NetworkBehaviour
{
    [Tooltip("Drag the TrainRoot prefab here")]
    public GameObject trainPrefab;

    [Tooltip("Drag the spawn point transform here")]
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (IsServer && trainPrefab != null && spawnPoint != null)
        {
            // 1. Instantiate the train at the spawn point
            GameObject spawnedTrain = Instantiate(trainPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject netObj = spawnedTrain.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();
            }

            // 2. Get the train movement controller
            AutomatedNetworkTransform trainMovement = spawnedTrain.GetComponent<AutomatedNetworkTransform>();

            // 3. Set the spawn point as the first waypoint
            if (trainMovement != null)
            {
                trainMovement.AddNewWaypoints(new Transform[] { spawnPoint });
                Debug.Log("[TrainSpawner] Train spawned and start point configured successfully.");
            }
        }
    }
}