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
            NetworkObject targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();

            if (targetNetObj != null)
            {
                // [แก้บั๊ก] ส่ง "ชื่อของปุ่ม (GameObject Name)" ไปบอก Server ด้วย!
                RequestInteractRpc(targetNetObj.NetworkObjectId, hit.collider.gameObject.name);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestInteractRpc(ulong targetNetworkObjectId, string targetName, RpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
        {
            // [แก้บั๊ก] ค้นหาปุ่มลูกที่มี "ชื่อตรงกับที่ถูกยิง" 
            Transform targetTransform = GetChildByName(targetObject.transform, targetName);

            if (targetTransform != null)
            {
                IInteractable interactable = targetTransform.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.OnInteract(rpcParams.Receive.SenderClientId);
                }
            }
        }
    }

    // ฟังก์ชันช่วยค้นหา Object ลูกจากชื่อ
    private Transform GetChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = GetChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}