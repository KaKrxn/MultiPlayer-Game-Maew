using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

// [เพิ่มคลาสเก็บข้อมูล Biome]
[System.Serializable]
public class BiomeData
{
    public string biomeName = "New Biome";
    [Tooltip("ใส่ Prefab ฉากทั้งหมดที่อยู่ใน Biome นี้")]
    public GameObject[] tilePrefabs;
    [Tooltip("จำนวนฉากขั้นต่ำที่จะสุ่มออกมาก่อนเปลี่ยน Biome")]
    public int minTiles = 3;
    [Tooltip("จำนวนฉากสูงสุดที่จะสุ่มออกมาก่อนเปลี่ยน Biome")]
    public int maxTiles = 7;
}

public class EndlessMapManager : NetworkBehaviour
{
    [Header("Train Reference")]
    [Tooltip("ไม่ต้องลากใส่แล้ว! ระบบจะค้นหารถไฟอัตโนมัติตอนเริ่มเกม")]
    public Transform trainTransform;

    [Header("Logic 4: Start Sequence (ฉากเริ่มต้น)")]
    [Tooltip("ฉากที่จะถูกเสกเรียงตามลำดับตอนเริ่มเกม")]
    public GameObject[] startTiles;

    [Header("Logic 1 & 2: Biome Settings (ระบบพื้นที่)")]
    public BiomeData[] biomes;
    public float standardTileLength = 50f;

    [Header("Logic 3: Transition Settings (ฉากเชื่อมคั่นกลาง)")]
    [Tooltip("ฉากอุโมงค์ หรือภูเขา ไว้กั้นสายตาก่อนเปลี่ยน Biome")]
    public GameObject transitionTilePrefab;
    [Tooltip("ความยาวของฉากเชื่อม (มักจะสั้นกว่าฉากปกติ)")]
    public float transitionTileLength = 30f;

    [Header("General Settings")]
    public int numberOfTilesOnScreen = 10;
    public float recycleDistance = 60f;

    [Header("Curved Horizon Control")]
    public int safeFlatTilesCount = 2;

    // ตัวแปรซ่อนสำหรับจัดการ State ของฉาก
    private List<GameObject> activeTiles = new List<GameObject>();
    private float spawnZ = 0f;

