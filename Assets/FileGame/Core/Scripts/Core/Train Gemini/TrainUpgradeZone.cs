using Blocks.Gameplay.Core;
using UnityEngine;

[DisallowMultipleComponent]
public class TrainUpgradeZone : MonoBehaviour, IInteractable, IHoldInteractable, IInteractionPromptDetailsProvider, IInteractionPromptViewProvider
{
    [Header("References")]
    [SerializeField] private TrainUpgradeSystem upgradeSystem;

    [Header("Interaction Settings")]
    [SerializeField] private float holdDuration = 1.25f;
    [SerializeField] private int priority = 15;

    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => priority;
    public float HoldDuration => holdDuration;

    public string InteractionPromptText
    {
        get
        {
            if (upgradeSystem == null)
            {
                return "Upgrade System Offline";
            }

            if (upgradeSystem.HasReachedMaxLevel)
            {
                return "Train Max Level";
            }

            if (!upgradeSystem.IsTrainStopped)
            {
                return "Stop the train to upgrade";
            }

            if (!HasEnoughScrap())
            {
                return $"Need Scrap Metal for Lv.{upgradeSystem.NextLevelNumber}";
            }

            return $"Upgrade Train to Lv.{upgradeSystem.NextLevelNumber} and repair to full";
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return upgradeSystem != null && !upgradeSystem.HasReachedMaxLevel;
    }

    public void Interact(GameObject interactor)
    {
        // Hold interactions are handled by InteractionAddon.
    }

    public bool CanHoldInteract(GameObject interactor)
    {
        return upgradeSystem != null &&
               upgradeSystem.CanUpgradeNow &&
               HasEnoughScrap();
    }

    public void OnHoldInteractStarted(GameObject interactor)
    {
    }

    public void OnHoldInteractProgress(GameObject interactor, float progress)
    {
    }

    public void OnHoldInteractCanceled(GameObject interactor)
    {
    }

    public void OnHoldInteractCompleted(GameObject interactor)
    {
        if (upgradeSystem == null || !CanHoldInteract(interactor))
        {
            return;
        }

        upgradeSystem.RequestUpgradeServerRpc(GetOwnedScrapCount());
    }

    public string GetPromptKeyText(GameObject interactor)
    {
        if (upgradeSystem == null)
        {
            return string.Empty;
        }

        if (upgradeSystem.HasReachedMaxLevel)
        {
            return "MAX";
        }

        if (!upgradeSystem.IsTrainStopped)
        {
            return "Stop";
        }

        return HasEnoughScrap() ? "Hold E" : "Locked";
    }

    public bool TryGetPromptRequirement(GameObject interactor, out InteractionPromptRequirementData requirement)
    {
        requirement = default(InteractionPromptRequirementData);

        if (upgradeSystem == null || upgradeSystem.HasReachedMaxLevel || upgradeSystem.ScrapMetalItem == null)
        {
            return false;
        }

        int requiredScrap = upgradeSystem.GetRequiredScrapForNextLevel();
        if (requiredScrap <= 0)
        {
            return false;
        }

        int ownedScrap = GetOwnedScrapCount();
        requirement = new InteractionPromptRequirementData(
            upgradeSystem.ScrapMetalItem.itemPicture,
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

        if (upgradeSystem != null && upgradeSystem.TryBuildPromptTrainData(GetOwnedScrapCount(), out InteractionPromptTrainData trainData))
        {
            viewData.TrainData = trainData;
            return true;
        }

        return true;
    }

    private bool HasEnoughScrap()
    {
        if (upgradeSystem == null || upgradeSystem.ScrapMetalItem == null)
        {
            return false;
        }

        return InventoryManager.instance != null &&
               InventoryManager.instance.HasItemAmount(upgradeSystem.ScrapMetalItem, upgradeSystem.GetRequiredScrapForNextLevel());
    }

    private int GetOwnedScrapCount()
    {
        if (upgradeSystem == null || upgradeSystem.ScrapMetalItem == null || InventoryManager.instance == null)
        {
            return 0;
        }

        return InventoryManager.instance.GetTotalItemCount(upgradeSystem.ScrapMetalItem);
    }
}
