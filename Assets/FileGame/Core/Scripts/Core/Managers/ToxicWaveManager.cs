using UnityEngine;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    public class ToxicWaveManager : NetworkBehaviour
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject toxicWavePrefab;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0, -10f); // Spawn slightly behind start

        [Header("Trigger Settings")]
        [SerializeField] private float triggerDistance = 500f; // Distance before wave starts moving (e.g. 10 biomes * 50m)

        private ToxicWaveController m_SpawnedWave;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            if (toxicWavePrefab == null)
            {
                Debug.LogError("[ToxicWaveManager] ToxicWave Prefab is missing!");
                return;
            }

            // Spawn the wave at the specified offset from the manager's position 
            // (Place this manager at the world's origin or player spawn point)
            GameObject waveObj = Instantiate(toxicWavePrefab, transform.position + spawnOffset, Quaternion.identity);
            NetworkObject netObj = waveObj.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
                m_SpawnedWave = waveObj.GetComponent<ToxicWaveController>();
                
                // You can override the trigger distance here if needed
                // m_SpawnedWave.triggerDistance = triggerDistance;
            }
            else
            {
                Debug.LogError("[ToxicWaveManager] ToxicWave Prefab MUST have a NetworkObject component!");
            }
        }
    }
}
