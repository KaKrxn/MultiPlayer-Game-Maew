using Blocks.Gameplay.Core;
using System;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class TrainRepairZone : MonoBehaviour, IInteractable, IHoldInteractable, IInteractionPromptDetailsProvider, IInteractionPromptViewProvider
{
    public static event Action<float, bool> OnLocalRepairHoldStateChanged;

    [Header("References")]
    [SerializeField] private TrainUpgradeSystem trainSystem;

    [Header("Interaction Settings")]
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private int priority = 16;
    [SerializeField] private bool debugRepairChecks = true;

    private string _lastDebugReason;
    private float _nextDebugTime;
    private float _fallbackHoldTimer;
    private float _nextRepairRequestTime;
    private bool _fallbackWaitingForRelease;

    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => priority;
    public float HoldDuration => holdDuration;

    public string InteractionPromptText
    {
        get
        {
            return trainSystem != null && trainSystem.CanRepairNow && HasEnoughSelectedScrap()
                ? $"Repair Train +{trainSystem.RepairAmountPerUse:0} HP"
                : string.Empty;
        }
    }

    private void Reset()
    {
        trainSystem = GetComponentInParent<TrainUpgradeSystem>();
    }

    private void Awake()
    {
        if (trainSystem == null)
            trainSystem = GetComponentInParent<TrainUpgradeSystem>();

        if (trainSystem != null)
            trainSystem.RegisterRepairAnchor(transform);
    }

    private void Update()
    {
        UpdateLocalFallbackRepairInput();
    }

    public bool CanInteract(GameObject interactor)
    {
        bool canInteract = TryGetRepairBlockReason(interactor, out string reason);
        if (!canInteract)
            LogRepairBlocked(reason);

        return canInteract;
    }

    public void Interact(GameObject interactor)
    {
        // Hold interactions are handled by InteractionAddon.
    }

    public bool CanHoldInteract(GameObject interactor)
    {
        bool canHold = TryGetRepairBlockReason(interactor, out string reason);
        if (!canHold)
            LogRepairBlocked(reason);

        return canHold;
    }

    public void OnHoldInteractStarted(GameObject interactor)
    {
        Debug.Log("[TrainRepairZone] Hold repair started.");
    }

    public void OnHoldInteractProgress(GameObject interactor, float progress)
    {
    }

    public void OnHoldInteractCanceled(GameObject interactor)
    {
        Debug.Log("[TrainRepairZone] Hold repair canceled.");
    }

    public void OnHoldInteractCompleted(GameObject interactor)
    {
        if (trainSystem == null || !CanHoldInteract(interactor))
        {
            Debug.LogWarning("[TrainRepairZone] Hold completed but repair conditions failed before RPC.");
            return;
        }

        TrySendRepairRequest(interactor, "InteractionAddon");
    }

    public string GetPromptKeyText(GameObject interactor)
    {
        return CanInteract(interactor) ? "Hold E" : string.Empty;
    }

    public bool TryGetPromptRequirement(GameObject interactor, out InteractionPromptRequirementData requirement)
    {
        requirement = default(InteractionPromptRequirementData);

        if (trainSystem == null || trainSystem.ScrapMetalItem == null || !trainSystem.CanRepairNow)
        {
            return false;
        }

        int ownedScrap = GetSelectedScrapCount();
        int requiredScrap = trainSystem.ScrapMetalCostPerRepair;
        requirement = new InteractionPromptRequirementData(
            trainSystem.ScrapMetalItem.itemPicture,
            $"{ownedScrap}/{requiredScrap}",
            ownedScrap >= requiredScrap ? new Color(0.45f, 1f, 0.45f, 1f) : new Color(1f, 0.45f, 0.45f, 1f));
        return true;
    }

    public bool TryBuildPromptView(InteractionPromptContext context, out InteractionPromptViewData viewData)
    {
        string keyText = GetPromptKeyText(context.Interactor);
        viewData = new InteractionPromptViewData(keyText, InteractionPromptText, InteractionPromptVariant.TrainUpgrade)
        {
            ShowHoldProgress = context.IsHoldInteractable && CanHoldInteract(context.Interactor),
            HoldProgress = context.HoldProgress
        };

        if (trainSystem == null)
        {
            return true;
        }

        float healthNormalized = trainSystem.MaxHealth <= 0f ? 0f : trainSystem.CurrentHealth / trainSystem.MaxHealth;
        float repairedHealth = Mathf.Min(trainSystem.MaxHealth, trainSystem.CurrentHealth + trainSystem.RepairAmountPerUse);
        float repairedNormalized = trainSystem.MaxHealth <= 0f ? 0f : repairedHealth / trainSystem.MaxHealth;

        InteractionPromptRequirementData[] requirements;
        if (TryGetPromptRequirement(context.Interactor, out InteractionPromptRequirementData requirement))
        {
            requirements = new[] { requirement };
        }
        else
        {
            requirements = new InteractionPromptRequirementData[0];
        }

        InteractionPromptStatCompareRow[] compareRows =
        {
            new InteractionPromptStatCompareRow(
                "HP",
                trainSystem.CurrentHealth.ToString("0"),
                repairedHealth.ToString("0"),
                healthNormalized,
                repairedNormalized,
                new Color(0.45f, 1f, 0.45f, 1f)),
            new InteractionPromptStatCompareRow(
                "REPAIR",
                "0",
                "+" + trainSystem.RepairAmountPerUse.ToString("0"),
                0f,
                Mathf.Clamp01(trainSystem.RepairAmountPerUse / Mathf.Max(1f, trainSystem.MaxHealth)),
                new Color(0.45f, 1f, 0.45f, 1f)),
            new InteractionPromptStatCompareRow(
                "COST",
                "0",
                trainSystem.ScrapMetalCostPerRepair.ToString(),
                0f,
                1f,
                Color.white),
            new InteractionPromptStatCompareRow(
                "SLOT",
                GetSelectedScrapCount().ToString(),
                trainSystem.ScrapMetalCostPerRepair.ToString(),
                Mathf.Clamp01(GetSelectedScrapCount() / Mathf.Max(1f, trainSystem.ScrapMetalCostPerRepair)),
                1f,
                HasEnoughSelectedScrap() ? new Color(0.45f, 1f, 0.45f, 1f) : new Color(1f, 0.45f, 0.45f, 1f))
        };

        viewData.TrainData = new InteractionPromptTrainData(
            "Train",
            "Repair",
            healthNormalized,
            $"{trainSystem.CurrentHealth:0}/{trainSystem.MaxHealth:0}",
            trainSystem.MaxFuel <= 0f ? 0f : trainSystem.CurrentFuel / trainSystem.MaxFuel,
            $"{trainSystem.CurrentFuel:0}/{trainSystem.MaxFuel:0}",
            compareRows,
            requirements);
        return true;
    }

    private bool HasEnoughSelectedScrap()
    {
        return trainSystem != null &&
               trainSystem.ScrapMetalItem != null &&
               InventoryManager.instance != null &&
               InventoryManager.instance.HasSelectedQuickSlotItemAmount(
                   trainSystem.ScrapMetalItem,
                   trainSystem.ScrapMetalCostPerRepair);
    }

    private int GetSelectedScrapCount()
    {
        if (trainSystem == null || trainSystem.ScrapMetalItem == null || InventoryManager.instance == null)
        {
            return 0;
        }

        return InventoryManager.instance.GetSelectedQuickSlotItemCount(trainSystem.ScrapMetalItem);
    }

    private bool IsInteractorInRange(GameObject interactor)
    {
        if (trainSystem == null || interactor == null)
        {
            return false;
        }

        float range = trainSystem.RepairRange;
        return range <= 0f || Vector3.Distance(interactor.transform.position, transform.position) <= range;
    }

    private bool TryGetRepairBlockReason(GameObject interactor, out string reason)
    {
        if (trainSystem == null)
        {
            reason = "missing TrainUpgradeSystem reference";
            return false;
        }

        if (!trainSystem.CanRepairNow)
        {
            reason = $"train HP is full or invalid ({trainSystem.CurrentHealth:0}/{trainSystem.MaxHealth:0})";
            return false;
        }

        if (!IsInteractorInRange(interactor))
        {
            float range = trainSystem.RepairRange;
            float distance = interactor != null ? Vector3.Distance(interactor.transform.position, transform.position) : -1f;
            reason = $"player outside repair range distance={distance:0.00}, required<={range:0.00}";
            return false;
        }

        if (trainSystem.ScrapMetalItem == null)
        {
            reason = "ScrapMetalItem is not assigned on TrainUpgradeSystem";
            return false;
        }

        if (InventoryManager.instance == null)
        {
            reason = "InventoryManager.instance is null";
            return false;
        }

        int selectedScrap = GetSelectedScrapCount();
        int requiredScrap = trainSystem.ScrapMetalCostPerRepair;
        if (selectedScrap < requiredScrap)
        {
            reason = $"selected Quick Slot does not have enough Scrap Metal ({selectedScrap}/{requiredScrap})";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private void LogRepairBlocked(string reason)
    {
        if (!debugRepairChecks || string.IsNullOrEmpty(reason) || Time.time < _nextDebugTime)
        {
            return;
        }

        if (reason == _lastDebugReason && Time.time < _nextDebugTime)
        {
            return;
        }

        _lastDebugReason = reason;
        _nextDebugTime = Time.time + 1f;
        Debug.Log($"[TrainRepairZone] Repair prompt hidden/blocked: {reason}");
    }

    private void UpdateLocalFallbackRepairInput()
    {
        if (!TryGetLocalInteractor(out GameObject interactor))
        {
            CancelFallbackHold();
            return;
        }

        bool isHoldingRepairKey = Input.GetKey(KeyCode.E);
        if (!isHoldingRepairKey)
        {
            _fallbackWaitingForRelease = false;
            CancelFallbackHold();
            return;
        }

        if (_fallbackWaitingForRelease)
        {
            return;
        }

        if (!TryGetRepairBlockReason(interactor, out string reason))
        {
            LogRepairBlocked(reason);
            CancelFallbackHold();
            return;
        }

        if (_fallbackHoldTimer <= 0f)
        {
            Debug.Log("[TrainRepairZone] Fallback hold repair started.");
        }

        _fallbackHoldTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_fallbackHoldTimer / Mathf.Max(0.01f, holdDuration));
        OnLocalRepairHoldStateChanged?.Invoke(progress, true);

        if (progress < 1f)
        {
            return;
        }

        TrySendRepairRequest(interactor, "FallbackInput");
        _fallbackHoldTimer = 0f;
        _fallbackWaitingForRelease = true;
        OnLocalRepairHoldStateChanged?.Invoke(1f, false);
    }

    private void CancelFallbackHold()
    {
        if (_fallbackHoldTimer <= 0f)
        {
            return;
        }

        _fallbackHoldTimer = 0f;
        OnLocalRepairHoldStateChanged?.Invoke(0f, false);
    }

    private void TrySendRepairRequest(GameObject interactor, string source)
    {
        if (Time.time < _nextRepairRequestTime)
        {
            return;
        }

        if (trainSystem == null || !CanHoldInteract(interactor))
        {
            Debug.LogWarning($"[TrainRepairZone] {source} completed but repair conditions failed before RPC.");
            return;
        }

        _nextRepairRequestTime = Time.time + 0.35f;
        Debug.Log($"[TrainRepairZone] {source} repair completed. Sending repair RPC with scrap={GetSelectedScrapCount()}.");
        trainSystem.RequestRepairServerRpc(GetSelectedScrapCount());
    }

    private static bool TryGetLocalInteractor(out GameObject interactor)
    {
        interactor = null;
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsConnectedClient ||
            NetworkManager.Singleton.LocalClient == null ||
            NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            return false;
        }

        interactor = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        return interactor != null;
    }
}
