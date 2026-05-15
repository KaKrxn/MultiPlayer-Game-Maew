using UnityEngine;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    public class DayNightVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DayNightCycleManager dayNightManager;
        [SerializeField] private Light directionalLight;

        [Header("Day Settings")]
        [SerializeField] private Color dayAmbientColor = Color.white;
        [SerializeField] private Color dayDirectionalColor = Color.white;
        [SerializeField] private float dayDirectionalIntensity = 1f;

        [Header("Night Settings")]
        [SerializeField] private Color nightAmbientColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color nightDirectionalColor = new Color(0.2f, 0.2f, 0.4f);
        [SerializeField] private float nightDirectionalIntensity = 0.1f;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 2f;

        private void Start()
        {
            if (dayNightManager == null)
            {
                dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            }

            if (dayNightManager != null)
            {
                if (directionalLight == null)
                {
                    directionalLight = dayNightManager.MainDirectionalLight;
                }

                dayNightManager.OnStateChanged += HandleStateChanged;
                // Initialize to current state
                ApplyInitialState(dayNightManager.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (dayNightManager != null)
            {
                dayNightManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void ApplyInitialState(DayNightState state)
        {
            bool isNight = state == DayNightState.Night;
            RenderSettings.ambientLight = isNight ? nightAmbientColor : dayAmbientColor;
            
            if (directionalLight != null)
            {
                directionalLight.color = isNight ? nightDirectionalColor : dayDirectionalColor;
                directionalLight.intensity = isNight ? nightDirectionalIntensity : dayDirectionalIntensity;
            }
        }

        private void HandleStateChanged(DayNightState newState)
        {
            StopAllCoroutines();
            StartCoroutine(TransitionToState(newState));
        }

        private IEnumerator TransitionToState(DayNightState state)
        {
            bool isNight = state == DayNightState.Night;
            Color targetAmbient = isNight ? nightAmbientColor : dayAmbientColor;
            Color targetDirectionalColor = isNight ? nightDirectionalColor : dayDirectionalColor;
            float targetIntensity = isNight ? nightDirectionalIntensity : dayDirectionalIntensity;

            Color startAmbient = RenderSettings.ambientLight;
            Color startDirColor = directionalLight != null ? directionalLight.color : Color.white;
            float startIntensity = directionalLight != null ? directionalLight.intensity : 0f;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;

                RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);
                
                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(startDirColor, targetDirectionalColor, t);
                    directionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                }

                yield return null;
            }

            // Final snap
            RenderSettings.ambientLight = targetAmbient;
            if (directionalLight != null)
            {
                directionalLight.color = targetDirectionalColor;
                directionalLight.intensity = targetIntensity;
            }
        }
    }
}
