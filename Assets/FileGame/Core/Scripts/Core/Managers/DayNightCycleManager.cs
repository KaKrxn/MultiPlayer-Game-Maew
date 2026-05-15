using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    public class DayNightCycleManager : NetworkBehaviour, IDayNightCycleManager
    {
        [Header("Settings")]
        [SerializeField] private float dayDuration = 60f;
        [SerializeField] private float nightDuration = 30f;
        [SerializeField] private DayNightState initialState = DayNightState.Day;

        [Header("References")]
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Volume dayVolume;
        [SerializeField] private Volume nightVolume;

        [Header("Fog Settings")]
        [SerializeField] private bool useFog = true;
        [SerializeField] private Color dayFogColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float dayFogDensity = 0.01f;
        [SerializeField] private Color nightFogColor = new Color(0.05f, 0.05f, 0.1f);
        [SerializeField] private float nightFogDensity = 0.05f;

        private List<Light> m_OtherDirectionalLights = new List<Light>();

        private NetworkVariable<DayNightState> m_CurrentState = new NetworkVariable<DayNightState>(
            DayNightState.Day,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> m_StateTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public DayNightState CurrentState => m_CurrentState.Value;
        public float DayDuration => dayDuration;
        public float NightDuration => nightDuration;
        public float TimeRemainingInState => m_StateTimeRemaining.Value;
        public Light MainDirectionalLight => mainDirectionalLight;

        public event Action<DayNightState> OnStateChanged;
        public event Action OnDayStarted;
        public event Action OnNightStarted;

        public override void OnNetworkSpawn()
        {
            m_CurrentState.OnValueChanged += HandleStateChanged;
            
            if (IsServer)
            {
                m_CurrentState.Value = initialState;
                m_StateTimeRemaining.Value = initialState == DayNightState.Day ? dayDuration : nightDuration;
            }

            InitializeOtherLights();
            UpdateVolumes(m_CurrentState.Value);
            UpdateFog(m_CurrentState.Value);
        }

        private void InitializeOtherLights()
        {
            m_OtherDirectionalLights.Clear();
            // Using IncludeInactive to catch any lights that might be off by default
            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light light in allLights)
            {
                if (light.type == LightType.Directional && light != mainDirectionalLight)
                {
                    m_OtherDirectionalLights.Add(light);
                }
            }
            // Initial sync
            UpdateOtherLightsState(m_CurrentState.Value);
        }

        private void UpdateOtherLightsState(DayNightState state)
        {
            bool isActive = (state == DayNightState.Day);
            foreach (Light light in m_OtherDirectionalLights)
            {
                if (light != null)
                {
                    light.gameObject.SetActive(isActive);
                    Debug.Log($"[DayNightCycleManager] Setting light {light.name} active: {isActive}");
                }
            }
        }

        private void UpdateVolumes(DayNightState state)
        {
            if (dayVolume != null) dayVolume.weight = (state == DayNightState.Day) ? 1f : 0f;
            if (nightVolume != null) nightVolume.weight = (state == DayNightState.Night) ? 1f : 0f;
            Debug.Log($"[DayNightCycleManager] Updated Volumes for state: {state}");
        }

        private void UpdateFog(DayNightState state)
        {
            if (!useFog) return;

            RenderSettings.fog = true;
            RenderSettings.fogColor = (state == DayNightState.Day) ? dayFogColor : nightFogColor;
            RenderSettings.fogDensity = (state == DayNightState.Day) ? dayFogDensity : nightFogDensity;
            Debug.Log($"[DayNightCycleManager] Updated Fog for state: {state}");
        }

        public override void OnNetworkDespawn()
        {
            m_CurrentState.OnValueChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (!IsServer) return;

            m_StateTimeRemaining.Value -= Time.deltaTime;

            if (m_StateTimeRemaining.Value <= 0)
            {
                ToggleState();
            }
        }

        private void ToggleState()
        {
            if (m_CurrentState.Value == DayNightState.Day)
            {
                m_CurrentState.Value = DayNightState.Night;
                m_StateTimeRemaining.Value = nightDuration;
            }
            else
            {
                m_CurrentState.Value = DayNightState.Day;
                m_StateTimeRemaining.Value = dayDuration;
            }
        }

        private void HandleStateChanged(DayNightState previousState, DayNightState newState)
        {
            OnStateChanged?.Invoke(newState);
            UpdateOtherLightsState(newState);
            UpdateVolumes(newState);
            UpdateFog(newState);

            if (newState == DayNightState.Day)
            {
                OnDayStarted?.Invoke();
                Debug.Log("[DayNightCycleManager] Day Started");
            }
            else
            {
                OnNightStarted?.Invoke();
                Debug.Log("[DayNightCycleManager] Night Started");
            }
        }
    }
}
