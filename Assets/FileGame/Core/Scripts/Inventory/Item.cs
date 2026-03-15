using UnityEngine;
using Unity.Netcode; // หรือ PurrNet ตามที่คุณใช้งาน

// แก้ไข: เอา , NetworkBehaviour ออก เพราะ AInteractable สืบทอดมาให้แล้ว
public class Item : AInteractable 
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemPicture;

    public string ItemName => itemName;
    public Sprite ItemPicture => itemPicture;

    public override void Interact()
    {
        Pickup();
    }

    public void Pickup()
    {
        CmdPickupItem();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdPickupItem(ServerRpcParams rpcParams = default)
    {
        // if (!IsServer) return;

        if (!InstanceHandler.TryGetInstance(out InventoryManager inventoryManager))
        {
            Debug.LogError("Couldn't get inventory manager for item: " + name);
            return;
        }

        // ข้อควรระวังลอจิก: บรรทัดนี้ทำงานอยู่บนฝั่ง "Server"
        inventoryManager.AddItem(this);

        var netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true); // Despawn จะทำลาย Object ผ่าน Network ให้ทุกคนเห็นว่าหายไป
        }
    }
}