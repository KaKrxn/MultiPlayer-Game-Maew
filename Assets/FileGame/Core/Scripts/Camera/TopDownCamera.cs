using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // ลาก Player มาใส่ หรือจะให้หาอัตโนมัติก็ได้
    
    [Header("Distance Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -7); // ระยะห่าง (สูง 10, ถอยหลัง 7)
    [SerializeField] private float smoothSpeed = 5f; // ความนุ่มนวลในการเลื่อนตาม

    [Header("Rotation Settings")]
    [SerializeField] private float tiltAngle = 55f; // มุมก้ม (Overcooked จะอยู่ประมาณ 45-60 องศา)

    private void Start()
    {
        // ตั้งค่ามุมก้มครั้งเดียวตอนเริ่ม
        transform.rotation = Quaternion.Euler(tiltAngle, 0, 0);
    }

    // ใช้ LateUpdate เพื่อให้กล้องขยับหลังจาก Player ขยับเสร็จแล้ว จะช่วยให้กล้องไม่สั่น
    private void LateUpdate()
    {
        if (target == null)
        {
            // ถ้าเป็นเกม Multiplayer อาจจะหา Local Player มาเป็นเป้าหมาย
            FindLocalPlayer();
            return;
        }

        // คำนวณตำแหน่งที่กล้องควรจะไปอยู่
        Vector3 desiredPosition = target.position + offset;
        
        // ทำให้กล้องเลื่อนตามแบบนุ่มนวล (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        transform.position = smoothedPosition;
    }

    private void FindLocalPlayer()
    {
        // สำหรับ Unity Netcode: หา Object ที่มี NetworkObject และเป็น Owner
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var netObj = p.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                target = p.transform;
                break;
            }
        }
    }
}