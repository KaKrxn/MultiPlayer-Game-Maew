using Unity.Netcode;
using UnityEngine;

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

        if (netObj != null && netObj.IsSpawned)
        {
            ulong winnerClientId = serverRpcParams.Receive.SenderClientId;
            // Debug.Log($"Server ตัดสินให้ Client ID: {winnerClientId} เป็นคนได้ของไป!");

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { winnerClientId }
                }
            };

            GrantItemClientRpc(clientRpcParams);
            netObj.Despawn();
        }
    }


    [ClientRpc]
    private void GrantItemClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Item myItem = GetComponent<Item>();
        if (myItem != null)
        {
            InventoryManager.instance.AddItem(myItem);
            // Debug.Log("เย้! ฉันเก็บไอเทมสำเร็จและเข้ากระเป๋าแล้ว!");
        }
    }
}