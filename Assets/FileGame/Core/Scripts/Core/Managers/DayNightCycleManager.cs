using System;
using UnityEngine;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    public class DayNightCycleManager : NetworkBehaviour, IDayNightCycleManager
    {
        [Header("Settings")]
        [SerializeField] private float dayDuration = 60f;
        [SerializeField] private float nightDuration = 30f;
        [SerializeField] private DayNightState initialState = DayNightState.Day;

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
