using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class InteractableLootBox : AInteractable
{
    [Header("Loot Configuration")]
    [SerializeField] private LootTableData lootTable;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Visuals & Audio")]
    [SerializeField] private ParticleSystem openParticles;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioSource audioSource;

    private bool _isOpened = false;

    public void Initialize(LootTableData table)
    {
        lootTable = table;
    }

    public override void Interact()
    {
        if (_isOpened) return;
        
        // Client requests server to open the box
        OpenBoxServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenBoxServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_isOpened || lootTable == null) return;
        
        _isOpened = true;
        
        // 1. Calculate Loot Autoritatively
        LootItemData selectedLoot = WeightedRandomUtility.GetRandomItem(lootTable.possibleLoot);
        
        if (selectedLoot != null && selectedLoot.itemPrefab != null)
        {
            // 2. Calculate Durability
            int durability = WeightedRandomUtility.CalculateDurability(selectedLoot);
            
            // 3. Spawn Loot
            Vector3 targetPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            GameObject spawnedItem = Instantiate(selectedLoot.itemPrefab, targetPos, Quaternion.identity);
            
            // Assign durability if the item component exists
            Item itemComponent = spawnedItem.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.Durability = durability;
            }
            
            // Network Spawn
            NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
        
        // 4. Trigger Client Visuals
        PlayOpenFXClientRpc();
        
        // 5. Despawn Box
        // We delay slightly to allow FX to trigger if needed, or rely on ClientRpc being sent first
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    private void PlayOpenFXClientRpc()
    {
        // One-shot visuals and audio
        if (openParticles != null)
        {
            // Instantiate at location since the box will be destroyed
            Instantiate(openParticles, transform.position, transform.rotation);
        }
        
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }
    }

    public override bool CanInteract()
    {
        return !_isOpened;
    }
}
