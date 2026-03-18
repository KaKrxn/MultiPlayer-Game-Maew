using UnityEngine;
using Unity.Netcode;
using System.Linq; // สำหรับใช้ Find

public class PlayerDropItem : NetworkBehaviour
{
    public static PlayerDropItem Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(string itemName, Vector3 position, Quaternion rotation)
    {
        // ดึงลิสต์ allItems จาก InventoryManager.instance
        if (InventoryManager.instance == null)
        {
            Debug.LogError("InventoryManager instance is null! ไม่สามารถหาไอเทมได้");
            return;
        }

        // ค้นหา Prefab จากชื่อที่ส่งมา
        Item prefabToSpawn = InventoryManager.instance.allItems.Find(x => x.ItemName == itemName);

        if (prefabToSpawn != null)
        {
            // สร้าง Object ที่ Server
            GameObject spawnedObject = Instantiate(prefabToSpawn.gameObject, position, rotation);

            // สั่งให้ปรากฏบน Network (Client ทุกคนจะเห็น)
            NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
        else
        {
            Debug.LogWarning($"Server: ไม่พบไอเทมชื่อ {itemName} ในลิสต์ allItems ของ InventoryManager");
        }
    }
}