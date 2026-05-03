using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class Item : NetworkBehaviour, IInteractable 
{
    [SerializeField] private ItemData itemData;
    public ItemData Data => itemData;

    [Header("Item State")]
    [SerializeField] private int durability = 100;
    [SerializeField] private float weightKg = 0.1f;

    private readonly NetworkVariable<int> durabilityNetwork = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> weightKgNetwork = new NetworkVariable<float>(
        0.1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int Durability 
    { 
        get => IsSpawned ? durabilityNetwork.Value : durability;
        set
        {
            int clampedValue = Mathf.Clamp(value, 0, 100);
            durability = clampedValue;

            if (!IsSpawned || IsServer)
            {
                durabilityNetwork.Value = clampedValue;
            }
        }
    }

    public float WeightKg
    {
        get => IsSpawned ? weightKgNetwork.Value : weightKg;
        set
        {
            float roundedWeight = WeightedRandomUtility.RoundWeight(value);
            weightKg = roundedWeight;

            if (!IsSpawned || IsServer)
            {
                weightKgNetwork.Value = roundedWeight;
            }
        }
    }

    public float WeightDebuffPercent => Mathf.Max(0f, WeightKg * 10f);
    public string DurabilityDisplayText => $"{Durability}%";
    public string WeightDisplayText => $"{WeightKg:0.0}kg";
    public string WeightDebuffDisplayText => $"{WeightDebuffPercent:0.#}%";

    private DestroyNetworkItemSync _networkSync;

    private void Awake()
    {
        _networkSync = GetComponent<DestroyNetworkItemSync>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            durabilityNetwork.Value = durability;
            weightKgNetwork.Value = WeightedRandomUtility.RoundWeight(weightKg);
        }
        else
        {
            durability = durabilityNetwork.Value;
            weightKg = weightKgNetwork.Value;
        }
    }

    [ContextMenu("Pickup Item")]
    public void Pickup()
    {
        if (InventoryManager.instance != null && !InventoryManager.instance.HasFreeSlot())
        {
            Debug.Log($"[Item] Inventory is full, cannot pick up {itemData?.itemName ?? "item"}.");
            return;
        }

        if (_networkSync != null)
        {
            _networkSync.RequestPickup();
        }
        else
        {
            Debug.LogWarning("ไม่มีสคริปต์ DestroyNetworkItemSync แปะอยู่บนไอเทมชิ้นนี้!");
        }
    }

    // --- IInteractable Implementation ---
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 5;
    public string InteractionPromptText => "Pick Up " + (itemData != null ? itemData.itemName : "Item");

    public bool CanInteract(GameObject interactor)
    {
        return InventoryManager.instance == null || InventoryManager.instance.HasFreeSlot();
    }

    public void Interact(GameObject interactor)
    {
        Pickup();
    }

    public ItemInstanceData BuildItemInstanceData()
    {
        return new ItemInstanceData(itemData, Durability, WeightKg);
    }

    public void ApplyInstanceData(ItemInstanceData itemInstanceData)
    {
        itemData = itemInstanceData.itemData;
        Durability = itemInstanceData.durabilityPercent;
        WeightKg = itemInstanceData.weightKg;
    }
}
