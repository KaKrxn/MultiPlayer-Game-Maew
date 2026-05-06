using Unity.Netcode;
using UnityEngine;
using FileGame.Core;

public class DestroyNetworkItemSync : NetworkBehaviour
{
    private bool _hasRequested = false;

    public void RequestPickup()
    {
        if (IsClient && !_hasRequested)
        {
            _hasRequested = true;
            PickupServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupServerRpc(ServerRpcParams serverRpcParams = default)
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        Item item = GetComponent<Item>();

        if (netObj != null && netObj.IsSpawned)
        {
            ulong winnerClientId = serverRpcParams.Receive.SenderClientId;

            if (item != null)
            {
                ApplyWeightToPlayer(winnerClientId, item.WeightKg);
            }

            GrantItemClientRpc(winnerClientId);
            DeactivateObjectClientRpc();

            netObj.Despawn(false);
            gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void GrantItemClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        Item myItem = GetComponent<Item>();
        if (myItem != null && myItem.Data != null && InventoryManager.instance != null)
        {
            InventoryManager.instance.AddItem(myItem.BuildItemInstanceData());
        }
    }

    [ClientRpc]
    private void DeactivateObjectClientRpc()
    {
        gameObject.SetActive(false);
    }

    private void ApplyWeightToPlayer(ulong clientId, float weightKg)
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientData)) return;

        NetworkObject playerObject = clientData.PlayerObject;
        if (playerObject == null) return;

        if (playerObject.TryGetComponent<PlayerSurvivalSystem>(out var survivalSystem))
        {
            survivalSystem.ApplyCarriedWeightDelta(weightKg);
        }
    }
}
