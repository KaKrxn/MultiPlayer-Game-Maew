using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public interface IInteractable
{
    void OnInteract(ulong interactorClientId);
}

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;

    private void Update()
    {
        if (!IsOwner) return;

        // วาดเส้นสีแดงให้เห็นตอนเทส
        Debug.DrawRay(transform.position + Vector3.up, transform.forward * interactRange, Color.red);

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OnInteractInput();
        }
    }

    public void OnInteractInput()
    {
        if (!IsOwner) return;

        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            Debug.Log($"[Client] Raycast ยิงโดน: {hit.collider.gameObject.name}");

            // หา NetworkObject จากตัวที่โดนยิง หรือตัวแม่ของมัน
            NetworkObject targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();

            if (targetNetObj != null)
            {
                Debug.Log($"[Client] ส่ง Request ไปที่ Server สำหรับ Object ID: {targetNetObj.NetworkObjectId}");
                RequestInteractRpc(targetNetObj.NetworkObjectId);
            }
            else
            {
                Debug.LogWarning("[Client] ยิงโดนแล้ว แต่วัตถุนี้ไม่มี NetworkObject!");
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestInteractRpc(ulong targetNetworkObjectId, RpcParams rpcParams = default)
    {
        Debug.Log($"[Server] ได้รับคำขอให้โต้ตอบกับ Object ID: {targetNetworkObjectId}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
        {
            // ค้นหา IInteractable ทั้งในตัวเองและในลูกๆ (แก้ปัญหา Nesting Hierarchy)
            IInteractable interactable = targetObject.GetComponentInChildren<IInteractable>();

            if (interactable != null)
            {
                Debug.Log($"[Server] อนุมัติ! สั่งทำงาน OnInteract ที่ {targetObject.name}");
                interactable.OnInteract(rpcParams.Receive.SenderClientId);
            }
            else
            {
                Debug.LogError($"[Server] ผิดพลาด! หา IInteractable ไม่พบบน {targetObject.name} หรือลูกๆ ของมัน");
            }
        }
    }
}