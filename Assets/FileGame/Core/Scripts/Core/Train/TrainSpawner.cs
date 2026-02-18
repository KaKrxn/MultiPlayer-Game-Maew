using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Script สำหรับ Spawn Train ใน Scene (ฝั่ง Server เท่านั้น)
/// วางไว้ใน Scene หรือเรียกจาก GameManager
/// </summary>
public class TrainSpawner : NetworkBehaviour
{
    [Header("Train Prefab")]
    [SerializeField] private GameObject trainPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private Quaternion spawnRotation = Quaternion.identity;
    [SerializeField] private bool spawnOnStart = true;

    private GameObject spawnedTrain;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && spawnOnStart)
        {
            SpawnTrain();
        }
    }

    /// <summary>
    /// Spawn Train (เรียกจาก Server เท่านั้น)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnTrainServerRpc()
    {
        SpawnTrain();
    }

    private void SpawnTrain()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TrainSpawner] Only server can spawn train!");
            return;
        }

        if (trainPrefab == null)
        {
            Debug.LogError("[TrainSpawner] Train Prefab is not assigned!");
            return;
        }

        // ถ้ามี Train อยู่แล้ว → ไม่ต้อง spawn ใหม่
        if (spawnedTrain != null && spawnedTrain != null)
        {
            Debug.LogWarning("[TrainSpawner] Train already exists!");
            return;
        }

        // Instantiate Train
        GameObject trainInstance = Instantiate(trainPrefab, spawnPosition, spawnRotation);
        NetworkObject trainNetworkObject = trainInstance.GetComponent<NetworkObject>();

        if (trainNetworkObject == null)
        {
            Debug.LogError("[TrainSpawner] Train Prefab must have NetworkObject component!");
            Destroy(trainInstance);
            return;
        }

        // Spawn บน Network
        trainNetworkObject.Spawn();

        spawnedTrain = trainInstance;
        Debug.Log($"[TrainSpawner] Train spawned at {spawnPosition}");
    }

    /// <summary>
    /// Despawn Train (เรียกจาก Server เท่านั้น)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DespawnTrainServerRpc()
    {
        if (spawnedTrain != null)
        {
            NetworkObject networkObject = spawnedTrain.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
            spawnedTrain = null;
        }
    }

    // Helper method สำหรับเรียกจาก Inspector หรือ script อื่น
    public void SpawnTrainManually()
    {
        if (IsServer)
        {
            SpawnTrain();
        }
        else
        {
            SpawnTrainServerRpc();
        }
    }
}
