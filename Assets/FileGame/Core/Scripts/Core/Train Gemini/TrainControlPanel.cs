using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

/// <summary>
/// Interactive train control panel (Forward/Brake).
/// Client presses interact → sends ServerRpc → server changes train state authoritatively.
/// </summary>
public class TrainControlPanel : NetworkBehaviour, IInteractable
{
    public enum ControlType
    {
        Forward,
        Brake
    }

    [Header("References")]
    public TrainMovementController trainMovement;

    [Header("Button Settings")]
    [Tooltip("Determines the function of this control button")]
    public ControlType buttonType;

    // --- IInteractable Implementation ---
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 10;
    public string InteractionPromptText => buttonType == ControlType.Forward ? "Move Forward" : "Brake";
    
    public bool CanInteract(GameObject interactor) => true;

    public void Interact(GameObject interactor)
    {
        ulong clientId = 0;
        if (interactor.TryGetComponent<NetworkObject>(out var netObj))
        {
            clientId = netObj.OwnerClientId;
        }

        RequestControlServerRpc(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestControlServerRpc(ulong interactorClientId)
    {
        if (!IsServer) return;

        if (buttonType == ControlType.Forward)
        {
            if (trainMovement.currentState.Value != TrainMovementController.TrainState.MovingForward)
            {
                trainMovement.currentState.Value = TrainMovementController.TrainState.MovingForward;
                Debug.Log($"[TrainControl] Player {interactorClientId} engaged throttle — moving forward.");
            }
        }
        else if (buttonType == ControlType.Brake)
        {
            if (trainMovement.currentState.Value != TrainMovementController.TrainState.Braking &&
                trainMovement.currentState.Value != TrainMovementController.TrainState.Stopped)
            {
                trainMovement.currentState.Value = TrainMovementController.TrainState.Braking;
                Debug.Log($"[TrainControl] Player {interactorClientId} pulled emergency brake.");
            }
        }
    }
}