using UnityEngine;
using Unity.Netcode;

public class FuelReceiverTrigger : NetworkBehaviour
{
    public TrainFuelSystem fuelSystem;
    public string targetFuelItemName = "Fuel";
    public float fuelRefillAmount = 25f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        Item droppedItem = other.GetComponent<Item>();
        if (droppedItem == null || droppedItem.Data == null || droppedItem.Data.itemName != targetFuelItemName)
        {
            return;
        }

        if (fuelSystem != null)
        {
            float durabilityRatio = droppedItem.Durability / 100f;
            float fuelToAdd = fuelRefillAmount * durabilityRatio;
            fuelSystem.AddFuel(fuelToAdd);
            
            // Trigger UI on all clients
            ShowFuelNotificationClientRpc(droppedItem.Durability);
        }

        NetworkObject fuelNetObj = other.GetComponent<NetworkObject>();
        if (fuelNetObj != null && fuelNetObj.IsSpawned)
        {
            fuelNetObj.Despawn(true);
        }
        else
        {
            // Fallback for non-networked objects (shouldn't happen in Netcode usually but keep for safety)
            Destroy(other.gameObject);
        }
    }

    [ClientRpc]
    private void ShowFuelNotificationClientRpc(float durability)
    {
        // Successful received Fuel Message with Yellow animated UI Text
        string message = $"+{durability:0}%!";
        FuelNotificationUI.Create(transform.position + Vector3.up * 1f, message);
    }
}
