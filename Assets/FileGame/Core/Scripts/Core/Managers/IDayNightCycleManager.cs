using System;

namespace Blocks.Gameplay.Core
{
    public enum DayNightState
    {
        Day,
        Night
    }

    public interface IDayNightCycleManager
    {
        DayNightState CurrentState { get; }
        event Action<DayNightState> OnStateChanged;
        event Action OnDayStarted;
        event Action OnNightStarted;
        
        float DayDuration { get; }
        float NightDuration { get; }
        float TimeRemainingInState { get; }
    }
}
