using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    public class ToxicWaveController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 2f;
        [SerializeField] private float nightSpeedMultiplier = 2.5f;
        [SerializeField] private float triggerDistance = 10f;
        [SerializeField] private Vector3 moveDirection = Vector3.forward;
        [SerializeField] private bool isActive = false;

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTick = 10f;
        [SerializeField] private float tickRate = 1f;

        [Header("References")]
        [SerializeField] private DayNightCycleManager dayNightManager;

        private NetworkVariable<float> m_CurrentDistance = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> m_IsWaveActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float m_LastTickTime;
        private float m_TargetSpeed;
        private Vector3 m_StartPosition;

        private int m_PainHash;

        public override void OnNetworkSpawn()
        {
            m_StartPosition = transform.position;
            m_PainHash = Animator.StringToHash("Pain");

            if (IsServer)
            {
                // Find DayNightCycleManager if not assigned
                if (dayNightManager == null)
                {
                    dayNightManager = FindObjectOfType<DayNightCycleManager>();
                }

                m_CurrentDistance.Value = 0f;
                m_IsWaveActive.Value = isActive;
                Debug.Log($"[ToxicWaveController] OnNetworkSpawn: StartPos={m_StartPosition}, IsActive={m_IsWaveActive.Value}");
                
                UpdateTargetSpeed(DayNightState.Day);

                if (dayNightManager != null)
                {
                    dayNightManager.OnStateChanged += HandleDayNightChange;
                    UpdateTargetSpeed(dayNightManager.CurrentState);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && dayNightManager != null)
            {
                dayNightManager.OnStateChanged -= HandleDayNightChange;
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                ServerUpdate();
            }
            else
            {
                ClientUpdate();
            }
        }

        private void ServerUpdate()
        {
            if (!m_IsWaveActive.Value)
            {
                CheckForActivation();
                return;
            }

            // Move the wave
            float nextDist = m_CurrentDistance.Value + m_TargetSpeed * Time.deltaTime;
            m_CurrentDistance.Value = nextDist;
            transform.position = m_StartPosition + moveDirection.normalized * nextDist;
        }

        private void ClientUpdate()
        {
            // Simple visual interpolation for clients
            float lerpSpeed = m_TargetSpeed > 0 ? m_TargetSpeed * 1.5f : 5f;
            Vector3 targetPos = m_StartPosition + moveDirection.normalized * m_CurrentDistance.Value;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
        }

        private void CheckForActivation()
        {
            float maxDistance = 0f;
            bool playerFound = false;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    Vector3 playerPos = client.PlayerObject.transform.position;
                    // Project player distance along the move direction
                    float dist = Vector3.Dot(playerPos - m_StartPosition, moveDirection.normalized);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        playerFound = true;
                    }
                }
            }

            if (playerFound && maxDistance >= triggerDistance)
            {
                m_IsWaveActive.Value = true;
                TriggerWarningClientRpc();
                Debug.Log($"[ToxicWave] Wave Activated! Max distance: {maxDistance}");
            }
        }

        [ClientRpc]
        public void TriggerWarningClientRpc()
        {
            Debug.Log("[ToxicWaveController] TriggerWarningClientRpc received!");
            if (ToxicWaveUI.Instance != null)
            {
                ToxicWaveUI.Instance.ShowWarning();
            }
            else
            {
                Debug.LogWarning("[ToxicWaveController] ToxicWaveUI.Instance is null!");
            }
        }

        private void HandleDayNightChange(DayNightState newState)
        {
            UpdateTargetSpeed(newState);
        }

        private void UpdateTargetSpeed(DayNightState state)
        {
            float multiplier = (state == DayNightState.Night) ? nightSpeedMultiplier : 1f;
            m_TargetSpeed = baseMoveSpeed * multiplier;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsServer) return;

            if (Time.time - m_LastTickTime < tickRate) return;

            // Check if it's a player
            if (other.TryGetComponent<CorePlayerManager>(out var player))
            {
                ApplyDamage(player);
                m_LastTickTime = Time.time;
            }
        }

        private void ApplyDamage(CorePlayerManager player)
        {
            if (player.CoreStats != null)
            {
                // Applied as gradual Pain (+5% per tick)
                player.CoreStats.ModifyStat(m_PainHash, 5f, 0, ModificationSource.Environmental);
                Debug.Log($"[ToxicWave] Applied 5% Pain damage to {player.PlayerName}");
            }
        }
    }
}
