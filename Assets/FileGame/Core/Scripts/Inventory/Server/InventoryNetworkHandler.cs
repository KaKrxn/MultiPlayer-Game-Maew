using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Linq;
using FileGame.Core;
using System.Diagnostics;

/// <summary>
/// Server-authoritative inventory handler living on each player prefab.
/// Owns the NetworkList of slot data and validates all inventory mutations via ServerRpc.
/// The client-side InventoryManager UI layer calls into this handler for all changes.
/// </summary>
public class InventoryNetworkHandler : NetworkBehaviour
{
    [Header("Server Item Registry")]
    [SerializeField] private ItemData[] serverItemRegistry;

    /// <summary>
    /// Server-authoritative inventory state. Index matches InventoryManager.allSlots.
    /// </summary>
    private NetworkList<NetworkInventorySlotData> _serverInventory;

    private int _slotCount;

    private void Awake()
    {
        _serverInventory = new NetworkList<NetworkInventorySlotData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Initialize with the expected slot count from InventoryManager
            int slotCount = GetExpectedSlotCount();
            _slotCount = slotCount;

            for (int i = 0; i < slotCount; i++)
            {
                _serverInventory.Add(NetworkInventorySlotData.Empty);
            }
        }

        if (IsOwner)
        {
            // Register this handler for the local player's InventoryManager to find
            _serverInventory.OnListChanged += OnServerInventoryChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _serverInventory.OnListChanged -= OnServerInventoryChanged;
        }
        base.OnNetworkDespawn();
    }

    private int GetExpectedSlotCount()
    {
        // InventoryManager sets up quickSlots + mainSlots
        if (InventoryManager.instance != null)
        {
            return InventoryManager.instance.allSlots.Count;
        }
        return 15; // fallback: 5 quickSlots + 10 mainSlots
    }

    // ── Server RPCs ──────────────────────────────────────────────

    [ServerRpc]
    public void RequestAddItemServerRpc(FixedString32Bytes itemName, int durability, float weight, int stackCount)
    {
        ItemData itemData = ResolveItemData(itemName.ToString());
        if (itemData == null)
        {
            UnityEngine.Debug.LogWarning($"[Inventory] Server rejected add: '{itemName}' not found in registry.");
            return;
        }

        int remaining = stackCount;
        bool isStackable = itemData.maxStack > 1;

        // Try stacking into existing slots first
        if (isStackable)
        {
            for (int i = 0; i < _serverInventory.Count && remaining > 0; i++)
            {
                var slot = _serverInventory[i];
                if (slot.isEmpty || slot.itemName.ToString() != itemName.ToString()) continue;

                // Check stacking compatibility (same durability/weight for non-amount items)
                if (!itemData.usesAmountValue)
                {
                    if (slot.durability != durability || Mathf.Abs(slot.weight - weight) > 0.001f)
                        continue;
                }

                int space = itemData.maxStack - slot.stackCount;
                if (space <= 0) continue;

                int added = Mathf.Min(space, remaining);
                slot.stackCount += added;
                _serverInventory[i] = slot;
                remaining -= added;
            }
        }

        // Place remaining into empty slots
        while (remaining > 0)
        {
            int freeSlot = FindFreeSlot();
            if (freeSlot == -1)
            {
                UnityEngine.Debug.LogWarning($"[Inventory] Server: No room for '{itemName}', {remaining} remaining.");
                break;
            }

            int amountToAdd = isStackable ? Mathf.Min(remaining, itemData.maxStack) : 1;

            _serverInventory[freeSlot] = new NetworkInventorySlotData
            {
                itemName = itemName,
                durability = durability,
                weight = weight,
                stackCount = amountToAdd,
                isEmpty = false,
            };

            remaining -= amountToAdd;
        }
    }

    [ServerRpc]
    public void RequestMoveItemServerRpc(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex)) return;
        if (fromIndex == toIndex) return;

        var temp = _serverInventory[toIndex];
        _serverInventory[toIndex] = _serverInventory[fromIndex];
        _serverInventory[fromIndex] = temp;
    }

    [ServerRpc]
    public void RequestDropItemServerRpc(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex)) return;

        var slotData = _serverInventory[slotIndex];
        if (slotData.isEmpty) return;

        ItemData itemData = ResolveItemData(slotData.itemName.ToString());
        if (itemData == null || itemData.dropPrefab == null)
        {
            UnityEngine.Debug.LogWarning($"[Inventory] Server: Cannot drop '{slotData.itemName}', no drop prefab.");
            return;
        }

        // Get player position for spawning
        Vector3 spawnPos = transform.position + transform.forward + Vector3.up;

        GameObject spawnedObject = Instantiate(itemData.dropPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        Item itemComponent = spawnedObject.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.ApplyInstanceData(new ItemInstanceData(
                itemData, slotData.durability, slotData.weight, slotData.stackCount));
        }

        // Apply weight change for the whole stack
        ServerUtility.ApplyWeightToPlayer(OwnerClientId, -(slotData.weight * slotData.stackCount));

        // Clear the slot
        _serverInventory[slotIndex] = NetworkInventorySlotData.Empty;
    }

    [ServerRpc]
    public void RequestConsumeItemServerRpc(int slotIndex, int amount)
    {
        if (!IsValidSlotIndex(slotIndex)) return;

        var slotData = _serverInventory[slotIndex];
        if (slotData.isEmpty) return;

        ItemData itemData = ResolveItemData(slotData.itemName.ToString());
        if (itemData == null || !itemData.isConsumable)
        {
            UnityEngine.Debug.LogWarning($"[Inventory] Server rejected consume: '{slotData.itemName}' is not consumable.");
            return;
        }

        int removeAmount = Mathf.Clamp(amount, 1, Mathf.Max(1, slotData.stackCount));

        // Deduct weight for the consumed items
        ServerUtility.ApplyWeightToPlayer(OwnerClientId, -(slotData.weight * removeAmount));

        // Apply effects server-side
        var statsHandler = GetComponent<Blocks.Gameplay.Core.CoreStatsHandler>();
        if (statsHandler != null)
        {
            UnityEngine.Debug.Log($"[Inventory] Consuming '{slotData.itemName}' x{removeAmount} for player {OwnerClientId}.");
            
            if (itemData.itemName == "Antidote") {
                // Antidote: Starts a restorative routine that cures toxicity
                var survivalSys = statsHandler.GetComponent<PlayerSurvivalSystem>();
                if (survivalSys != null)
                {
                    survivalSys.AddAntidoteRoutine(slotData.durability * removeAmount);
                }
                else
                {
                    // Fallback
                    float reductionAmount = -slotData.durability * removeAmount;
                    statsHandler.ModifyStat(SurvivalStatKeys.Toxic, reductionAmount, OwnerClientId, Blocks.Gameplay.Core.ModificationSource.Natural);
                    UnityEngine.Debug.Log($"[Survival] Player used Antidote (Fallback)! Cured {Mathf.Abs(reductionAmount)} Toxic.");
                }
            
            } else if (itemData.itemName == "Bandage") {
                // Bandage: reduces Pain based on durability
                float reductionAmount = -(slotData.durability) * removeAmount;
                statsHandler.ModifyStat(SurvivalStatKeys.Pain, reductionAmount, OwnerClientId, Blocks.Gameplay.Core.ModificationSource.Natural);
                UnityEngine.Debug.Log($"[Survival] Player used Bandage! Reduced Pain by {Mathf.Abs(reductionAmount)}.");

            } else {
                // Food: reduces hunger based on durability
                float reductionAmount = -(slotData.durability / GameConstants.HungerReductionDivisor) * removeAmount;
                statsHandler.ModifyStat(SurvivalStatKeys.Hunger, reductionAmount, OwnerClientId, Blocks.Gameplay.Core.ModificationSource.Natural);

                // Rotten food chance
                if (itemData.toxicChance > 0f && Random.value <= itemData.toxicChance)
                {
                    float toxicAmount = Random.Range(GameConstants.RottenToxicMin, GameConstants.RottenToxicMax);
                    
                    var survivalSys = statsHandler.GetComponent<PlayerSurvivalSystem>();
                    if (survivalSys != null)
                    {
                        survivalSys.AddDelayedToxicity(toxicAmount);
                        UnityEngine.Debug.Log($"[Survival] Player ate food that will cause a stomach ache! Pending Toxic: {toxicAmount}");
                    }
                    else
                    {
                        // Fallback
                        statsHandler.ModifyStat(SurvivalStatKeys.Toxic, toxicAmount, OwnerClientId, Blocks.Gameplay.Core.ModificationSource.Environmental);
                        UnityEngine.Debug.Log($"[Survival] Player ate food that caused instant toxicity! Added {toxicAmount} Toxic.");
                    }
                }
            }
        }

        // Update stack
        if (slotData.stackCount > removeAmount)
        {
            slotData.stackCount -= removeAmount;
            _serverInventory[slotIndex] = slotData;
        }
        else
        {
            _serverInventory[slotIndex] = NetworkInventorySlotData.Empty;
        }
    }

    [ServerRpc]
    public void RequestRemoveItemFromSlotServerRpc(int slotIndex, int amount, FixedString32Bytes expectedItemName)
    {
        if (!IsValidSlotIndex(slotIndex) || amount <= 0) return;

        var slotData = _serverInventory[slotIndex];
        if (slotData.isEmpty) return;

        string expectedName = expectedItemName.ToString();
        if (!string.IsNullOrEmpty(expectedName) && slotData.itemName.ToString() != expectedName)
        {
            UnityEngine.Debug.LogWarning($"[Inventory] Server rejected slot remove: slot {slotIndex} has '{slotData.itemName}', expected '{expectedName}'.");
            return;
        }

        int removeAmount = Mathf.Clamp(amount, 1, Mathf.Max(1, slotData.stackCount));

        ServerUtility.ApplyWeightToPlayer(OwnerClientId, -(slotData.weight * removeAmount));

        if (slotData.stackCount > removeAmount)
        {
            slotData.stackCount -= removeAmount;
            _serverInventory[slotIndex] = slotData;
        }
        else
        {
            _serverInventory[slotIndex] = NetworkInventorySlotData.Empty;
        }

        UnityEngine.Debug.Log($"[Inventory] Removed '{slotData.itemName}' x{removeAmount} from slot {slotIndex} for player {OwnerClientId}.");
    }

    [ServerRpc]
    public void RequestConsumeItemByNameServerRpc(FixedString32Bytes itemName, int amount)
    {
        if (amount <= 0) return;

        string name = itemName.ToString();
        int remaining = amount;

        for (int i = 0; i < _serverInventory.Count && remaining > 0; i++)
        {
            var slot = _serverInventory[i];
            if (slot.isEmpty || slot.itemName.ToString() != name) continue;

            int removeAmount = Mathf.Min(remaining, Mathf.Max(1, slot.stackCount));

            // Deduct weight
            ServerUtility.ApplyWeightToPlayer(OwnerClientId, -(slot.weight * removeAmount));

            if (slot.stackCount > removeAmount)
            {
                slot.stackCount -= removeAmount;
                _serverInventory[i] = slot;
            }
            else
            {
                _serverInventory[i] = NetworkInventorySlotData.Empty;
            }

            remaining -= removeAmount;
        }
    }

    // ── Client Sync ──────────────────────────────────────────────

    private void OnServerInventoryChanged(NetworkListEvent<NetworkInventorySlotData> changeEvent)
    {
        if (!IsOwner) return;
        if (InventoryManager.instance == null) return;

        InventoryManager.instance.ApplyServerSlotUpdate(changeEvent.Index, changeEvent.Value);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private int FindFreeSlot()
    {
        for (int i = 0; i < _serverInventory.Count; i++)
        {
            if (_serverInventory[i].isEmpty) return i;
        }
        return -1;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < _serverInventory.Count;
    }

    private ItemData ResolveItemData(string itemName)
    {
        if (serverItemRegistry != null)
        {
            foreach (var item in serverItemRegistry)
            {
                if (item != null && item.itemName == itemName) return item;
            }
        }

        // Fallback: search all loaded ItemData assets
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.itemName == itemName) return item;
        }

        return null;
    }

    /// <summary>
    /// Get the server-side slot data count for verification.
    /// </summary>
    public int ServerSlotCount => _serverInventory != null ? _serverInventory.Count : 0;

    /// <summary>
    /// Read server slot data at index (for queries from game systems).
    /// </summary>
    public NetworkInventorySlotData GetServerSlotData(int index)
    {
        if (index < 0 || index >= _serverInventory.Count) return NetworkInventorySlotData.Empty;
        return _serverInventory[index];
    }

    /// <summary>
    /// Check if a specific item exists in hotslots (quickslots, indices 0 to quickSlotCount-1).
    /// Game systems can use this to check conditions like "has shield in hotslot".
    /// </summary>
    public bool HasItemInHotSlot(string itemName, int quickSlotCount = 5)
    {
        int count = Mathf.Min(quickSlotCount, _serverInventory.Count);
        for (int i = 0; i < count; i++)
        {
            if (!_serverInventory[i].isEmpty && _serverInventory[i].itemName.ToString() == itemName)
                return true;
        }
        return false;
    }
}

/// <summary>
/// Network-serializable inventory slot data for the server's NetworkList.
/// </summary>
public struct NetworkInventorySlotData : INetworkSerializable, System.IEquatable<NetworkInventorySlotData>
{
    public FixedString32Bytes itemName;
    public int durability;
    public float weight;
    public int stackCount;
    public bool isEmpty;

    public static NetworkInventorySlotData Empty => new NetworkInventorySlotData
    {
        itemName = "",
        durability = 0,
        weight = 0f,
        stackCount = 0,
        isEmpty = true,
    };

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref durability);
        serializer.SerializeValue(ref weight);
        serializer.SerializeValue(ref stackCount);
        serializer.SerializeValue(ref isEmpty);
    }

    public bool Equals(NetworkInventorySlotData other)
    {
        return itemName.Equals(other.itemName) &&
               durability == other.durability &&
               Mathf.Approximately(weight, other.weight) &&
               stackCount == other.stackCount &&
               isEmpty == other.isEmpty;
    }

    public override bool Equals(object obj) => obj is NetworkInventorySlotData other && Equals(other);
    public override int GetHashCode() => itemName.GetHashCode();
}
