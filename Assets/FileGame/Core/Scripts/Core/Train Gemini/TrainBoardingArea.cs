using UnityEngine;
using Unity.Netcode;

public class TrainBoardingArea : NetworkBehaviour
{
    [Tooltip("ลาก PlayerAnchor ที่เพิ่งสร้างมาใส่ตรงนี้")]
    public Transform playerAnchor;

    // เมื่อผู้เล่นเดินเข้ามาในโซนรถไฟ
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // ให้ Server เป็นคนจัดการเท่านั้น

        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่ (เช็คจาก Tag Player ที่คุณตั้งไว้)
        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerAnchor != null)
            {
                // สั่งให้ผู้เล่นกลายเป็น "ลูก" ของรถไฟ
                playerNetObj.TrySetParent(playerAnchor, false);
                Debug.Log($"[Server] ผู้เล่นขึ้นรถไฟแล้ว! ล็อกตำแหน่งเข้ากับ PlayerAnchor");
            }
        }
    }

    // เมื่อผู้เล่นกระโดดลงจากรถไฟ
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.transform.parent == playerAnchor)
            {
                // ปลดผู้เล่นออกจากการเป็นลูก
                playerNetObj.TryRemoveParent();
                Debug.Log($"[Server] ผู้เล่นลงจากรถไฟแล้ว!");
            }
        }
    }
}