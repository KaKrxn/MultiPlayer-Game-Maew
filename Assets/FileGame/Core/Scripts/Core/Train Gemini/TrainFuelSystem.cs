using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class TrainFuelSystem : NetworkBehaviour
{
    [Header("Train Reference")]
    public AutomatedNetworkTransform trainMovement;

    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float fuelConsumptionRate = 2f;

    public NetworkVariable<float> currentFuel = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

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

        // ถ้ารถไฟสตาร์ทเครื่องอยู่
        if (trainMovement.IsMoving)
        {
            if (currentFuel.Value > 0)
            {
                currentFuel.Value -= fuelConsumptionRate * Time.deltaTime;

                if (currentFuel.Value <= 0)
                {
                    currentFuel.Value = 0;
                    trainMovement.SetTrainMovingRpc(false);
                    Debug.LogWarning("[Server] ⛽ น้ำมันหมดเกลี้ยง! บังคับเบรกรถไฟฉุกเฉิน");
                }
            }
            else
            {
                // [เพิ่มโค้ดส่วนนี้] ถ้าน้ำมัน <= 0 แต่รถไฟดันพยายามจะวิ่ง (เช่น เพิ่งโดนกดปุ่มมา)
                // ให้ดับเครื่องทันที!
                trainMovement.SetTrainMovingRpc(false);
            }
        }
    }

    public void AddFuel(float amount)
    {
        if (!IsServer) return;

        currentFuel.Value += amount;
        if (currentFuel.Value > maxFuel)
        {
            currentFuel.Value = maxFuel;
        }
        Debug.Log($"[Server] ⛽ เติมน้ำมัน {amount} หน่วย -> น้ำมันปัจจุบัน: {currentFuel.Value}/{maxFuel}");
    }
}