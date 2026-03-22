using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class TrainSpawner : NetworkBehaviour
{
    [Tooltip("ลาก Prefab รถไฟ TrainRoot มาใส่ตรงนี้")]
    public GameObject trainPrefab;

    [Tooltip("ลากจุดเกิดรถไฟ (GameObject ว่างๆ) มาใส่ตรงนี้")]
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (IsServer && trainPrefab != null && spawnPoint != null)
        {
            // 1. เสกรถไฟที่จุด Spawn
            GameObject spawnedTrain = Instantiate(trainPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject netObj = spawnedTrain.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();
            }

            // 2. ดึงสคริปต์ควบคุมรถไฟ
            AutomatedNetworkTransform trainMovement = spawnedTrain.GetComponent<AutomatedNetworkTransform>();

            // 3. ยัดจุด Spawn ให้กลายเป็น Start Point (Waypoint แรกสุด) ของรถไฟ!
            if (trainMovement != null)
            {
                trainMovement.AddNewWaypoints(new Transform[] { spawnPoint });
                Debug.Log("[Server] 📍 เสกรถไฟและตั้งค่า Start Point สำเร็จ!");
            }
        }
    }
}