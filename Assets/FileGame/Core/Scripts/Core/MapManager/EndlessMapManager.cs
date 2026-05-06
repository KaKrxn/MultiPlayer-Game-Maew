using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Data container for a biome configuration.
/// Each biome has a set of tile prefabs and a range of tiles to spawn before transitioning.
/// </summary>
[System.Serializable]
public class BiomeData
{
    public string biomeName = "New Biome";
    [Tooltip("All tile prefabs belonging to this biome")]
    public GameObject[] tilePrefabs;
    [Tooltip("Minimum number of tiles before biome transition")]
    public int minTiles = 3;
    [Tooltip("Maximum number of tiles before biome transition")]
    public int maxTiles = 7;
}

/// <summary>
/// Server-authoritative endless map generator.
/// Spawns tiles in biome sequences with transition tiles between biomes.
/// Automatically finds the train on spawn and feeds waypoints to it.
/// </summary>
public class EndlessMapManager : NetworkBehaviour
{
    [Header("Train Reference")]
    [Tooltip("Auto-discovered at runtime — no need to assign")]
    public Transform trainTransform;

    [Header("Start Sequence")]
    [Tooltip("Tiles spawned in order at game start")]
    public GameObject[] startTiles;

    [Header("Biome Settings")]
    public BiomeData[] biomes;
    public float standardTileLength = 50f;

    [Header("Transition Settings")]
    [Tooltip("Transition tile (tunnel/mountain) placed between biomes")]
    public GameObject transitionTilePrefab;
    [Tooltip("Length of the transition tile (usually shorter than standard)")]
    public float transitionTileLength = 30f;

    [Header("General Settings")]
    public int numberOfTilesOnScreen = 10;
    public float recycleDistance = 60f;

    [Header("Curved Horizon Control")]
    public int safeFlatTilesCount = 2;

    // Internal state
    private List<GameObject> activeTiles = new List<GameObject>();
    private float spawnZ = 0f;

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
        // Wait for the train to be spawned
        Blocks.Gameplay.Core.AutomatedNetworkTransform trainController = null;
        while (trainController == null)
        {
            trainController = FindFirstObjectByType<Blocks.Gameplay.Core.AutomatedNetworkTransform>();
            yield return null;
        }

        trainTransform = trainController.transform;
        Debug.Log("[MapManager] Train found! Starting biome-based track generation.");

        // Spawn initial set of tiles
        for (int i = 0; i < numberOfTilesOnScreen; i++)
        {
            SpawnNextLogicTile();
        }
    }

    private void Update()
    {
        if (!IsServer || trainTransform == null || activeTiles.Count == 0) return;

        // Recycle oldest tile when train passes it beyond the recycle distance
        if (trainTransform.position.z - activeTiles[0].transform.position.z > recycleDistance)
        {
            RecycleOldestTile();
        }
    }

    /// <summary>
    /// Core logic: decides what tile to spawn next (Start, Biome, or Transition).
    /// </summary>
    private void SpawnNextLogicTile()
    {
        GameObject prefabToSpawn = null;
        float lengthOfThisTile = standardTileLength;

        // Phase 1: Start sequence tiles
        if (isSpawningStartTiles)
        {
            if (startTiles != null && currentStartTileIndex < startTiles.Length)
            {
                prefabToSpawn = startTiles[currentStartTileIndex];
                currentStartTileIndex++;
            }
            else
            {
                // Start sequence complete — enter biome system
                isSpawningStartTiles = false;
                PickRandomBiome();
                prefabToSpawn = GetRandomTileFromCurrentBiome();
                tilesLeftInCurrentBiome--;
            }
        }
        // Phase 2: Transition tile between biomes
        else if (isTransitionPhase)
        {
            if (transitionTilePrefab != null)
            {
                prefabToSpawn = transitionTilePrefab;
                lengthOfThisTile = transitionTileLength;
            }

            isTransitionPhase = false;
            PickRandomBiome();
        }
        // Phase 3: Normal biome tiles
        else
        {
            prefabToSpawn = GetRandomTileFromCurrentBiome();
            tilesLeftInCurrentBiome--;

            // Biome quota reached — next tile will be a transition
            if (tilesLeftInCurrentBiome <= 0)
            {
                isTransitionPhase = true;
            }
        }

        if (prefabToSpawn == null) return;

        InstantiateAndSetupTile(prefabToSpawn, lengthOfThisTile);
    }

    /// <summary>
    /// Randomly selects a new biome and determines how many tiles to spawn.
    /// </summary>
    private void PickRandomBiome()
    {
        if (biomes == null || biomes.Length == 0) return;

        currentBiomeIndex = Random.Range(0, biomes.Length);
        BiomeData currentBiome = biomes[currentBiomeIndex];

        tilesLeftInCurrentBiome = Random.Range(currentBiome.minTiles, currentBiome.maxTiles + 1);

        Debug.Log($"[MapManager] Entering Biome: {currentBiome.biomeName} ({tilesLeftInCurrentBiome} tiles)");
    }

    /// <summary>
    /// Returns a random tile prefab from the current biome.
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
    /// Instantiates a tile, configures it (biome spawner, waypoints, curve), and tracks it.
    /// </summary>
    private void InstantiateAndSetupTile(GameObject prefab, float tileLength)
    {
        Vector3 spawnPosition = new Vector3(0, 0, spawnZ);
        GameObject tile = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Inject current biome name into the tile's loot spawner
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

        // Feed waypoints from the new tile to the train controller
        TileWaypoints tilePath = tile.GetComponent<TileWaypoints>();
        if (tilePath != null && trainTransform != null)
        {
            var trainController = trainTransform.GetComponent<Blocks.Gameplay.Core.AutomatedNetworkTransform>();
            if (trainController != null)
            {
                trainController.AddNewWaypoints(tilePath.orderedPoints);
            }
        }

        // Configure curved horizon effect
        CurvedHorizonTile curveScript = tile.GetComponent<CurvedHorizonTile>();
        if (curveScript != null)
        {
            curveScript.flatDistance = safeFlatTilesCount * standardTileLength;
        }

        activeTiles.Add(tile);
        spawnZ += tileLength;
    }

    /// <summary>
    /// Destroys the oldest tile and spawns a new one at the end of the track.
    /// </summary>
    private void RecycleOldestTile()
    {
        GameObject oldTile = activeTiles[0];
        activeTiles.RemoveAt(0);

        // Despawn and destroy to free memory
        NetworkObject netObj = oldTile.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(oldTile);
        }

        SpawnNextLogicTile();
    }
}