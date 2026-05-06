using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Defines the contract for any object that can be interacted with by a CoreInteractor.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Gets the method by which this interaction is triggered.
        /// </summary>
        InteractionTriggerMode TriggerMode { get; }

        /// <summary>
        /// Gets the priority of this interactable. When multiple interactables are detected,
        /// the one with the highest priority value is chosen as the focus.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the text to display on the UI prompt.
        /// Should return string.Empty for automatically triggered interactions.
        /// </summary>
        string InteractionPromptText { get; }

        /// <summary>
        /// Determines if the specified interactor can currently interact with this object.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Executes the interaction logic.
        /// </summary>
        void Interact(GameObject interactor);
    }

    public interface IHoldInteractable
    {
        float HoldDuration { get; }
        bool CanHoldInteract(GameObject interactor);
        void OnHoldInteractStarted(GameObject interactor);
        void OnHoldInteractProgress(GameObject interactor, float progress);
        void OnHoldInteractCanceled(GameObject interactor);
        void OnHoldInteractCompleted(GameObject interactor);
    }

    public interface IInteractionPromptDetailsProvider
    {
        string GetPromptKeyText(GameObject interactor);
        bool TryGetPromptRequirement(GameObject interactor, out InteractionPromptRequirementData requirement);
    }

    public interface IInteractionPromptViewProvider
    {
        bool TryBuildPromptView(InteractionPromptContext context, out InteractionPromptViewData viewData);
    }

    public enum InteractionPromptVariant
    {
        Generic,
        Item,
        ScrapMetalItem,
        TrainUpgrade
    }

    public struct InteractionPromptContext
    {
        public GameObject Interactor { get; }
        public bool IsHoldInteractable { get; }
        public bool IsHolding { get; }
        public float HoldProgress { get; }

        public InteractionPromptContext(GameObject interactor, bool isHoldInteractable, bool isHolding, float holdProgress)
        {
            Interactor = interactor;
            IsHoldInteractable = isHoldInteractable;
            IsHolding = isHolding;
            HoldProgress = Mathf.Clamp01(holdProgress);
        }
    }

    public struct InteractionPromptViewData
    {
        public string KeyText;
        public string DescriptionText;
        public InteractionPromptVariant Variant;
        public bool ShowHoldProgress;
        public float HoldProgress;
        public InteractionPromptItemData ItemData;
        public InteractionPromptTrainData TrainData;

        public InteractionPromptViewData(string keyText, string descriptionText, InteractionPromptVariant variant)
        {
            KeyText = keyText;
            DescriptionText = descriptionText;
            Variant = variant;
            ShowHoldProgress = false;
            HoldProgress = 0f;
            ItemData = default(InteractionPromptItemData);
            TrainData = default(InteractionPromptTrainData);
        }
    }

    public struct InteractionPromptRequirementData
    {
        public bool IsValid;
        public Sprite Icon;
        public string AmountText;
        public Color AmountColor;

        public InteractionPromptRequirementData(Sprite icon, string amountText, Color amountColor)
        {
            IsValid = true;
            Icon = icon;
            AmountText = amountText;
            AmountColor = amountColor;
        }
    }

    public struct InteractionPromptItemData
    {
        public bool IsValid;
        public Sprite Icon;
        public string AmountText;
        public string DurabilityText;
        public string WeightText;
        public bool ShowAmount;
        public bool ShowDurability;
        public bool ShowWeight;
        public float DurabilityNormalized;
        public float WeightNormalized;

        public InteractionPromptItemData(
            Sprite icon,
            string amountText,
            string durabilityText,
            string weightText,
            bool showAmount,
            bool showDurability,
            bool showWeight,
            float durabilityNormalized,
            float weightNormalized)
        {
            IsValid = true;
            Icon = icon;
            AmountText = amountText;
            DurabilityText = durabilityText;
            WeightText = weightText;
            ShowAmount = showAmount;
            ShowDurability = showDurability;
            ShowWeight = showWeight;
            DurabilityNormalized = Mathf.Clamp01(durabilityNormalized);
            WeightNormalized = Mathf.Clamp01(weightNormalized);
        }
    }

    public struct InteractionPromptStatCompareRow
    {
        public string Label;
        public string CurrentValueText;
        public string NextValueText;
        public float CurrentNormalized;
        public float NextNormalized;
        public Color DeltaColor;

        public InteractionPromptStatCompareRow(
            string label,
            string currentValueText,
            string nextValueText,
            float currentNormalized,
            float nextNormalized,
            Color deltaColor)
        {
            Label = label;
            CurrentValueText = currentValueText;
            NextValueText = nextValueText;
            CurrentNormalized = Mathf.Clamp01(currentNormalized);
            NextNormalized = Mathf.Clamp01(nextNormalized);
            DeltaColor = deltaColor;
        }
    }

    public struct InteractionPromptTrainData
    {
        public bool IsValid;
        public string CurrentLevelLabel;
        public string NextLevelLabel;
        public float HealthNormalized;
        public string HealthText;
        public float FuelNormalized;
        public string FuelText;
        public InteractionPromptStatCompareRow[] CompareRows;
        public InteractionPromptRequirementData[] Requirements;

        public InteractionPromptTrainData(
            string currentLevelLabel,
            string nextLevelLabel,
            float healthNormalized,
            string healthText,
            float fuelNormalized,
            string fuelText,
            InteractionPromptStatCompareRow[] compareRows,
            InteractionPromptRequirementData[] requirements)
        {
            IsValid = true;
            CurrentLevelLabel = currentLevelLabel;
            NextLevelLabel = nextLevelLabel;
            HealthNormalized = Mathf.Clamp01(healthNormalized);
            HealthText = healthText;
            FuelNormalized = Mathf.Clamp01(fuelNormalized);
            FuelText = fuelText;
            CompareRows = compareRows;
            Requirements = requirements;
        }
    }
}

public enum InteractionTriggerMode
{
    OnButtonPress, // Triggered by player input.
    OnFocusEnter, // Triggered automatically when the object gains focus.
    OnCharacterControllerHit, // Triggered automatically when a CharacterController collides with it.
    OnTriggerEnter, // Triggered by a standard physics trigger collision.
    OnRigidbodyCollision // Triggered by a standard physics collision.
}
