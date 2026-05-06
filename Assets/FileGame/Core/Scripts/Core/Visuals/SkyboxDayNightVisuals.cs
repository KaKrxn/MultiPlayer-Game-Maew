using UnityEngine;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Component responsible for changing the Skybox based on the Day/Night state.
    /// Listens to DayNightCycleManager independent of lighting configs to keep concerns separated.
    /// </summary>
    public class SkyboxDayNightVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DayNightCycleManager dayNightManager;

        [Header("Skybox Settings")]
        [SerializeField] private Material daySkybox;
        [SerializeField] private Material nightSkybox;

        private void Start()
        {
            if (dayNightManager == null)
            {
                dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            }

            if (dayNightManager != null)
            {
                dayNightManager.OnStateChanged += HandleStateChanged;
                // Initialize to current state
                ApplyState(dayNightManager.CurrentState);
            }
            else
            {
                Debug.LogWarning("[SkyboxDayNightVisuals] DayNightCycleManager not found! Skybox won't change dynamically.");
            }
        }

        private void OnDestroy()
        {
            if (dayNightManager != null)
            {
                dayNightManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(DayNightState newState)
        {
            ApplyState(newState);
        }

        private void ApplyState(DayNightState state)
        {
            bool isNight = state == DayNightState.Night;
            
            Material targetSkybox = isNight ? nightSkybox : daySkybox;

            if (targetSkybox != null && RenderSettings.skybox != targetSkybox)
            {
                RenderSettings.skybox = targetSkybox;
                DynamicGI.UpdateEnvironment(); // Update Environment lighting so refelctions also match the skybox!
            }
        }
    }
}
