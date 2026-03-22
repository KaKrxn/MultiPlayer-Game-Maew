using UnityEngine;
using Blocks.Gameplay.Core;

public class TrainControlButton : MonoBehaviour, IInteractable
{
    [Header("Train Reference")]
    public AutomatedNetworkTransform trainController;

    [Tooltip("ลากสคริปต์ TrainFuelSystem มาใส่ช่องนี้เพื่อเช็คก่อนสตาร์ท")]
    public TrainFuelSystem fuelSystem;

    [Header("Button Settings")]
    public bool isForwardButton = true;

    public void OnInteract(ulong interactorClientId)
    {
        if (trainController != null)
        {
            // [เพิ่มโค้ดส่วนนี้] เช็คน้ำมันก่อนกดปุ่มเดินหน้า
            if (isForwardButton && fuelSystem != null && fuelSystem.currentFuel.Value <= 0)
            {
                Debug.LogWarning($"[Client {interactorClientId}] ⛽ สตาร์ทไม่ติด! น้ำมันหมดถังแล้ว!");
                // ยกเลิกการส่งคำสั่งไป Server ทันที
                return;
            }

            trainController.SetTrainMovingRpc(isForwardButton);

            if (isForwardButton)
                Debug.Log($"[Client {interactorClientId}] สับคันเร่ง! รถไฟกำลังพุ่งไปข้างหน้า");
            else
                Debug.Log($"[Client {interactorClientId}] ดึงเบรกฉุกเฉิน! รถไฟกำลังชะลอความเร็ว");
        }
    }
}