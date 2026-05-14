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
    public void RequestSpawnItemServerRpc(string itemName, int durabilityPercent, float weightKg, int stackCount, Vector3 position, Quaternion rotation, ServerRpcParams serverRpcParams = default)
    {
        if (serverItemRegistry == null || serverItemRegistry.Length == 0)
        {
            Debug.LogError("PlayerDropItem: serverItemRegistry is empty.");
            return;
        }

        ItemData itemToDrop = serverItemRegistry.FirstOrDefault(x => x != null && x.itemName == itemName);
        if (itemToDrop == null)
        {
            itemToDrop = Resources.FindObjectsOfTypeAll<ItemData>()
                .FirstOrDefault(x => x != null && x.itemName == itemName);
        }

        if (itemToDrop != null && itemToDrop.dropPrefab != null)
        {
            GameObject spawnedObject = Instantiate(itemToDrop.dropPrefab, position, rotation);
            
            // Apply Dynamic Scaling based on weight
            spawnedObject.transform.localScale *= WeightedRandomUtility.CalculateDynamicScale(weightKg);

            NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            Item itemComponent = spawnedObject.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.ApplyInstanceData(new ItemInstanceData(itemToDrop, durabilityPercent, weightKg, stackCount));
            }

            ApplyWeightToPlayer(serverRpcParams.Receive.SenderClientId, -weightKg);
        }
        else
        {
            string registryContents = string.Join(", ", serverItemRegistry.Select(x => x != null ? x.itemName : "null"));
            Debug.LogWarning($"Server: Could not spawn item '{itemName}' from serverItemRegistry. Available in registry: [{registryContents}]");
        }
    }

    private void ApplyWeightToPlayer(ulong clientId, float kgDelta)
    {
        ServerUtility.ApplyWeightToPlayer(clientId, kgDelta);
    }
}
