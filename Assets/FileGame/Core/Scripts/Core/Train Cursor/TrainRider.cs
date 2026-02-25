using UnityEngine;
using Blocks.Gameplay.Core; // CoreMovement

public class TrainRider : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 1.0f;
    [SerializeField] private LayerMask trainLayerMask = ~0; // กำหนดเป็น Layer ของ Train

    private CoreMovement movement;
    private TrainPlatform currentTrain;
    private Vector3 lastTrainPosition;
    private Vector3 trainDelta;

    private void Awake()
    {
        movement = GetComponent<CoreMovement>();
        if (movement == null)
        {
            Debug.LogError("[TrainRider] CoreMovement not found.");
            enabled = false;
            return;
        }

        // ตั้ง override ตัวเดียว ใช้ method นี้ตลอด
        movement.FinalMoveCalculationOverride = ApplyTrainMovement;
    }

    private void Update()
    {
        UpdateTrainUnderFeet();
    }

    private void UpdateTrainUnderFeet()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        bool onTrain = false;
        TrainPlatform hitTrain = null;

        if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, trainLayerMask))
        {
            hitTrain = hit.collider.GetComponentInParent<TrainPlatform>();
            if (hitTrain != null)
            {
                onTrain = true;
            }
        }

        if (onTrain)
        {
            if (currentTrain != hitTrain)
            {
                currentTrain = hitTrain;
                lastTrainPosition = currentTrain.transform.position;
                trainDelta = Vector3.zero;
            }
            else
            {
                Vector3 pos = currentTrain.transform.position;
                trainDelta = pos - lastTrainPosition;
                lastTrainPosition = pos;
            }
        }
        else
        {
            currentTrain = null;
            trainDelta = Vector3.zero;
        }
    }

    // ถูกเรียกจาก CoreMovement ก่อน CharacterController.Move()
    private Vector3 ApplyTrainMovement(Vector3 originalMove)
    {
        if (currentTrain == null)
            return originalMove;

        Vector3 extra = trainDelta;
        extra.y = 0f; // ไม่ดันแกน Y เพื่อไม่ให้ตัวละครลอย/จม

        return originalMove + extra;
    }

    private void OnDestroy()
    {
        if (movement != null && movement.FinalMoveCalculationOverride == ApplyTrainMovement)
        {
            movement.FinalMoveCalculationOverride = null;
        }
    }
}