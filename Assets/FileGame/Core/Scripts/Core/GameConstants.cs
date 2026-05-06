using UnityEngine;

namespace FileGame.Core
{
    /// <summary>
    /// Single source of truth for all game balance values and magic numbers.
    /// Eliminates scattered hardcoded values across multiple scripts.
    /// </summary>
    public static class GameConstants
    {
        // ── Survival: Hunger ──────────────────────────────────────
        public const float HungerTickInterval     = 120f;   // seconds between hunger ticks
        public const float HungerTickAmount        = 10f;    // % hunger added per tick
        public const float HungerExertionMultiplier = 1.25f; // multiplier when running/jumping

        // ── Survival: Toxic ───────────────────────────────────────
        public const float ToxicTickInterval = 5f;     // seconds between toxic ticks
        public const float ToxicTickMin      = 1f;     // min toxic added per tick
        public const float ToxicTickMax      = 3f;     // max toxic added per tick
        public const int ToxicMaxTicksMin    = 3;      // min ticks before growth stops
        public const int ToxicMaxTicksMax    = 7;      // max ticks before growth stops
        public const float ToxicDelayMin     = 60f;    // minimum delay before toxic kicks in
        public const float ToxicDelayMax     = 300f;   // maximum delay before toxic kicks in

        // ── Survival: Antidote ────────────────────────────────────
        public const float AntidoteTickInterval = 5f;    // seconds between antidote healing ticks
        public const float AntidoteMaxLifetime  = 120f;  // max lifetime before the antidote effect expires
        public const float AntidoteTickDivisor  = 10f;   // tick amount is durability / divisor

        // ── Food Consumption ──────────────────────────────────────
        public const float RottenFoodChance       = 1f;  // 15% chance food is rotten
        public const float RottenToxicMin          = 5f;     // min toxic from rotten food
        public const float RottenToxicMax          = 15f;    // max toxic from rotten food
        public const float HungerReductionDivisor  = 2f;     // durability / this = hunger reduced
        public const float DefaultHoldDuration     = 2.0f;   // seconds to hold for eating

        // ── Weight ────────────────────────────────────────────────
        public const float WeightToDebuffMultiplier = 10f;   // 1kg = 10% debuff

        // ── Vitality ──────────────────────────────────────────────
        public const float MaxVitality       = 100f;
        public const float VitalityMargin    = 0.05f;  // threshold for stat update

        // ── Train: Fuel ───────────────────────────────────────────
        public const float TrainDefaultMaxFuel         = 100f;
        public const float TrainDefaultFuelConsumption = 2f;
        public const float TrainFuelRefillBase         = 25f;  // base fuel per item drop

        // ── UI ────────────────────────────────────────────────────
        public const float SelectedSlotScale = 1.15f;

        // ── Player: Death & Revive ────────────────────────────────
        public const float DefaultReviveDistance = 3f;
    }

    /// <summary>
    /// Centralized stat hash keys for survival stats not covered by the
    /// auto-generated StatKeys (which only has Health, Stamina, Coin).
    /// </summary>
    public static class SurvivalStatKeys
    {
        public static readonly int Pain     = Animator.StringToHash("Pain");
        public static readonly int Hunger   = Animator.StringToHash("Hunger");
        public static readonly int Vitality = Animator.StringToHash("Vitality");
        public static readonly int Weight   = Animator.StringToHash("Weight");
        public static readonly int Toxic    = Animator.StringToHash("Toxic");
    }
}
