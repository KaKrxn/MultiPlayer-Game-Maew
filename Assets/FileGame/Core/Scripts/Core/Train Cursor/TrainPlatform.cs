using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Component สำหรับ Train ที่ทำให้ Player สามารถยืนอยู่บน Train และเคลื่อนที่ตาม Train ได้
/// ใส่ไว้บน Train GameObject
/// </summary>
[RequireComponent(typeof(TrainNetworkController))]
public class TrainPlatform : NetworkBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private float platformCheckRadius = 5f;
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private float platformHeight = 2f; // ความสูงจาก Train ที่ Player ยืนได้

    private TrainNetworkController trainController;
    private Vector3 lastTrainPosition;
    private Vector3 trainVelocity;

    private void Start()
    {
        trainController = GetComponent<TrainNetworkController>();
        if (trainController == null)
        {
            Debug.LogError("[TrainPlatform] TrainNetworkController not found!");
        }

        lastTrainPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // คำนวณ velocity ของ Train
        Vector3 currentPosition = transform.position;
        trainVelocity = (currentPosition - lastTrainPosition) / Time.fixedDeltaTime;
        lastTrainPosition = currentPosition;

        // หา Player ที่อยู่บน Train
        FindPlayersOnTrain();
    }

    private void FindPlayersOnTrain()
    {
        // ใช้ OverlapSphere เพื่อหา Player ที่อยู่ใกล้ Train
        Collider[] colliders = Physics.OverlapSphere(
            transform.position + Vector3.up * platformHeight,
            platformCheckRadius,
            playerLayerMask
        );

        foreach (var col in colliders)
        {
            // ตรวจสอบว่าเป็น Player หรือไม่
            var playerMovement = col.GetComponent<Blocks.Gameplay.Core.CoreMovement>();
            if (playerMovement != null)
            {
                var playerNetworkObject = col.GetComponent<NetworkObject>();
                var trainNetworkObject = GetComponent<NetworkObject>();

                if (playerNetworkObject == null || trainNetworkObject == null)
                {
                    continue;
                }

                // ตรวจสอบว่า Player อยู่บน Train หรือไม่ (Raycast ลงมา)
                if (IsPlayerOnTrain(col.transform.position))
                {
                    // ถ้า Player ยังไม่ได้เป็นลูกของ Train → ทำให้เป็นลูก (parent)
                    if (playerNetworkObject.transform.parent != transform)
                    {
                        // ใช้ Netcode parenting เพื่อ sync ข้าม network
                        // ระบุ Transform ให้ชัดเจนเพื่อหลีกเลี่ยง ambiguity
                        playerNetworkObject.TrySetParent(trainNetworkObject.transform, false);
                    }
                }
                else
                {
                    // ถ้า Player เคยเป็นลูกของ Train นี้ → เอา parent ออก
                    if (playerNetworkObject.transform.parent == transform)
                    {
                        // ระบุ Transform null ให้ชัดเจน
                        playerNetworkObject.TrySetParent((Transform)null, false);
                    }
                }
            }
        }
    }

    private bool IsPlayerOnTrain(Vector3 playerPosition)
    {
        // Raycast ลงมาจาก Player เพื่อดูว่าถูก Train หรือไม่
        RaycastHit hit;
        float rayDistance = platformHeight + 1f;
        
        if (Physics.Raycast(playerPosition + Vector3.up * 0.5f, Vector3.down, out hit, rayDistance))
        {
            // ตรวจสอบว่า hit กับ Train หรือไม่
            return hit.collider.transform == transform || 
                   hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }

    /// <summary>
    /// รับ Train Velocity สำหรับ Player ที่ยืนอยู่บน Train
    /// </summary>
    public Vector3 GetTrainVelocity()
    {
        return trainVelocity;
    }

    /// <summary>
    /// รับ Train Position สำหรับคำนวณ relative position
    /// </summary>
    public Vector3 GetTrainPosition()
    {
        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        // วาด Gizmo เพื่อแสดง platform area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * platformHeight, platformCheckRadius);
    }
}
