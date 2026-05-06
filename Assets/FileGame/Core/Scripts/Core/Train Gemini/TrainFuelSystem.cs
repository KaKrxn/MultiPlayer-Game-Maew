using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;
using FileGame.Core;

/// <summary>
/// Server-authoritative train fuel system.
/// Tracks fuel consumption while moving and exposes methods for refueling.
/// </summary>
public class TrainFuelSystem : NetworkBehaviour
{
    [Header("Train Reference")]
    public AutomatedNetworkTransform trainMovement;

    [Header("Fuel Settings")]
    public float maxFuel = GameConstants.TrainDefaultMaxFuel;
    public float fuelConsumptionRate = GameConstants.TrainDefaultFuelConsumption;

    public NetworkVariable<float> currentFuel = new NetworkVariable<float>(
        GameConstants.TrainDefaultMaxFuel,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentFuel => currentFuel.Value;
    public float MaxFuel => maxFuel;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentFuel.Value = maxFuel;
        }
    }

    private void Update()
    {
        if (!IsServer || trainMovement == null) return;

        if (trainMovement.IsMoving)
        {
            if (currentFuel.Value > 0)
            {
                currentFuel.Value -= fuelConsumptionRate * Time.deltaTime;

                if (currentFuel.Value <= 0)
                {
                    currentFuel.Value = 0;
                    trainMovement.SetTrainMoving(false);
                    Debug.LogWarning("[TrainFuel] Fuel depleted! Emergency brake engaged.");
                }
            }
            else
            {
                // Fuel empty but train is trying to move — force stop
                trainMovement.SetTrainMoving(false);
            }
        }
    }

    public void AddFuel(float amount)
    {
        if (!IsServer) return;

        currentFuel.Value = Mathf.Min(currentFuel.Value + amount, maxFuel);
    }

    public void ConfigureFuelConsumption(float newFuelConsumptionRate)
    {
        if (!IsServer) return;

        fuelConsumptionRate = Mathf.Max(0f, newFuelConsumptionRate);
    }

    public void RefillToFull()
    {
        if (!IsServer) return;

        currentFuel.Value = maxFuel;
    }
}
