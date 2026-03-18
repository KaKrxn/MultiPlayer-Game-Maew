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
        //if (!IsServer) return;
        // 🚨 [สำคัญมาก!] กำแพงป้องกัน: ถ้าไม่ใช่ Server หรือหารถไฟไม่เจอ ให้หยุดทำงานและกระโดดออกจากฟังก์ชันนี้ไปเลย!
        if (!IsServer || trainMovement == null) return;

        // ถ้ารถไฟสตาร์ทเครื่องอยู่ (Server จะเป็นคนทำส่วนนี้เท่านั้น)
        if (trainMovement.IsMoving)
        {
            if (currentFuel.Value > 0)
            {
                // ลดน้ำมันตามเวลา
                currentFuel.Value -= fuelConsumptionRate * Time.deltaTime;

                if (currentFuel.Value <= 0)
                {
                    currentFuel.Value = 0;
                    trainMovement.SetTrainMoving(false);
                    Debug.LogWarning("[Server] ⛽ น้ำมันหมดเกลี้ยง! บังคับเบรกรถไฟฉุกเฉิน");
                }
            }
            else
            {
                // ถ้าน้ำมันหมดแล้วแต่รถไฟพยายามจะวิ่ง ให้สั่งดับเครื่อง
                trainMovement.SetTrainMoving(false);
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