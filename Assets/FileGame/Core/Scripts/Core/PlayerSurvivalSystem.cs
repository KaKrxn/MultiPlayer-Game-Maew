using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
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
        
        private struct PendingToxicity { public float delay; public float amount; }
        private List<PendingToxicity> _pendingDelays = new List<PendingToxicity>();

        private struct ToxicRoutine { public int ticksRemaining; public float timer; }
        private List<ToxicRoutine> _activeRoutines = new List<ToxicRoutine>();

        private struct AntidoteRoutine { public float remainingHealingAmount; public float tickAmount; public float remainingLifetime; public float tickTimer; }
        private List<AntidoteRoutine> _antidoteRoutines = new List<AntidoteRoutine>();

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
            
            _pendingDelays.Clear();
            _activeRoutines.Clear();
            _antidoteRoutines.Clear();

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

            // Toxic ticks: multiple routines grow over time
            for (int i = _activeRoutines.Count - 1; i >= 0; i--)
            {
                var routine = _activeRoutines[i];
                routine.timer += Time.deltaTime;

                if (routine.timer >= GameConstants.ToxicTickInterval)
                {
                    routine.timer = 0f;
                    routine.ticksRemaining--;
                    
                    float tickAmount = Random.Range(GameConstants.ToxicTickMin, GameConstants.ToxicTickMax);
                    stats.ModifyStat(SurvivalStatKeys.Toxic, tickAmount, OwnerClientId, ModificationSource.Natural);

                    if (routine.ticksRemaining <= 0)
                    {
                        _activeRoutines.RemoveAt(i);
                        Debug.Log("[Survival] An individual toxic routine has finished.");
                    }
                    else
                    {
                        _activeRoutines[i] = routine;
                    }
                }
                else
                {
                    _activeRoutines[i] = routine;
                }
            }

            // Handle delayed toxicity (Stomach ache)
            for (int i = _pendingDelays.Count - 1; i >= 0; i--)
            {
                var pending = _pendingDelays[i];
                pending.delay -= Time.deltaTime;

                if (pending.delay <= 0f)
                {
                    stats.ModifyStat(SurvivalStatKeys.Toxic, pending.amount, OwnerClientId, ModificationSource.Environmental);
                    Debug.Log($"[Survival] Delayed toxicity triggered (Stomach ache): +{pending.amount} Toxic.");
                    _pendingDelays.RemoveAt(i);
                }
                else
                {
                    _pendingDelays[i] = pending;
                }
            }

            // Handle Antidote effect
            float toxicVal = stats.GetCurrentValue(SurvivalStatKeys.Toxic);
            for (int i = _antidoteRoutines.Count - 1; i >= 0; i--)
            {
                var routine = _antidoteRoutines[i];
                routine.remainingLifetime -= Time.deltaTime;

                if (routine.remainingLifetime <= 0f)
                {
                    _antidoteRoutines.RemoveAt(i);
                    Debug.Log("[Survival] Antidote routine expired naturally (2 minutes limit).");
                    continue;
                }

                if (toxicVal > 0f)
                {
                    routine.tickTimer += Time.deltaTime;
                    if (routine.tickTimer >= GameConstants.AntidoteTickInterval)
                    {
                        routine.tickTimer = 0f;
                        
                        // Limit healing so it doesn't overshoot existing toxicity or remaining healing power
                        float heal = Mathf.Min(routine.tickAmount, routine.remainingHealingAmount);
                        heal = Mathf.Min(heal, toxicVal); // do not heal if no toxicity
                        
                        if (heal > 0f)
                        {
                            stats.ModifyStat(SurvivalStatKeys.Toxic, -heal, OwnerClientId, ModificationSource.Healing);
                            toxicVal -= heal;
                            routine.remainingHealingAmount -= heal;
                            
                            Debug.Log($"[Survival] Antidote tick! Restored {-heal} Toxic (Remaining Potential: {routine.remainingHealingAmount}).");
                        }
                        
                        if (routine.remainingHealingAmount <= 0.001f)
                        {
                            _antidoteRoutines.RemoveAt(i);
                            Debug.Log("[Survival] Antidote fully consumed.");
                            continue;
                        }
                    }
                }
                
                _antidoteRoutines[i] = routine;
            }

            // Calculate target vitality from total debuffs
            float pain   = stats.GetCurrentValue(SurvivalStatKeys.Pain);
            float hunger = stats.GetCurrentValue(SurvivalStatKeys.Hunger);
            float weight = stats.GetCurrentValue(SurvivalStatKeys.Weight);
            float toxic  = stats.GetCurrentValue(SurvivalStatKeys.Toxic);

            float totalDebuffs = pain + hunger + weight + toxic;
            float targetVitality = GameConstants.MaxVitality - totalDebuffs;

            // Weight now triggers death (Vitality can reach 0)
            targetVitality = Mathf.Max(0f, targetVitality);

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

            // Toxic reset: if toxicity increases from external/environment, reset tick growth
            // Toxic session: if toxicity increases from external/environment, start a NEW individual tick routine
            bool isToxicIncrease = payload.statID == SurvivalStatKeys.Toxic && payload.changeAmount > 0;
            if (isToxicIncrease && isExternalDamage)
            {
                var newRoutine = new ToxicRoutine
                {
                    ticksRemaining = Random.Range(GameConstants.ToxicMaxTicksMin, GameConstants.ToxicMaxTicksMax + 1),
                    timer = 0f
                };
                _activeRoutines.Add(newRoutine);
                Debug.Log($"[Survival] New individual toxic routine started! {newRoutine.ticksRemaining} ticks.");
            }
        }

        public void AddDelayedToxicity(float amount)
        {
            if (!IsServer) return;
            
            var newPending = new PendingToxicity
            {
                amount = amount,
                delay = Random.Range(GameConstants.ToxicDelayMin, GameConstants.ToxicDelayMax)
            };
            
            _pendingDelays.Add(newPending);
            Debug.Log($"[Survival] Toxicity session delayed by {newPending.delay} seconds.");
        }

        public void AddAntidoteRoutine(float durability)
        {
            if (!IsServer) return;
            
            var newRoutine = new AntidoteRoutine
            {
                remainingHealingAmount = durability,
                tickAmount = durability / GameConstants.AntidoteTickDivisor,
                remainingLifetime = GameConstants.AntidoteMaxLifetime,
                tickTimer = 0f
            };
            
            _antidoteRoutines.Add(newRoutine);
            Debug.Log($"[Survival] Antidote Consumed. Will heal {durability} Toxic over time (Cached for {GameConstants.AntidoteMaxLifetime}s).");
        }
    }
}
