using UnityEngine;
using Unity.Netcode;

public class PlayerLocation : NetworkBehaviour
{
    public static PlayerLocation localPlayerMovement;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            localPlayerMovement = this;
        }
    }
}