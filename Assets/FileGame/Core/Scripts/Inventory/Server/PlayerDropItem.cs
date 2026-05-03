using UnityEngine;
using Unity.Netcode;
using System.Linq;
using FileGame.Core;

public class PlayerDropItem : NetworkBehaviour
{
    public static PlayerDropItem Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("Server Item Registry")]
    [SerializeField] private ItemData[] serverItemRegistry;

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(string itemName, int durabilityPercent, float weightKg, Vector3 position, Quaternion rotation, ServerRpcParams serverRpcParams = default)
    {
        if (serverItemRegistry == null || serverItemRegistry.Length == 0)
        {
            Debug.LogError("PlayerDropItem: serverItemRegistry is empty.");
            return;
        }

        ItemData itemToDrop = serverItemRegistry.FirstOrDefault(x => x != null && x.itemName == itemName);

        if (itemToDrop != null && itemToDrop.dropPrefab != null)
        {
            GameObject spawnedObject = Instantiate(itemToDrop.dropPrefab, position, rotation);
            Item itemComponent = spawnedObject.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.ApplyInstanceData(new ItemInstanceData(itemToDrop, durabilityPercent, weightKg));
            }

            NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            ApplyWeightToPlayer(serverRpcParams.Receive.SenderClientId, -weightKg);
        }
        else
        {
            Debug.LogWarning($"Server: Could not spawn item '{itemName}' from serverItemRegistry.");
        }
    }

    private void ApplyWeightToPlayer(ulong clientId, float kgDelta)
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientData)) return;

        NetworkObject playerObject = clientData.PlayerObject;
        if (playerObject == null) return;

        if (playerObject.TryGetComponent<PlayerSurvivalSystem>(out var survivalSystem))
        {
            survivalSystem.ApplyCarriedWeightDelta(kgDelta);
        }
    }
}