    // Track สถานะการ Spawn
    private bool isSpawningStartTiles = true;
    private int currentStartTileIndex = 0;
    private int currentBiomeIndex = -1;
    private int tilesLeftInCurrentBiome = 0;
    private bool isTransitionPhase = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(WaitAndSpawnInitialMap());
        }
    }

    private IEnumerator WaitAndSpawnInitialMap()
    {
        // 1. นั่งรอรถไฟเกิด
        Blocks.Gameplay.Core.AutomatedNetworkTransform trainController = null;
        while (trainController == null)
        {
            trainController = FindFirstObjectByType<Blocks.Gameplay.Core.AutomatedNetworkTransform>();
            yield return null;
        }

        trainTransform = trainController.transform;
        Debug.Log("[Server] 🚂 MapManager หารถไฟเจอแล้ว! เริ่มปูรางตามระบบ Biome!");

        // 2. เริ่มปูรางจำนวน numberOfTilesOnScreen ชิ้นแรก
        for (int i = 0; i < numberOfTilesOnScreen; i++)
        {
            SpawnNextLogicTile();
        }
    }

    private void Update()
    {
        if (!IsServer || trainTransform == null || activeTiles.Count == 0) return;

        // ถ้ารถไฟวิ่งเลยฉากแรกสุดไปไกลกว่าระยะ recycle ให้ทำลายฉากเก่าและสร้างฉากใหม่
        if (trainTransform.position.z - activeTiles[0].transform.position.z > recycleDistance)
        {
            RecycleOldestTile();
        }
    }

    /// <summary>
    /// ฟังก์ชันหลักที่ทำหน้าที่ตัดสินใจว่าจะ Spawn อะไร (Start, Biome หรือ Transition)
    /// </summary>
    private void SpawnNextLogicTile()
    {
        GameObject prefabToSpawn = null;
        float lengthOfThisTile = standardTileLength;

        // Logic 4: เช็คว่ากำลังปูฉากเริ่มต้นอยู่หรือไม่
        if (isSpawningStartTiles)
        {
            if (startTiles != null && currentStartTileIndex < startTiles.Length)
            {
                prefabToSpawn = startTiles[currentStartTileIndex];
                currentStartTileIndex++;
            }
            else
            {
                // หมดคิวฉาก Start แล้ว เข้าสู่ระบบ Biome
                isSpawningStartTiles = false;
                PickRandomBiome();
                prefabToSpawn = GetRandomTileFromCurrentBiome();
                tilesLeftInCurrentBiome--;
            }
        }
        // Logic 3: เช็คว่าถึงคิวของฉากเชื่อม (Transition) หรือไม่
        else if (isTransitionPhase)
        {
            if (transitionTilePrefab != null)
            {
                prefabToSpawn = transitionTilePrefab;
                lengthOfThisTile = transitionTileLength; // ใช้ความยาวเฉพาะของตัวเชื่อม
            }

            isTransitionPhase = false;
            PickRandomBiome(); // พอวางทางเชื่อมเสร็จ ก็สุ่ม Biome ใหม่รอไว้เลย
        }
        // Logic 1 & 2: ปูฉาก Biome ปกติ
        else
        {
            prefabToSpawn = GetRandomTileFromCurrentBiome();
            tilesLeftInCurrentBiome--;

            // ถ้าปูฉาก Biome นี้ครบโควต้าแล้ว คิวต่อไปให้ปูฉากเชื่อม (Transition)
            if (tilesLeftInCurrentBiome <= 0)
            {
                isTransitionPhase = true;
            }
        }

        // ถ้าหา Prefab ไม่ได้ (ลืมตั้งค่า) ให้ข้ามไป
        if (prefabToSpawn == null) return;

        // ทำการสร้างฉาก
        InstantiateAndSetupTile(prefabToSpawn, lengthOfThisTile);
    }

    /// <summary>
    /// สุ่มเลือก Biome ใหม่ และสุ่มจำนวนฉากที่จะปู
    /// </summary>
    private void PickRandomBiome()
    {
        if (biomes == null || biomes.Length == 0) return;

        currentBiomeIndex = Random.Range(0, biomes.Length);
        BiomeData currentBiome = biomes[currentBiomeIndex];

        // สุ่มว่า Biome นี้จะยาวกี่ Tile
        tilesLeftInCurrentBiome = Random.Range(currentBiome.minTiles, currentBiome.maxTiles + 1);

        Debug.Log($"[Map Manager] 🌲 เข้าสู่ Biome: {currentBiome.biomeName} (ความยาว {tilesLeftInCurrentBiome} ฉาก)");
    }

    /// <summary>
    /// สุ่มเลือก Prefab ฉากจาก Biome ปัจจุบัน
    /// </summary>
    private GameObject GetRandomTileFromCurrentBiome()
    {
        if (biomes == null || biomes.Length == 0 || currentBiomeIndex < 0) return null;

        BiomeData currentBiome = biomes[currentBiomeIndex];
        if (currentBiome.tilePrefabs == null || currentBiome.tilePrefabs.Length == 0) return null;

        int randomTileIndex = Random.Range(0, currentBiome.tilePrefabs.Length);
        return currentBiome.tilePrefabs[randomTileIndex];
    }

    /// <summary>
    /// เสกฉากลงในโลก และป้อน Waypoint ให้รถไฟ
    /// </summary>
    private void InstantiateAndSetupTile(GameObject prefab, float tileLength)
    {
        Vector3 spawnPosition = new Vector3(0, 0, spawnZ);
        GameObject tile = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Inject current biome name into the tile's spawner logic
        if (currentBiomeIndex >= 0 && currentBiomeIndex < biomes.Length)
        {
            var spawner = tile.GetComponent<BiomeBoxSpawner>();
            if (spawner == null) spawner = tile.GetComponentInChildren<BiomeBoxSpawner>();
            if (spawner != null)
            {
                spawner.currentBiomeName = biomes[currentBiomeIndex].biomeName;
            }
        }

        NetworkObject netObj = tile.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        // ดึง Waypoint ของรางชิ้นใหม่ ไปต่อคิวให้รถไฟ
        TileWaypoints tilePath = tile.GetComponent<TileWaypoints>();
        if (tilePath != null && trainTransform != null)
        {
            var trainController = trainTransform.GetComponent<Blocks.Gameplay.Core.AutomatedNetworkTransform>();
            if (trainController != null)
            {
                trainController.AddNewWaypoints(tilePath.orderedPoints);
            }
        }

        // สั่งการความโค้ง
        CurvedHorizonTile curveScript = tile.GetComponent<CurvedHorizonTile>();
        if (curveScript != null)
        {
            curveScript.flatDistance = safeFlatTilesCount * standardTileLength;
        }

        activeTiles.Add(tile);
        spawnZ += tileLength; // ขยับจุด SpawnZ ไปข้างหน้าตามความยาวของฉากที่เพิ่งเสก
    }

    /// <summary>
    /// ทำลายฉากเก่าทิ้ง แล้วเรียกฟังก์ชันปูฉากใหม่มาต่อท้าย
    /// </summary>
    private void RecycleOldestTile()
    {
        GameObject oldTile = activeTiles[0];
        activeTiles.RemoveAt(0);

        // Despawn และทำลายทิ้งเพื่อคืน RAM
        NetworkObject netObj = oldTile.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true); // true = ให้ Destroy GameObject ด้วย
        }
        else
        {
            Destroy(oldTile);
        }

        // สั่งสร้างฉากใหม่ไปต่อท้ายคิว
        SpawnNextLogicTile();
    }
}