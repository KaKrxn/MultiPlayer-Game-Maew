using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class EndlessMapManager : NetworkBehaviour
{
    [Header("Train Reference")]
    [Tooltip("ลาก TrainRoot ของรถไฟมาใส่ เพื่อให้ระบบรู้ว่าตอนนี้รถไฟอยู่ตรงไหน")]
    public Transform trainTransform;

    [Header("Tile Settings (ตั้งค่าฉาก)")]
    [Tooltip("ใส่ Prefab ฉากต่างๆ ที่มี (เช่น ทางตรง, ทางโค้งนิดๆ, ทางมีต้นไม้)")]
    public GameObject[] tilePrefabs;

    [Tooltip("จำนวนฉากที่จะ Spawn มารอไว้ในฉาก (ยิ่งเยอะยิ่งมองเห็นไกล แต่กินสเปค)")]
    public int numberOfTilesOnScreen = 10;

    [Tooltip("ความยาวของฉาก 1 ชิ้น (แกน Z) เพื่อให้มันต่อกันสนิทพอดี")]
    public float tileLength = 50f;

    [Tooltip("ระยะห่างด้านหลังรถไฟ ที่จะยอมให้ฉากถูกดึงกลับไปต่อคิวข้างหน้า")]
    public float recycleDistance = 60f;

    // List สำหรับเก็บฉากที่กำลังใช้งานอยู่ (เรียงลำดับจากหลังสุด ไปหน้าสุด)
    private List<GameObject> activeTiles = new List<GameObject>();

    // พิกัด Z สำหรับวางฉากชิ้นต่อไป
    private float spawnZ = 0f;

    public override void OnNetworkSpawn()
    {
        // กฎเหล็ก: ให้ Server เป็นคนสร้างและเรียงฉากเท่านั้น Client มีหน้าที่แค่ดู!
        if (IsServer)
        {
            // สร้างฉากเริ่มต้นเรียงกันตามจำนวนที่ตั้งไว้
            for (int i = 0; i < numberOfTilesOnScreen; i++)
            {
                SpawnRandomTile();
            }
        }
    }

    private void Update()
    {
        // Server เท่านั้นที่คอยตรวจจับระยะทาง
        if (!IsServer || trainTransform == null || activeTiles.Count == 0) return;

        // เช็คว่ารถไฟวิ่งเลย "ฉากชิ้นแรกสุด (ท้ายขบวน)" ไปไกลเกินระยะ recycleDistance หรือยัง
        if (trainTransform.position.z - activeTiles[0].transform.position.z > recycleDistance)
        {
            RecycleTile();
        }
    }

    /// <summary>
    /// สุ่มดึง Prefab จากตะกร้ามาสร้าง และสั่ง Spawn ผ่านเครือข่าย
    /// </summary>
    private void SpawnRandomTile()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0) return;

        // 1. สุ่มเลือก Index ของ Prefab
        int randomIndex = Random.Range(0, tilePrefabs.Length);

        // 2. สร้าง GameObject ลงในฉากที่พิกัด spawnZ
        Vector3 spawnPosition = new Vector3(0, 0, spawnZ);
        GameObject tile = Instantiate(tilePrefabs[randomIndex], spawnPosition, Quaternion.identity);

        // 3. สั่ง Spawn ผ่าน Network (สำคัญมาก: เพื่อให้ Client เห็นด้วย)
        tile.GetComponent<NetworkObject>().Spawn();

        // 4. เอาเก็บเข้า List และเลื่อนจุดสร้างชิ้นต่อไป
        activeTiles.Add(tile);
        spawnZ += tileLength;
    }

    /// <summary>
    /// ดึงฉากที่รถไฟวิ่งผ่านไปแล้ว ย้ายไปดักรอที่หน้าสุดของขบวน (สายพาน)
    /// </summary>
    private void RecycleTile()
    {
        // 1. หยิบฉากชิ้นแรกสุด (ที่อยู่หลังรถไฟ) ออกมาจาก List
        GameObject oldTile = activeTiles[0];
        activeTiles.RemoveAt(0);

        // 2. จับมันวาร์ปไปวางที่ตำแหน่งหน้าสุด
        oldTile.transform.position = new Vector3(0, 0, spawnZ);

        // 3. จับใส่กลับเข้าไปท้าย List (ต่อคิวเป็นฉากหน้าสุด)
        activeTiles.Add(oldTile);

        // 4. เลื่อนพิกัดเตรียมรับฉากชิ้นถัดไป
        spawnZ += tileLength;

        // หมายเหตุ: เนื่องจาก Prefab มี NetworkTransform แปะอยู่
        // ทันทีที่ Server จับมันวาร์ป (เปลี่ยน position) 
        // Client ทุกคนจะเห็นฉากนี้วาร์ปไปดักหน้าโดยอัตโนมัติ!
    }
}