using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Blocks.Gameplay.Core;

public class InteractableLootBox : NetworkBehaviour, IInteractable
{
    [Header("Loot Configuration")]
    [SerializeField] private LootTableData lootTable;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Visuals & Audio")]
    [SerializeField] private ParticleSystem openParticles;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioSource audioSource;

    private bool _isOpened = false;

    // --- IInteractable Implementation ---
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 10;
    public string InteractionPromptText => "Open Box";

    public bool CanInteract(GameObject interactor)
    {
        return !_isOpened;
    }

    public void Interact(GameObject interactor)
    {
        if (_isOpened) return;
        
        // Client requests server to open the box
        OpenBoxServerRpc();
    }

    public void Initialize(LootTableData table)
    {
        lootTable = table;
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenBoxServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_isOpened || lootTable == null) return;
        
        _isOpened = true;
        
        // 1. Calculate Loot Autoritatively
        if (WeightedRandomUtility.GetRandomItem(lootTable.possibleLoot) is LootItemData selectedLoot)
        {
            if (selectedLoot.itemPrefab != null)
            {
                // 2. Calculate Durability
                int durability = WeightedRandomUtility.CalculateDurability(selectedLoot);
                float weightKg = WeightedRandomUtility.CalculateWeight(selectedLoot);
                
                // 3. Spawn Loot
                Vector3 targetPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
                GameObject spawnedItem = Instantiate(selectedLoot.itemPrefab, targetPos, Quaternion.identity);
                
                // Assign durability if the item component exists
                Item itemComponent = spawnedItem.GetComponent<Item>();
                if (itemComponent != null)
                {
                    itemComponent.Durability = durability;
                    itemComponent.WeightKg = weightKg;
                }
                
                // Network Spawn
                NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }
            }
        }
        
        // 4. Trigger Client Visuals
        PlayOpenFXClientRpc();
        
        // 5. Despawn Box
        if (TryGetComponent<NetworkObject>(out var networkObject))
        {
            networkObject.Despawn(false);
        }
        gameObject.SetActive(false);
    }

    [ClientRpc]
    private void PlayOpenFXClientRpc()
    {
        // One-shot visuals and audio
        if (openParticles != null)
        {
            // Instantiate at location since the box will be deactivated
            Instantiate(openParticles, transform.position, transform.rotation);
        }
        
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        // Hide the box on clients
        gameObject.SetActive(false);
    }
}
