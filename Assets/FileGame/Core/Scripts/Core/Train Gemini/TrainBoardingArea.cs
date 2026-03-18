using UnityEngine;
using Unity.Netcode;

public class TrainBoardingArea : NetworkBehaviour
{
    [Tooltip("ลาก PlayerAnchor ที่อยู่ข้างใน TrainRoot มาใส่ช่องนี้")]
    public Transform playerAnchor;

    private void OnTriggerEnter(Collider other)
    {
        // ให้ Server เป็นคนจับ Parent
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();

            // จับเป็นลูกของ Anchor ที่มี Scale 1,1,1 เท่านั้น! (ป้องกัน CharacterController พัง)
            if (playerNetObj != null && playerAnchor != null)
            {
                playerNetObj.TrySetParent(playerAnchor, true);
                Debug.Log($"[Server] ผู้เล่น {playerNetObj.OwnerClientId} เกาะรถไฟที่ PlayerAnchor แล้ว!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.transform.parent == playerAnchor)
            {
                // ปลดออกจากการเป็นลูก
                playerNetObj.TryRemoveParent();
                Debug.Log($"[Server] ผู้เล่น {playerNetObj.OwnerClientId} ลงจากรถไฟแล้ว!");
            }
        }
    }
}