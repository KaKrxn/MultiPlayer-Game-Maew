using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Alternative train control button using AutomatedNetworkTransform directly.
/// Checks fuel before allowing forward movement.
/// </summary>
public class TrainControlButton : MonoBehaviour, IInteractable
{
    [Header("Train Reference")]
    public AutomatedNetworkTransform trainController;

    [Tooltip("Drag the TrainFuelSystem here to check fuel before starting")]
    public TrainFuelSystem fuelSystem;

    [Header("Button Settings")]
    public bool isForwardButton = true;

    // --- IInteractable Implementation ---
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 10;
    public string InteractionPromptText => isForwardButton ? "Start Train" : "Stop Train";
    
    public bool CanInteract(GameObject interactor) => true;

    public void Interact(GameObject interactor)
    {
        ulong interactorClientId = 0;
        if (interactor.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
        {
            interactorClientId = netObj.OwnerClientId;
        }
        if (trainController != null)
        {
            // Check fuel before allowing forward movement
            if (isForwardButton && fuelSystem != null && fuelSystem.currentFuel.Value <= 0)
            {
                Debug.LogWarning($"[TrainControl] Player {interactorClientId} tried to start but fuel tank is empty!");
                return;
            }

            trainController.SetTrainMovingRpc(isForwardButton);

            if (isForwardButton)
                Debug.Log($"[TrainControl] Player {interactorClientId} engaged throttle — moving forward.");
            else
                Debug.Log($"[TrainControl] Player {interactorClientId} pulled emergency brake — decelerating.");
        }
    }
}