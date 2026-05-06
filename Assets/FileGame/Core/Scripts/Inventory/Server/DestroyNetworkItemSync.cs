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
                ServerUtility.ApplyWeightToPlayer(winnerClientId, item.WeightKg * item.StackCount);
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
        if (myItem != null && myItem.Data != null)
        {
            // Route through server-authoritative inventory handler
            if (PlayerLocation.localPlayerMovement != null)
            {
                var networkHandler = PlayerLocation.localPlayerMovement.GetComponent<InventoryNetworkHandler>();
                if (networkHandler != null)
                {
                    var instanceData = myItem.BuildItemInstanceData();
                    networkHandler.RequestAddItemServerRpc(
                        new Unity.Collections.FixedString32Bytes(instanceData.itemData.itemName),
                        instanceData.durabilityPercent,
                        instanceData.weightKg,
                        instanceData.stackCount);
                    return;
                }
            }

            // Fallback: local-only
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.AddItem(myItem.BuildItemInstanceData());
            }
        }
    }

    [ClientRpc]
    private void DeactivateObjectClientRpc()
    {
        gameObject.SetActive(false);
    }
}
