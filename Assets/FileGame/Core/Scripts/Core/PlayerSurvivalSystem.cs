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
    public class PlayerSurvivalSystem : NetworkBehaviour
    {
        [Header("Pain Logic")]
        [Tooltip("Percentage of health loss that is converted to permanent Pain.")]
        [Range(0f, 1f)]
        public float damageToPainRatio = 1.0f;
        
        [Header("Hunger Logic")]
        [Tooltip("Interval in seconds for hunger to increase.")]
        public float hungerInterval = 120f;
        [Tooltip("Amount of hunger to add every interval (percentage).")]
        public float hungerAmountPerTick = 10f;

        private CoreStatsHandler stats;
        private int healthHash;
        private int painHash;
        private int hungerHash;
        private int vitalityHash;
        private int weightHash;

        private float hungerTimer;

        private void Awake()
        {
            stats = GetComponent<CoreStatsHandler>();
            healthHash = StatKeys.Health;
            painHash = Animator.StringToHash("Pain");
            hungerHash = Animator.StringToHash("Hunger");
            vitalityHash = Animator.StringToHash("Vitality");
            weightHash = Animator.StringToHash("Weight");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return; // Logic only runs on server
            
            stats.OnStatChanged += HandleStatChanged;
            hungerTimer = 0f;

            // Optional: Hard reset survival stats on spawn to prevent "old value" carryover bugs
            // Only if they aren't already at their starting defaults
            InitializeSurvivalStats();
        }

        private void InitializeSurvivalStats()
        {
            if (!IsServer) return;

            // Ensure we start with 100 Vitality and 0 Debuffs if this is a fresh spawn
            // This prevents the "instant 100% debuff" bug if assets were misconfigured
            float currentVitality = stats.GetCurrentValue(vitalityHash);
            if (currentVitality <= 0)
            {
                stats.ModifyStat(vitalityHash, 100f, OwnerClientId, ModificationSource.Natural);
            }

            // Reset debuffs to 0 on a fresh spawn
            stats.ModifyStat(painHash, -stats.GetCurrentValue(painHash), OwnerClientId, ModificationSource.Natural);
            stats.ModifyStat(hungerHash, -stats.GetCurrentValue(hungerHash), OwnerClientId, ModificationSource.Natural);
            stats.ModifyStat(weightHash, -stats.GetCurrentValue(weightHash), OwnerClientId, ModificationSource.Natural);
            
            Debug.Log($"[Survival] Swpawn Initialized: Vitality={stats.GetCurrentValue(vitalityHash)}");
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

            // Handle discrete Hunger ticks (+10% every 2 mins by default)
            hungerTimer += Time.deltaTime;
            if (hungerTimer >= hungerInterval)
            {
                hungerTimer -= hungerInterval;
                stats.ModifyStat(hungerHash, hungerAmountPerTick, OwnerClientId, ModificationSource.Natural);
                Debug.Log($"[Survival] Hunger Ticked: +10% (Total: {stats.GetCurrentValue(hungerHash)}%)");
            }

            // Calculate current survival capacity (Vitality)
            float pain = stats.GetCurrentValue(painHash);
            float hunger = stats.GetCurrentValue(hungerHash);
            float weight = stats.GetCurrentValue(weightHash);

            float totalDebuffs = pain + hunger + weight;
            float targetVitality = Mathf.Max(0f, 100f - totalDebuffs);

            // 1. Enforce Vitality stat (Life)
            float currentVitality = stats.GetCurrentValue(vitalityHash);
            float vitalityDiff = targetVitality - currentVitality;
            if (Mathf.Abs(vitalityDiff) > 0.05f) // Small margin
            {
                stats.ModifyStat(vitalityHash, vitalityDiff, OwnerClientId, ModificationSource.Natural);
            }

            // 2. Enforce Energy (Health) cap based on current capacity
            float currentHealth = stats.GetCurrentValue(healthHash);
            if (currentHealth > targetVitality + 0.05f)
            {
                float overage = targetVitality - currentHealth;
                stats.ModifyStat(healthHash, overage, OwnerClientId, ModificationSource.Natural);
            }
        }

        private void HandleStatChanged(StatChangePayload payload)
        {
            // IMPORTANT: We only intercept actual EXTERNAL damage to the Energy Bar (Health)
            // If the source is 'Natural' (our own capping logic) or 'Consumption' (sprinting), we ignore it!
            bool isExternalDamage = payload.sourceType == ModificationSource.Direct || 
                                    payload.sourceType == ModificationSource.Damage || 
                                    payload.sourceType == ModificationSource.Environmental;

            if (payload.statID == healthHash && payload.changeAmount < 0 && isExternalDamage)
            {
                // 1. Revert the health drain (keep the green bar full)
                stats.ModifyStat(healthHash, Mathf.Abs(payload.changeAmount), OwnerClientId, ModificationSource.Healing);
                
                // 2. Add it to Pain instead
                float painIncrease = Mathf.Abs(payload.changeAmount) * damageToPainRatio;
                stats.ModifyStat(painHash, painIncrease, OwnerClientId, ModificationSource.Injury);
            }
        }
    }
}
