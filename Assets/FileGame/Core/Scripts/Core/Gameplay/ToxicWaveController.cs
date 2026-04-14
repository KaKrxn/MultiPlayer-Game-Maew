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
        [SerializeField] private bool isActive = false;

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTick = 10f;
        [SerializeField] private float tickRate = 1f;

        [Header("References")]
        [SerializeField] private DayNightCycleManager dayNightManager;

        private NetworkVariable<float> m_CurrentZPosition = new NetworkVariable<float>(
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
        private float m_StartPointZ;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Find DayNightCycleManager if not assigned
                if (dayNightManager == null)
                {
                    dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
                }

                m_StartPointZ = transform.position.z;
                m_CurrentZPosition.Value = m_StartPointZ;
                m_IsWaveActive.Value = isActive;
                
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
            float nextZ = m_CurrentZPosition.Value + m_TargetSpeed * Time.deltaTime;
            m_CurrentZPosition.Value = nextZ;
            transform.position = new Vector3(transform.position.x, transform.position.y, nextZ);
        }

        private void ClientUpdate()
        {
            // Simple visual interpolation for clients
            float lerpSpeed = m_TargetSpeed > 0 ? m_TargetSpeed * 1.5f : 5f;
            Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, m_CurrentZPosition.Value);
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
                    float playerZ = client.PlayerObject.transform.position.z;
                    float dist = playerZ - m_StartPointZ;
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
                Debug.Log("[ToxicWave] Wave Activated!");
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
                // Correct way to apply damage in Blocks framework: 
                // ModifyStat(hash, negative amount, sourcePlayerId, sourceType)
                // Source ID 0 is often used for environmental/system damage
                player.CoreStats.ModifyStat(StatKeys.Health, -damagePerTick, 0, ModificationSource.Direct);
                Debug.Log($"[ToxicWave] Applied {damagePerTick} damage to {player.PlayerName}");
            }
        }
    }
}
