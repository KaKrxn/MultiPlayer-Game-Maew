using UnityEngine;
using Unity.Netcode;

[DefaultExecutionOrder(-100)]
public class TrainMovementController : NetworkBehaviour
{
    public enum TrainState { Stopped, MovingForward, Braking }

    [Header("Train Movement Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 2f;
    public float brakeForce = 5f;

    // ซิงก์ State จาก Server ไปยัง Client ทุกคนตามโครงสร้าง Sync Train State
    public NetworkVariable<float> currentSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<TrainState> currentState = new NetworkVariable<TrainState>(
        TrainState.Stopped, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private void Update()
    {
        // 1. ให้ Server เป็นคนถือ Movement Authority เพื่อคำนวณและอนุมัติความเร็วเท่านั้น
        if (IsServer)
        {
            CalculateSpeed();
        }

        // 2. ให้ทุกเครื่อง (ทั้ง Server และ Client) ขยับโมเดลรถไฟด้วยตัวเองตามความเร็วที่ Server อนุมัติ
        MoveTrain();
    }

    private void CalculateSpeed()
    {
        switch (currentState.Value)
        {
            case TrainState.MovingForward:
                if (currentSpeed.Value < maxSpeed)
                {
                    currentSpeed.Value += acceleration * Time.deltaTime;
                    currentSpeed.Value = Mathf.Min(currentSpeed.Value, maxSpeed);
                }
                break;

            case TrainState.Braking:
            case TrainState.Stopped:
                if (currentSpeed.Value > 0)
                {
                    float currentDeceleration = (currentState.Value == TrainState.Braking) ? brakeForce : (brakeForce * 0.5f);
                    currentSpeed.Value -= currentDeceleration * Time.deltaTime;
                    currentSpeed.Value = Mathf.Max(currentSpeed.Value, 0f);
                }
                else if (currentState.Value == TrainState.Braking && currentSpeed.Value == 0f)
                {
                    currentState.Value = TrainState.Stopped;
                }
                break;
        }
    }

    private void MoveTrain()
    {
        if (currentSpeed.Value > 0)
        {
            transform.Translate(Vector3.forward * (currentSpeed.Value * Time.deltaTime));
        }
    }
}