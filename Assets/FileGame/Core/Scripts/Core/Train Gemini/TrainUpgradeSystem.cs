using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct TrainUpgradeLevel
{
    public string levelName;
    public float maxHealth;
    public float maxSpeed;
    public float fuelConsumptionRate;
    public float brakeDeceleration;
    public int scrapRequired;
    public GameObject visualPrefab;
}

public struct TrainUpgradeLevelPreview
{
    public string LevelName { get; }
    public float MaxHealth { get; }
    public float MaxSpeed { get; }
    public float FuelConsumptionRate { get; }
    public float BrakeDeceleration { get; }
    public int ScrapRequired { get; }

    public TrainUpgradeLevelPreview(
        string levelName,
        float maxHealth,
        float maxSpeed,
        float fuelConsumptionRate,
        float brakeDeceleration,
        int scrapRequired)
    {
        LevelName = levelName;
        MaxHealth = maxHealth;
        MaxSpeed = maxSpeed;
        FuelConsumptionRate = fuelConsumptionRate;
        BrakeDeceleration = brakeDeceleration;
        ScrapRequired = scrapRequired;
    }
}

public class TrainUpgradeSystem : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private AutomatedNetworkTransform trainMovement;
    [SerializeField] private TrainFuelSystem fuelSystem;
    [SerializeField] private Transform visualAnchor;
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private ItemData scrapMetalItem;

    [Header("Upgrade Levels")]
    [SerializeField] private TrainUpgradeLevel[] levels =
    {
        new TrainUpgradeLevel
        {
            levelName = "MK I",
            maxHealth = 100f,
            maxSpeed = 40f,
            fuelConsumptionRate = 2f,
            brakeDeceleration = 5f,
            scrapRequired = 0,
            visualPrefab = null
        },
        new TrainUpgradeLevel
        {
            levelName = "MK II",
            maxHealth = 160f,
            maxSpeed = 48f,
            fuelConsumptionRate = 1.7f,
            brakeDeceleration = 7f,
            scrapRequired = 25,
            visualPrefab = null
        },
        new TrainUpgradeLevel
        {
            levelName = "MK III",
            maxHealth = 240f,
            maxSpeed = 56f,
            fuelConsumptionRate = 1.4f,
            brakeDeceleration = 9f,
            scrapRequired = 60,
            visualPrefab = null
        }
    };

    private readonly NetworkVariable<int> m_CurrentLevelIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> m_CurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private GameObject m_CurrentVisualInstance;

    public ItemData ScrapMetalItem => scrapMetalItem;
    public int CurrentLevelNumber => MaxLevelCount > 0 ? Mathf.Clamp(m_CurrentLevelIndex.Value + 1, 1, MaxLevelCount) : 0;
    public int NextLevelNumber => MaxLevelCount > 0 ? Mathf.Clamp(m_CurrentLevelIndex.Value + 2, 1, MaxLevelCount) : 0;
    public int MaxLevelCount => levels != null ? levels.Length : 0;
    public float CurrentHealth => m_CurrentHealth.Value;
    public float MaxHealth => TryGetLevel(m_CurrentLevelIndex.Value, out TrainUpgradeLevel level) ? level.maxHealth : 0f;
    public bool HasReachedMaxLevel => MaxLevelCount == 0 || m_CurrentLevelIndex.Value >= MaxLevelCount - 1;
    public bool IsTrainStopped => trainMovement == null || trainMovement.IsTrainStopped;
    public bool CanUpgradeNow => !HasReachedMaxLevel && IsTrainStopped;
    public float CurrentFuel => fuelSystem != null ? fuelSystem.CurrentFuel : 0f;
    public float MaxFuel => fuelSystem != null ? fuelSystem.MaxFuel : 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        m_CurrentLevelIndex.OnValueChanged += HandleLevelChanged;

        if (IsServer)
        {
            ApplyLevelToTrain(m_CurrentLevelIndex.Value, true);
        }
        else
        {
            ApplyVisualForLevel(m_CurrentLevelIndex.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        m_CurrentLevelIndex.OnValueChanged -= HandleLevelChanged;
        base.OnNetworkDespawn();
    }

    public int GetRequiredScrapForNextLevel()
    {
        if (HasReachedMaxLevel)
        {
            return 0;
        }

        return TryGetLevel(m_CurrentLevelIndex.Value + 1, out TrainUpgradeLevel nextLevel)
            ? Mathf.Max(0, nextLevel.scrapRequired)
            : 0;
    }

    public bool TryGetCurrentLevelPreview(out TrainUpgradeLevelPreview preview)
    {
        return TryGetLevelPreview(m_CurrentLevelIndex.Value, out preview);
    }

    public bool TryGetNextLevelPreview(out TrainUpgradeLevelPreview preview)
    {
        if (HasReachedMaxLevel)
        {
            preview = default(TrainUpgradeLevelPreview);
            return false;
        }

        return TryGetLevelPreview(m_CurrentLevelIndex.Value + 1, out preview);
    }

    public bool TryBuildPromptTrainData(int ownedScrap, out InteractionPromptTrainData trainData)
    {
        trainData = default(InteractionPromptTrainData);

        if (!TryGetCurrentLevelPreview(out TrainUpgradeLevelPreview currentLevel))
        {
            return false;
        }

        TrainUpgradeLevelPreview nextLevel = currentLevel;
        bool hasNextLevel = TryGetNextLevelPreview(out nextLevel);

        float maxHealthScale = GetHighestLevelValue(TrainStatType.MaxHealth);
        float maxSpeedScale = GetHighestLevelValue(TrainStatType.MaxSpeed);
        float bestFuelScale = GetLowestLevelValue(TrainStatType.FuelConsumptionRate);
        float worstFuelScale = GetHighestLevelValue(TrainStatType.FuelConsumptionRate);
        float maxBrakeScale = GetHighestLevelValue(TrainStatType.BrakeDeceleration);

        InteractionPromptStatCompareRow[] compareRows = new[]
        {
            BuildCompareRow("HP", currentLevel.MaxHealth, hasNextLevel ? nextLevel.MaxHealth : currentLevel.MaxHealth, maxHealthScale, 0f, false, "0"),
            BuildCompareRow("SPD", currentLevel.MaxSpeed, hasNextLevel ? nextLevel.MaxSpeed : currentLevel.MaxSpeed, maxSpeedScale, 0f, false, "0.#"),
            BuildCompareRow("FUEL", currentLevel.FuelConsumptionRate, hasNextLevel ? nextLevel.FuelConsumptionRate : currentLevel.FuelConsumptionRate, bestFuelScale, worstFuelScale, true, "0.#"),
            BuildCompareRow("BRK", currentLevel.BrakeDeceleration, hasNextLevel ? nextLevel.BrakeDeceleration : currentLevel.BrakeDeceleration, maxBrakeScale, 0f, false, "0.#")
        };

        InteractionPromptRequirementData[] requirements = hasNextLevel && scrapMetalItem != null
            ? new[]
            {
                new InteractionPromptRequirementData(
                    scrapMetalItem.itemPicture,
                    $"{ownedScrap}/{Mathf.Max(0, nextLevel.ScrapRequired)}",
                    ownedScrap >= nextLevel.ScrapRequired ? new Color(0.45f, 1f, 0.45f, 1f) : new Color(1f, 0.45f, 0.45f, 1f))
            }
            : new InteractionPromptRequirementData[0];

        trainData = new InteractionPromptTrainData(
            $"Lv.{CurrentLevelNumber}",
            hasNextLevel ? $"Lv.{NextLevelNumber}" : "MAX",
            MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth,
            $"{CurrentHealth:0}/{MaxHealth:0}",
            MaxFuel <= 0f ? 0f : CurrentFuel / MaxFuel,
            $"{CurrentFuel:0}/{MaxFuel:0}",
            compareRows,
            requirements);

        return true;
    }

    public void RestoreFullHealth()
    {
        if (!IsServer) return;

        m_CurrentHealth.Value = MaxHealth;
    }

    public void ApplyDamage(float damageAmount)
    {
        if (!IsServer || damageAmount <= 0f) return;

        m_CurrentHealth.Value = Mathf.Max(0f, m_CurrentHealth.Value - damageAmount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUpgradeServerRpc(int clientReportedScrap, ServerRpcParams rpcParams = default)
    {
        if (!CanUpgradeNow)
        {
            return;
        }

        int requiredScrap = GetRequiredScrapForNextLevel();

        // Server validates: client must report having enough scrap
        if (requiredScrap > 0 && clientReportedScrap < requiredScrap)
        {
            Debug.LogWarning($"[TrainUpgrade] Player {rpcParams.Receive.SenderClientId} tried to upgrade but reported {clientReportedScrap}/{requiredScrap} scrap. Rejected.");
            return;
        }

        m_CurrentLevelIndex.Value = Mathf.Min(m_CurrentLevelIndex.Value + 1, MaxLevelCount - 1);
        ApplyLevelToTrain(m_CurrentLevelIndex.Value, true);

        if (requiredScrap > 0 && scrapMetalItem != null)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
                }
            };

            ConsumeUpgradeScrapClientRpc(requiredScrap, clientRpcParams);
        }
    }

    [ClientRpc]
    private void ConsumeUpgradeScrapClientRpc(int scrapAmount, ClientRpcParams clientRpcParams = default)
    {
        if (scrapMetalItem == null || InventoryManager.instance == null || scrapAmount <= 0)
        {
            return;
        }

        InventoryManager.instance.ConsumeItemAmount(scrapMetalItem, scrapAmount);
    }

    private void HandleLevelChanged(int previousValue, int newValue)
    {
        ApplyLevelToTrain(newValue, false);
    }

    private void ApplyLevelToTrain(int levelIndex, bool restoreHealth)
    {
        if (!TryGetLevel(levelIndex, out TrainUpgradeLevel level))
        {
            return;
        }

        if (IsServer)
        {
            if (trainMovement != null)
            {
                trainMovement.ConfigureEndlessTrainStats(level.maxSpeed, level.brakeDeceleration);
            }

            if (fuelSystem != null)
            {
                fuelSystem.ConfigureFuelConsumption(level.fuelConsumptionRate);
            }

            if (restoreHealth)
            {
                m_CurrentHealth.Value = level.maxHealth;
            }
            else
            {
                m_CurrentHealth.Value = Mathf.Clamp(m_CurrentHealth.Value, 0f, level.maxHealth);
            }
        }

        ApplyVisualForLevel(levelIndex);
    }

    private void ApplyVisualForLevel(int levelIndex)
    {
        if (!TryGetLevel(levelIndex, out TrainUpgradeLevel level))
        {
            return;
        }

        if (visualAnchor == null)
        {
            if (defaultVisualRoot != null)
            {
                defaultVisualRoot.SetActive(true);
            }

            return;
        }

        if (m_CurrentVisualInstance != null)
        {
            Destroy(m_CurrentVisualInstance);
            m_CurrentVisualInstance = null;
        }

        if (defaultVisualRoot != null)
        {
            defaultVisualRoot.SetActive(level.visualPrefab == null);
        }

        if (level.visualPrefab == null)
        {
            return;
        }

        m_CurrentVisualInstance = Instantiate(level.visualPrefab, visualAnchor);
        m_CurrentVisualInstance.transform.localPosition = Vector3.zero;
        m_CurrentVisualInstance.transform.localRotation = Quaternion.identity;
        m_CurrentVisualInstance.transform.localScale = Vector3.one;
    }

    private bool TryGetLevel(int levelIndex, out TrainUpgradeLevel level)
    {
        if (levels != null && levelIndex >= 0 && levelIndex < levels.Length)
        {
            level = levels[levelIndex];
            return true;
        }

        level = default(TrainUpgradeLevel);
        return false;
    }

    private bool TryGetLevelPreview(int levelIndex, out TrainUpgradeLevelPreview preview)
    {
        if (TryGetLevel(levelIndex, out TrainUpgradeLevel level))
        {
            preview = new TrainUpgradeLevelPreview(
                level.levelName,
                level.maxHealth,
                level.maxSpeed,
                level.fuelConsumptionRate,
                level.brakeDeceleration,
                level.scrapRequired);
            return true;
        }

        preview = default(TrainUpgradeLevelPreview);
        return false;
    }

    private float GetHighestLevelValue(TrainStatType statType)
    {
        if (levels == null || levels.Length == 0)
        {
            return 0f;
        }

        float highestValue = float.MinValue;
        for (int index = 0; index < levels.Length; index++)
        {
            highestValue = Mathf.Max(highestValue, GetLevelStatValue(levels[index], statType));
        }

        return highestValue == float.MinValue ? 0f : highestValue;
    }

    private float GetLowestLevelValue(TrainStatType statType)
    {
        if (levels == null || levels.Length == 0)
        {
            return 0f;
        }

        float lowestValue = float.MaxValue;
        for (int index = 0; index < levels.Length; index++)
        {
            lowestValue = Mathf.Min(lowestValue, GetLevelStatValue(levels[index], statType));
        }

        return lowestValue == float.MaxValue ? 0f : lowestValue;
    }

    private static float GetLevelStatValue(TrainUpgradeLevel level, TrainStatType statType)
    {
        switch (statType)
        {
            case TrainStatType.MaxHealth:
                return level.maxHealth;
            case TrainStatType.MaxSpeed:
                return level.maxSpeed;
            case TrainStatType.FuelConsumptionRate:
                return level.fuelConsumptionRate;
            case TrainStatType.BrakeDeceleration:
                return level.brakeDeceleration;
            default:
                return 0f;
        }
    }

    private static InteractionPromptStatCompareRow BuildCompareRow(
        string label,
        float currentValue,
        float nextValue,
        float bestValue,
        float worstValue,
        bool lowerIsBetter,
        string valueFormat)
    {
        float currentNormalized = NormalizeCompareValue(currentValue, bestValue, worstValue, lowerIsBetter);
        float nextNormalized = NormalizeCompareValue(nextValue, bestValue, worstValue, lowerIsBetter);
        float deltaValue = nextValue - currentValue;
        bool improved = lowerIsBetter ? nextValue < currentValue : nextValue > currentValue;
        bool worsened = lowerIsBetter ? nextValue > currentValue : nextValue < currentValue;
        Color deltaColor = improved
            ? new Color(0.45f, 1f, 0.45f, 1f)
            : worsened
                ? new Color(1f, 0.45f, 0.45f, 1f)
                : Color.white;
        string nextValueText = nextValue.ToString(valueFormat);

        if (Mathf.Abs(deltaValue) > 0.001f)
        {
            string deltaSign = deltaValue > 0f ? "+" : "-";
            nextValueText += " " + deltaSign + Mathf.Abs(deltaValue).ToString(valueFormat);
        }

        return new InteractionPromptStatCompareRow(
            label,
            currentValue.ToString(valueFormat),
            nextValueText,
            currentNormalized,
            nextNormalized,
            deltaColor);
    }

    private static float NormalizeCompareValue(float value, float bestValue, float worstValue, bool lowerIsBetter)
    {
        if (lowerIsBetter)
        {
            if (Mathf.Approximately(bestValue, worstValue))
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.InverseLerp(bestValue, worstValue, value));
        }

        float safeBestValue = Mathf.Max(0.01f, bestValue);
        return Mathf.Clamp01(value / safeBestValue);
    }

    private enum TrainStatType
    {
        MaxHealth,
        MaxSpeed,
        FuelConsumptionRate,
        BrakeDeceleration
    }
}
