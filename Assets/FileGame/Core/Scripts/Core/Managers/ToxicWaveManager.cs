using UnityEngine;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages the spawning of the Toxic Wave.
    /// Changed to MonoBehaviour to ensure it runs even if the Manager object itself isn't a NetworkObject.
    /// </summary>
    public class ToxicWaveManager : MonoBehaviour
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject toxicWavePrefab;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0, -10f); // Spawn slightly behind start

        [Header("Trigger Settings")]
        [SerializeField] private float triggerDistance = 500f; // Distance before wave starts moving

        private ToxicWaveController m_SpawnedWave;
        private bool m_HasSpawned = false;

        private void Awake()
        {
            Debug.Log($"[ToxicWaveManager] Awake called on GameObject: {gameObject.name}");
        }

        private void Start()
        {
            Debug.Log($"[ToxicWaveManager] Start called. Waiting for NetworkManager server to start...");
        }

        private void Update()
        {
            // We wait for the NetworkManager to be ready and for the local instance to be the Server/Host
            if (!m_HasSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // IsListening ensures the handshake is done and we are actually running
                if (NetworkManager.Singleton.IsListening)
                {
                    SpawnWave();
                    m_HasSpawned = true;
                }
            }
        }

        private void SpawnWave()
        {
            if (toxicWavePrefab == null)
            {
                Debug.LogError("[ToxicWaveManager] ToxicWave Prefab is missing! Assing it in the inspector.");
                return;
            }

            // Spawn the wave at the specified offset from the manager's position 
            Vector3 spawnPos = transform.position + spawnOffset;
            GameObject waveObj = Instantiate(toxicWavePrefab, spawnPos, Quaternion.identity);
            
            NetworkObject netObj = waveObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // This makes the object appear on all clients
                netObj.Spawn();
                m_SpawnedWave = waveObj.GetComponent<ToxicWaveController>();
                
                Debug.Log($"[ToxicWaveManager] SUCCESS! ToxicWave spawned at {spawnPos}.");
                
                // Optional: sync trigger distance if needed
                // if (m_SpawnedWave != null) m_SpawnedWave.triggerDistance = triggerDistance;
            }
            else
            {
                Debug.LogError("[ToxicWaveManager] ERROR: ToxicWave Prefab MUST have a NetworkObject component to sync across players!");
                Destroy(waveObj); // Clean up failed spawn
            }
        }
    }
}
