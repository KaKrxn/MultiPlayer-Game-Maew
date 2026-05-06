using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Blocks.Gameplay.Core;

public class BiomeBoxSpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject lootBoxPrefab;
    [SerializeField] private BiomeLootRegistry lootRegistry;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Header("Settings")]
    [Range(0, 10)]
    [SerializeField] private int maxBoxesPerTile = 2;
    [Range(0, 1f)]
    [SerializeField] private float spawnChance = 0.5f;

    // This can be set by the EndlessMapManager when the tile is spawned
    [HideInInspector] public string currentBiomeName;

    public override void OnNetworkSpawn()
    {
        // Only the Server should handle spawning of networked objects
        if (!IsServer) return;

        SpawnLootBoxes();
    }

    private void SpawnLootBoxes()
    {
        if (lootBoxPrefab == null || spawnPoints.Count == 0 || lootRegistry == null) return;

        LootTableData targetLootTable = lootRegistry.GetLootTableForBiome(currentBiomeName);
        if (targetLootTable == null)
        {
            Debug.LogWarning($"[BiomeBoxSpawner] No loot table found for biome: {currentBiomeName}");
            return;
        }

        int boxesToSpawn = 0;
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < maxBoxesPerTile; i++)
        {
            if (availablePoints.Count == 0) break;
            
            if (Random.value <= spawnChance)
            {
                int pointIndex = Random.Range(0, availablePoints.Count);
                Transform targetPoint = availablePoints[pointIndex];
                availablePoints.RemoveAt(pointIndex);

                SpawnBox(targetPoint, targetLootTable);
                boxesToSpawn++;
            }
        }
        
        Debug.Log($"[Server] Spawned {boxesToSpawn} boxes for biome {currentBiomeName}");
    }

    private void SpawnBox(Transform point, LootTableData lootTable)
    {
        GameObject boxObj = Instantiate(lootBoxPrefab, point.position, point.rotation);
        
        // Configure the box
        InteractableLootBox boxLogic = boxObj.GetComponent<InteractableLootBox>();
        if (boxLogic != null)
        {
            SetLootTable(boxLogic, lootTable);
        }

        // Spawn it on the network
        NetworkObject netObj = boxObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            // Ensure the box belongs to this tile so it's destroyed when the tile is recycled
            netObj.TrySetParent(transform);
        }
        else
        {
            // Fallback for non-networked boxes if any exist
            boxObj.transform.SetParent(transform);
        }
    }

    private void SetLootTable(InteractableLootBox box, LootTableData table)
    {
        box.Initialize(table);
    }
}
