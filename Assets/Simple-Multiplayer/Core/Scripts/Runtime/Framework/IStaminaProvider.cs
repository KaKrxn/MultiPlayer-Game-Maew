namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Interface for components that can provide overrides for movement-related stamina/energy costs.
    /// This allows game-specific survival systems to influence framework-level energy consumption.
    /// </summary>
    public interface IStaminaProvider
    {
        /// <summary>
        /// Gets the override cost for jumping. Return -1 to use the default ability cost.
        /// </summary>
        float JumpEnergyCost { get; }

        /// <summary>
        /// Gets the override cost for sprinting per second. Return -1 to use the default ability cost.
        /// </summary>
        float SprintEnergyCost { get; }
    }
}
