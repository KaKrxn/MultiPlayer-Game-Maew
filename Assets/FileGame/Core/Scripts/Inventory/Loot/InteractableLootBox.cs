using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
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
                if (selectedLoot.itemData != null && selectedLoot.itemData.dropPrefab != null)
                {
                    ItemSizeRarity rarity = WeightedRandomUtility.RollSizeRarity();

                    // 2. Calculate Durability, Weight, and Scale
                    int durability = WeightedRandomUtility.CalculateDurability(rarity);
                    float weightKg = WeightedRandomUtility.CalculateWeight(rarity);
                    float scaleMult = WeightedRandomUtility.GetScaleMultiplier(rarity);
                    
                    // 3. Spawn Loot
                    Vector3 targetPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
                    GameObject spawnedItem = Instantiate(selectedLoot.itemData.dropPrefab, targetPos, Quaternion.identity);
                    
                    // Scale the visual transform
                    spawnedItem.transform.localScale *= scaleMult;

                    // Network Spawn FIRST so NetworkVariables are ready
                    NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }

                    // Assign data AFTER spawn
                    Item itemComponent = spawnedItem.GetComponent<Item>();
                    if (itemComponent != null)
                    {
                        ItemInstanceData spawnedData = selectedLoot.itemData != null && selectedLoot.itemData.usesAmountValue
                            ? new ItemInstanceData(selectedLoot.itemData, 100, 0f, durability)
                            : new ItemInstanceData(selectedLoot.itemData, durability, weightKg);
                        itemComponent.ApplyInstanceData(spawnedData);
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
}
