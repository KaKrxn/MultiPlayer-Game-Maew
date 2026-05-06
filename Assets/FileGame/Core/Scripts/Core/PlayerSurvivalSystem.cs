using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace FileGame.Core
{
    /// <summary>
    /// Handles advanced survival logic, converting damage to Pain,
    /// managing Hunger over time, and enforcing Vitality (Death) logic based on total Debuffs.
    /// </summary>
    [RequireComponent(typeof(CoreStatsHandler))]
    public class PlayerSurvivalSystem : NetworkBehaviour, IStaminaProvider
    {
        public float JumpEnergyCost => jumpEnergyCost;
        public float SprintEnergyCost => sprintEnergyCost;

        [Header("Pain Logic")]
        [Tooltip("Percentage of health loss that is converted to permanent Pain.")]
        [Range(0f, 1f)]
        public float damageToPainRatio = 1.0f;
        
        [Header("Hunger Logic")]
        [Tooltip("Interval in seconds for hunger to increase.")]
        public float hungerInterval = 120f;
        [Tooltip("Amount of hunger to add every interval (percentage).")]
        public float hungerAmountPerTick = 10f;

        [Header("Energy Consumption (Balance)")]
        [Tooltip("Amount of Energy consumed per jump. If -1, uses default Ability cost.")]
        public float jumpEnergyCost = -1f;
        [Tooltip("Amount of Energy consumed per second while sprinting. If -1, uses default Ability cost.")]
        public float sprintEnergyCost = -1f;

        private CoreStatsHandler stats;
        private CoreMovement movement;
        private int healthHash;

        private float hungerTimer;
        private float toxicTimer;

        private void Awake()
        {
            stats = GetComponent<CoreStatsHandler>();
            movement = GetComponent<CoreMovement>();
            healthHash = StatKeys.Health;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            
            stats.OnStatChanged += HandleStatChanged;
            hungerTimer = 0f;

            // Reset survival stats on spawn to prevent stale value carryover
            InitializeSurvivalStats();
        }

        private void InitializeSurvivalStats()
        {
            if (!IsServer) return;

            // Ensure we start with full Vitality on a fresh spawn
            float currentVitality = stats.GetCurrentValue(SurvivalStatKeys.Vitality);
            if (currentVitality <= 0)
            {
                stats.ModifyStat(SurvivalStatKeys.Vitality, GameConstants.MaxVitality, OwnerClientId, ModificationSource.Natural);
            }

            // Reset all debuffs to 0
            stats.ModifyStat(SurvivalStatKeys.Pain, -stats.GetCurrentValue(SurvivalStatKeys.Pain), OwnerClientId, ModificationSource.Natural);
            stats.ModifyStat(SurvivalStatKeys.Hunger, -stats.GetCurrentValue(SurvivalStatKeys.Hunger), OwnerClientId, ModificationSource.Natural);
            stats.ModifyStat(SurvivalStatKeys.Weight, -stats.GetCurrentValue(SurvivalStatKeys.Weight), OwnerClientId, ModificationSource.Natural);
            stats.ModifyStat(SurvivalStatKeys.Toxic, -stats.GetCurrentValue(SurvivalStatKeys.Toxic), OwnerClientId, ModificationSource.Natural);
            
            Debug.Log($"[Survival] Spawn Initialized: Vitality={stats.GetCurrentValue(SurvivalStatKeys.Vitality)}");
        }

        public void ApplyCarriedWeightDelta(float kgDelta)
        {
            if (!IsServer || stats == null) return;

            float currentWeight = stats.GetCurrentValue(SurvivalStatKeys.Weight);
            float targetWeight = Mathf.Max(0f, currentWeight + (kgDelta * GameConstants.WeightToDebuffMultiplier));
            float appliedDelta = targetWeight - currentWeight;

            if (Mathf.Abs(appliedDelta) > 0.001f)
            {
                stats.ModifyStat(SurvivalStatKeys.Weight, appliedDelta, OwnerClientId, ModificationSource.Natural);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && stats != null)
            {
                stats.OnStatChanged -= HandleStatChanged;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            // Hunger ticks: base rate modified by exertion
            float hungerMultiplier = 1.0f;
            if (movement != null)
            {
                bool isRunning = movement.IsSprinting && movement.CurrentSpeed > 0.1f && movement.IsGrounded;
                bool isJumping = !movement.IsGrounded;
                
                if (isRunning || isJumping)
                {
                    hungerMultiplier = GameConstants.HungerExertionMultiplier;
                }
            }

            hungerTimer += Time.deltaTime * hungerMultiplier;
            if (hungerTimer >= hungerInterval)
            {
                hungerTimer -= hungerInterval;
                stats.ModifyStat(SurvivalStatKeys.Hunger, hungerAmountPerTick, OwnerClientId, ModificationSource.Natural);
                Debug.Log($"[Survival] Hunger Ticked: +{hungerAmountPerTick}% (Total: {stats.GetCurrentValue(SurvivalStatKeys.Hunger)}%)");
            }

            // Read current debuff values
            float pain   = stats.GetCurrentValue(SurvivalStatKeys.Pain);
            float hunger = stats.GetCurrentValue(SurvivalStatKeys.Hunger);
            float weight = stats.GetCurrentValue(SurvivalStatKeys.Weight);
            float toxic  = stats.GetCurrentValue(SurvivalStatKeys.Toxic);

            // Toxic ticks: debuff grows over time while toxic > 0
            if (toxic > 0f)
            {
                toxicTimer += Time.deltaTime;
                if (toxicTimer >= GameConstants.ToxicTickInterval)
                {
                    toxicTimer = 0f;
                    float tickAmount = Random.Range(GameConstants.ToxicTickMin, GameConstants.ToxicTickMax);
                    stats.ModifyStat(SurvivalStatKeys.Toxic, tickAmount, OwnerClientId, ModificationSource.Natural);
                }
            }

            // Calculate target vitality from total debuffs
            float totalDebuffs = pain + hunger + weight + toxic;
            float targetVitality = GameConstants.MaxVitality - totalDebuffs;

            // Weight alone should not kill the player
            if (targetVitality < GameConstants.VitalityDeadZone && (pain + hunger + toxic) < GameConstants.MaxVitality)
            {
                targetVitality = GameConstants.VitalityDeadZone;
            }
            else
            {
                targetVitality = Mathf.Max(0f, targetVitality);
            }

            // Enforce Vitality stat
            float currentVitality = stats.GetCurrentValue(SurvivalStatKeys.Vitality);
            float vitalityDiff = targetVitality - currentVitality;
            if (Mathf.Abs(vitalityDiff) > GameConstants.VitalityMargin)
            {
                stats.ModifyStat(SurvivalStatKeys.Vitality, vitalityDiff, OwnerClientId, ModificationSource.Natural);
            }

            // Cap Health to current vitality
            float currentHealth = stats.GetCurrentValue(healthHash);
            if (currentHealth > targetVitality + GameConstants.VitalityMargin)
            {
                float overage = targetVitality - currentHealth;
                stats.ModifyStat(healthHash, overage, OwnerClientId, ModificationSource.Natural);
            }
        }

        private void HandleStatChanged(StatChangePayload payload)
        {
            // Only intercept external damage to Health (not internal capping or sprint consumption)
            bool isExternalDamage = payload.sourceType == ModificationSource.Direct || 
                                    payload.sourceType == ModificationSource.Damage || 
                                    payload.sourceType == ModificationSource.Environmental;

            if (payload.statID == healthHash && payload.changeAmount < 0 && isExternalDamage)
            {
                // Revert the health drain and convert it to Pain
                stats.ModifyStat(healthHash, Mathf.Abs(payload.changeAmount), OwnerClientId, ModificationSource.Healing);
                
                float painIncrease = Mathf.Abs(payload.changeAmount) * damageToPainRatio;
                stats.ModifyStat(SurvivalStatKeys.Pain, painIncrease, OwnerClientId, ModificationSource.Injury);
            }
        }
    }
}
