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

    // เปลี่ยนเป็น float ธรรมดา เพราะเราไม่ต้องซิงก์ตัวเลขความเร็วผ่านเน็ตแล้ว 
    // เราจะซิงก์แค่ "ตำแหน่ง (Position)" ที่ Server ขยับเสร็จแล้วเท่านั้น
    public float currentSpeed = 0f;

    public NetworkVariable<TrainState> currentState = new NetworkVariable<TrainState>(
        TrainState.Stopped, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private void Update()
    {
        // หัวใจหลักของ AutomatedNetworkTransform: 
        // ถ้าไม่ใช่ Server (ไม่มีสิทธิ์) ให้หยุดทำงานตรงนี้เลย Client ห้ามแตะต้องพิกัด!
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
                    float currentDeceleration = (currentState.Value == TrainState.Braking) ? brakeForce : (brakeForce * 0.5f);
                    currentSpeed -= currentDeceleration * Time.deltaTime;
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
            // Server ขยับโมเดลคนเดียว แล้ว NetworkTransform จะดึงพิกัดนี้ไปส่งให้ Client เอง
            transform.Translate(Vector3.forward * (currentSpeed * Time.deltaTime));
        }
    }
}