using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative train movement controller.
/// Handles speed calculation (acceleration/braking) and position updates.
/// Clients receive position updates via NetworkTransform.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TrainMovementController : NetworkBehaviour
{
    public enum TrainState { Stopped, MovingForward, Braking }

    [Header("Train Movement Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 2f;
    public float brakeForce = 5f;

    // Local speed value — not synced over network.
    // Only the final position (via NetworkTransform) is synced to clients.
    public float currentSpeed = 0f;

    public NetworkVariable<TrainState> currentState = new NetworkVariable<TrainState>(
        TrainState.Stopped, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private void Update()
    {
        // Only server controls train physics
        if (!IsServer) return;

        CalculateSpeed();
        MoveTrain();
    }

    private void CalculateSpeed()
    {
        switch (currentState.Value)
        {
            case TrainState.MovingForward:
                if (currentSpeed < maxSpeed)
                {
                    currentSpeed += acceleration * Time.deltaTime;
                    currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
                }
                break;

            case TrainState.Braking:
            case TrainState.Stopped:
                if (currentSpeed > 0)
                {
                    float deceleration = (currentState.Value == TrainState.Braking) 
                        ? brakeForce 
                        : (brakeForce * 0.5f);
                    currentSpeed -= deceleration * Time.deltaTime;
                    currentSpeed = Mathf.Max(currentSpeed, 0f);
                }
                else if (currentState.Value == TrainState.Braking && currentSpeed == 0f)
                {
                    currentState.Value = TrainState.Stopped;
                }
                break;
        }
    }

    private void MoveTrain()
    {
        if (currentSpeed > 0)
        {
            // Server moves the model; NetworkTransform syncs position to clients
            transform.Translate(Vector3.forward * (currentSpeed * Time.deltaTime));
        }
    }
}