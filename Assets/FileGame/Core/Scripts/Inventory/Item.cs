using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class Item : NetworkBehaviour, IInteractable, IInteractionPromptViewProvider
{
    [SerializeField] private ItemData itemData;
    public ItemData Data => itemData;

    [Header("Item State")]
    [SerializeField] private int durability = 100;
    [SerializeField] private float weightKg = 0.1f;
    [SerializeField] private int stackCount = 1;

    private readonly NetworkVariable<int> durabilityNetwork = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> weightKgNetwork = new NetworkVariable<float>(
        0.1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> stackCountNetwork = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Unity.Collections.FixedString32Bytes> itemNameNetwork = new NetworkVariable<Unity.Collections.FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int Durability 
    { 
        get => IsSpawned ? durabilityNetwork.Value : durability;
        set
        {
            int clampedValue = Mathf.Clamp(value, 0, 100);
            durability = clampedValue;

            if (IsSpawned && IsServer)
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

            if (IsSpawned && IsServer)
            {
                weightKgNetwork.Value = roundedWeight;
            }
        }
    }

    public int StackCount
    {
        get => IsSpawned ? stackCountNetwork.Value : stackCount;
        set
        {
            int clampedValue = Mathf.Max(1, value);
            stackCount = clampedValue;

            if (IsSpawned && IsServer)
            {
                stackCountNetwork.Value = clampedValue;
            }
        }
    }

    public float WeightDebuffPercent => Mathf.Max(0f, WeightKg * 10f);
    public bool UsesAmountValue => itemData != null && itemData.usesAmountValue;
    public bool ShouldShowWeightStat => WeightKg > 0.001f;
    public string DurabilityDisplayText => UsesAmountValue ? StackCount.ToString() : $"{Durability}%";
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
            stackCountNetwork.Value = Mathf.Max(1, stackCount);
            if (itemData != null)
            {
                itemNameNetwork.Value = itemData.itemName;
            }
        }
        else
        {
            durability = durabilityNetwork.Value;
            weightKg = weightKgNetwork.Value;
            stackCount = stackCountNetwork.Value;
            
            // Client side sync of itemData from the network name
            string netName = itemNameNetwork.Value.ToString();
            if (itemData == null && !string.IsNullOrEmpty(netName))
            {
                SyncItemDataByName(netName);
            }
        }
    }

    private void SyncItemDataByName(string itemName)
    {
        // Try to find the item data in the project (Resources) or registry
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
            {
                itemData = item;
                break;
            }
        }
    }

    [ContextMenu("Pickup Item")]
    public void Pickup()
    {
        if (InventoryManager.instance != null && !InventoryManager.instance.CanAcceptItem(BuildItemInstanceData()))
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
            Debug.LogWarning("Item is missing DestroyNetworkItemSync component!");
        }
    }

    // --- IInteractable Implementation ---
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 5;
    public string InteractionPromptText => "Pick Up " + (itemData != null ? itemData.itemName : "Item");

    public bool CanInteract(GameObject interactor)
    {
        return InventoryManager.instance == null || InventoryManager.instance.CanAcceptItem(BuildItemInstanceData());
    }

    public void Interact(GameObject interactor)
    {
        Pickup();
    }

    public bool TryBuildPromptView(InteractionPromptContext context, out InteractionPromptViewData viewData)
    {
        string keyText = context.IsHoldInteractable ? "Hold E" : "E";
        InteractionPromptVariant variant = itemData != null && itemData.promptDisplayType == ItemPromptDisplayType.ScrapMetal
            ? InteractionPromptVariant.ScrapMetalItem
            : InteractionPromptVariant.Item;

        viewData = new InteractionPromptViewData(keyText, InteractionPromptText, variant)
        {
            ShowHoldProgress = context.IsHoldInteractable,
            HoldProgress = context.HoldProgress,
            ItemData = BuildPromptItemData(variant)
        };

        return true;
    }

    public ItemInstanceData BuildItemInstanceData()
    {
        return new ItemInstanceData(itemData, Durability, WeightKg, StackCount);
    }

    public void ApplyInstanceData(ItemInstanceData itemInstanceData)
    {
        itemData = itemInstanceData.itemData;
        durability = itemInstanceData.durabilityPercent; // Set backing field
        weightKg = itemInstanceData.weightKg; // Set backing field
        stackCount = itemInstanceData.stackCount;
        
        if (IsServer && IsSpawned)
        {
            durabilityNetwork.Value = durability;
            weightKgNetwork.Value = weightKg;
            stackCountNetwork.Value = stackCount;
            if (itemData != null)
            {
                itemNameNetwork.Value = itemData.itemName;
            }
        }
    }

    private InteractionPromptItemData BuildPromptItemData(InteractionPromptVariant variant)
    {
        bool showAmount = variant == InteractionPromptVariant.ScrapMetalItem;
        bool usesAmount = UsesAmountValue;
        float durabilityNormalized = usesAmount && Data != null
            ? Mathf.Clamp01((float)StackCount / Mathf.Max(1, Data.maxStack))
            : Mathf.Clamp01(Durability / 100f);

        return new InteractionPromptItemData(
            Data != null ? Data.itemPicture : null,
            usesAmount ? StackCount.ToString() : string.Empty,
            DurabilityDisplayText,
            WeightDisplayText,
            showAmount,
            !showAmount,
            !showAmount && ShouldShowWeightStat,
            durabilityNormalized,
            Mathf.Clamp01(WeightDebuffPercent / 100f));
    }
}
