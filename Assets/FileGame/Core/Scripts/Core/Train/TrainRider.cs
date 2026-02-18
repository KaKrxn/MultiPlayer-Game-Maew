using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Component สำหรับ Player ที่ยืนอยู่บน Train
/// จะถูกเพิ่มอัตโนมัติเมื่อ Player ยืนอยู่บน Train
/// </summary>
[RequireComponent(typeof(CoreMovement))]
public class TrainRider : MonoBehaviour
{
    private TrainPlatform trainPlatform;
    private CoreMovement playerMovement;
    private Vector3 lastTrainPosition;
    private bool wasOnTrainLastFrame;

    private void Start()
    {
        playerMovement = GetComponent<CoreMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("[TrainRider] CoreMovement not found!");
        }
    }

    private void LateUpdate()
    {
        if (trainPlatform == null)
        {
            // ถ้าไม่ได้อยู่บน Train → ไม่ต้องทำอะไร
            if (wasOnTrainLastFrame)
            {
                // เพิ่ม callback เมื่อลงจาก Train (ถ้าต้องการ)
                OnExitTrain();
            }
            wasOnTrainLastFrame = false;
            return;
        }

        wasOnTrainLastFrame = true;

        // รับ Train velocity
        Vector3 trainVelocity = trainPlatform.GetTrainVelocity();

        // เพิ่ม Train velocity เข้าไปในการเคลื่อนที่ของ Player
        // โดยใช้ FinalMoveCalculationOverride ของ CoreMovement
        playerMovement.FinalMoveCalculationOverride = (originalMove) =>
        {
            // รวมการเคลื่อนที่ของ Player กับ Train
            // ใช้ FixedDeltaTime เพื่อความแม่นยำ
            Vector3 trainMove = trainVelocity * Time.fixedDeltaTime;
            
            // เพิ่มเฉพาะ horizontal movement (X, Z) ไม่เพิ่ม Y เพื่อไม่ให้ Player ลอย
            trainMove.y = 0f;
            
            return originalMove + trainMove;
        };

        // อัปเดต last position
        lastTrainPosition = trainPlatform.GetTrainPosition();
    }

    private void OnExitTrain()
    {
        // ลบ override เมื่อลงจาก Train
        if (playerMovement != null)
        {
            playerMovement.FinalMoveCalculationOverride = null;
        }
    }

    /// <summary>
    /// Set Train Platform ที่ Player ยืนอยู่
    /// </summary>
    public void SetTrainPlatform(TrainPlatform platform)
    {
        trainPlatform = platform;

        if (platform == null)
        {
            OnExitTrain();
        }
        else
        {
            lastTrainPosition = platform.GetTrainPosition();
        }
    }

    /// <summary>
    /// รับ Train Platform ปัจจุบัน
    /// </summary>
    public TrainPlatform GetTrainPlatform()
    {
        return trainPlatform;
    }

    private void OnDestroy()
    {
        // ลบ override เมื่อ component ถูก destroy
        if (playerMovement != null)
        {
            playerMovement.FinalMoveCalculationOverride = null;
        }
    }
}
