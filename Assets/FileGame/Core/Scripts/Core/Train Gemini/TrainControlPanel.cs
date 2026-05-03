using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

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
    [Tooltip("กำหนดว่าปุ่มนี้ทำหน้าที่อะไร")]
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

        // Send RPC to Server to process the state change authoritatively
        RequestControlServerRpc(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestControlServerRpc(ulong interactorClientId)
    {
        if (!IsServer) return;

        // แยกการทำงานตามประเภทของปุ่มที่ตั้งค่าไว้
        if (buttonType == ControlType.Forward)
        {
            // ถ้ากดปุ่มเดินหน้า และรถยังไม่ได้เดินหน้าอยู่ ให้สั่งเดินหน้า
            if (trainMovement.currentState.Value != TrainMovementController.TrainState.MovingForward)
            {
                trainMovement.currentState.Value = TrainMovementController.TrainState.MovingForward;
                Debug.Log($"[Server] Player {interactorClientId} สับคันเร่ง เดินหน้ารถไฟ!");
            }
        }
        else if (buttonType == ControlType.Brake)
        {
            // ถ้ากดปุ่มเบรก และรถยังไม่ได้เบรกหรือจอดอยู่ ให้สั่งเบรก
            if (trainMovement.currentState.Value != TrainMovementController.TrainState.Braking &&
                trainMovement.currentState.Value != TrainMovementController.TrainState.Stopped)
            {
                trainMovement.currentState.Value = TrainMovementController.TrainState.Braking;
                Debug.Log($"[Server] Player {interactorClientId} ดึงเบรกฉุกเฉิน!");
            }
        }
    }
}