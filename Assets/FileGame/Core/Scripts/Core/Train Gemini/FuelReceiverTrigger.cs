using UnityEngine;
using Unity.Netcode;
using FileGame.Core;

/// <summary>
/// Server-side trigger that absorbs dropped fuel items and adds fuel to the train.
/// Fuel amount is based on item durability scaled by the base refill amount.
/// </summary>
public class FuelReceiverTrigger : NetworkBehaviour
{
    public TrainFuelSystem fuelSystem;
    public string targetFuelItemName = "Fuel";
    public float fuelRefillAmount = GameConstants.TrainFuelRefillBase;

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
            
            // Notify all clients to show fuel added notification
            ShowFuelNotificationClientRpc(fuelToAdd);
        }

        NetworkObject fuelNetObj = other.GetComponent<NetworkObject>();
        if (fuelNetObj != null && fuelNetObj.IsSpawned)
        {
            fuelNetObj.Despawn(true);
        }
        else
        {
            Destroy(other.gameObject);
        }
    }

    [ClientRpc]
    private void ShowFuelNotificationClientRpc(float fuelAmount)
    {
        string message = $"+{fuelAmount:0}";
        FuelNotificationUI.Create(transform.position + Vector3.up * 1f, message);
    }
}
