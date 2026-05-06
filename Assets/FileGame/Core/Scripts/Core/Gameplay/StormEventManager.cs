using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    public class StormEventManager : NetworkBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        // StormSafeZone ใช้ Instance นี้เพื่อ Register/Unregister ตัวเอง
        public static StormEventManager Instance { get; private set; }

        [Header("Timer Settings")]
        [SerializeField] private float rollIntervalMinutes = 5f;
        [SerializeField] private float baseProbability = 20f;
        [SerializeField] private float maxProbability = 100f;
        [SerializeField] private float probabilityMultiplier = 2f;

        [Header("Event Settings")]
        [SerializeField] private float eventDurationSeconds = 30f;
        [SerializeField] private float damageTick = 2f;
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private int jacketDurabilityDrain = 10;

        [Header("References")]
        [SerializeField] private GameObject stormParticlePrefab;

        // Safe Zones ที่ spawn/despawn แบบ dynamic จาก Train & Tile Map
        // StormSafeZone จะ Register ตัวเองมาที่นี่อัตโนมัติ
        private readonly List<StormSafeZone> _registeredSafeZones = new List<StormSafeZone>();

        private float _rollTimer;
        private float _currentProbability;
        private bool _stormActive;
        private float _stormTimer;
        private float _damageTimer;

        // ── Registration API ─────────────────────────────────────────────────
        // เรียกจาก StormSafeZone.OnEnable — ลงทะเบียน zone ใหม่ที่ spawn ขึ้นมา
        public void RegisterSafeZone(StormSafeZone zone)
        {
            if (zone != null && !_registeredSafeZones.Contains(zone))
            {
                _registeredSafeZones.Add(zone);
                Debug.Log($"[StormEventManager] SafeZone registered: {zone.gameObject.name} (total: {_registeredSafeZones.Count})");
            }
        }

        // เรียกจาก StormSafeZone.OnDisable/OnDestroy — ถอนตัวเองออกจาก list
        public void UnregisterSafeZone(StormSafeZone zone)
        {
            if (_registeredSafeZones.Remove(zone))
            {
                Debug.Log($"[StormEventManager] SafeZone unregistered: {zone.gameObject.name} (total: {_registeredSafeZones.Count})");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            _currentProbability = baseProbability;
            Debug.Log($"[StormEventManager] ✅ Spawned | IsServer={IsServer} | IsClient={IsClient} | " +
                      $"RollInterval={rollIntervalMinutes} min | BaseProbability={baseProbability}%");
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (!_stormActive)
            {
                _rollTimer += Time.deltaTime;

                // Log ความคืบหน้า timer ทุก 30 วิ เพื่อยืนยันว่า Update ทำงาน
                if (Mathf.FloorToInt(_rollTimer) % 30 == 0 && Mathf.FloorToInt(_rollTimer) > 0
                    && Time.frameCount % 100 == 0)
                {
                    float remaining = (rollIntervalMinutes * 60f) - _rollTimer;
                    Debug.Log($"[StormEventManager] ⏱ Waiting to roll | " +
                              $"Timer={_rollTimer:F1}s / {rollIntervalMinutes * 60f:F0}s | " +
                              $"Next roll in {remaining:F0}s | CurrentProbability={_currentProbability:F1}%");
                }

                if (_rollTimer >= rollIntervalMinutes * 60f)
                {
                    _rollTimer = 0f;
                    float roll = Random.Range(0f, 100f);

                    if (roll < _currentProbability)
                    {
                        // HIT — storm triggers
                        Debug.Log($"[StormEventManager] 🎲 ROLL RESULT: HIT! | " +
                                  $"Roll={roll:F1} < Probability={_currentProbability:F1}% | " +
                                  $"→ Storm triggered! Probability reset to {baseProbability}%");
                        _currentProbability = baseProbability;
                        StartStorm();
                    }
                    else
                    {
                        // MISS — probability doubles
                        float oldProb = _currentProbability;
                        _currentProbability = Mathf.Min(
                            _currentProbability * probabilityMultiplier, maxProbability);
                        Debug.Log($"[StormEventManager] 🎲 ROLL RESULT: MISS | " +
                                  $"Roll={roll:F1} >= Probability={oldProb:F1}% | " +
                                  $"→ Probability increased: {oldProb:F1}% → {_currentProbability:F1}%");
                    }
                }
            }
            else
            {
                _stormTimer += Time.deltaTime;
                _damageTimer += Time.deltaTime;

                if (_damageTimer >= damageTick)
                {
                    _damageTimer = 0f;
                    ApplyDamageToAllPlayers();
                }

                if (_stormTimer >= eventDurationSeconds)
                {
                    EndStorm();
                }
            }
        }

        private void StartStorm()
        {
            _stormActive = true;
            _stormTimer = 0f;
            _damageTimer = 0f;
            Debug.Log($"[StormEventManager] ⛈ STORM STARTED | " +
                      $"Duration={eventDurationSeconds}s | DamageTick={damageTick}s | " +
                      $"Damage={damageAmount} | JacketDrain={jacketDurabilityDrain} | " +
                      $"SafeZones active={_registeredSafeZones.Count} | " +
                      $"Players={NetworkManager.Singleton.ConnectedClientsList.Count}");
            PlayStormEffectClientRpc();
        }

        private void EndStorm()
        {
            _stormActive = false;
            Debug.Log($"[StormEventManager] ☀️ STORM ENDED | " +
                      $"Duration lasted={_stormTimer:F1}s | " +
                      $"Next roll probability={_currentProbability:F1}%");
            StopStormEffectClientRpc();
        }

        private void ApplyDamageToAllPlayers()
        {
            int totalPlayers   = 0;
            int safePlayers    = 0;
            int jacketPlayers  = 0;
            int damagedPlayers = 0;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                totalPlayers++;
                ulong id = client.ClientId;

                bool safe = _registeredSafeZones.Any(z => z != null && z.IsPlayerInside(id));
                if (safe)
                {
                    safePlayers++;
                    Debug.Log($"[StormEventManager] 🛡 Client {id} — SAFE (inside SafeZone), skipping damage");
                    continue;
                }

                damagedPlayers++;
                var p = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { id } }
                };
                TryDrainJacketOrApplyPainClientRpc(p);
            }

            Debug.Log($"[StormEventManager] 💨 Damage tick | " +
                      $"Total={totalPlayers} | Safe={safePlayers} | Sent damage RPC={damagedPlayers}");
        }

        [ClientRpc]
        private void TryDrainJacketOrApplyPainClientRpc(ClientRpcParams rpcParams = default)
        {
            if (InventoryManager.instance == null)
            {
                Debug.LogWarning($"[StormEventManager] ⚠️ [CLIENT] InventoryManager.instance is NULL — cannot check jacket");
                RequestApplyPainServerRpc();
                return;
            }

            bool hadJacket = InventoryManager.instance.TryConsumeJacketDurability(jacketDurabilityDrain);

            if (hadJacket)
            {
                Debug.Log($"[StormEventManager] 🧥 [CLIENT] Jacket absorbed damage | Drained={jacketDurabilityDrain} durability");
            }
            else
            {
                Debug.Log($"[StormEventManager] 💔 [CLIENT] No jacket found — requesting Pain damage from Server");
                // ModifyStat ต้องรันบน Server เท่านั้น — ส่ง ServerRpc กลับไปให้ Server apply pain
                RequestApplyPainServerRpc();
            }
        }

        // Client ส่ง request กลับมาที่ Server เพื่อ apply pain damage
        [ServerRpc(RequireOwnership = false)]
        private void RequestApplyPainServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
            {
                Debug.LogWarning($"[StormEventManager] ⚠️ [SERVER] RequestApplyPain — Client {senderId} not found");
                return;
            }
            if (client.PlayerObject == null)
            {
                Debug.LogWarning($"[StormEventManager] ⚠️ [SERVER] RequestApplyPain — PlayerObject null for Client {senderId}");
                return;
            }

            var stats = client.PlayerObject.GetComponent<CoreStatsHandler>();
            if (stats == null)
            {
                Debug.LogWarning($"[StormEventManager] ⚠️ [SERVER] RequestApplyPain — CoreStatsHandler not found on Client {senderId}");
                return;
            }

            stats.ModifyStat(Animator.StringToHash("Pain"),
                damageAmount, senderId, ModificationSource.Environmental);

            Debug.Log($"[StormEventManager] 🩸 [SERVER] Applied Pain +{damageAmount} to Client {senderId}");
        }

        [ClientRpc]
        private void PlayStormEffectClientRpc()
        {
            Debug.Log($"[StormEventManager] 🌩 [CLIENT] PlayStormEffect received | " +
                      $"ParticlePrefab={(stormParticlePrefab != null ? stormParticlePrefab.name : "NULL")}");

            if (stormParticlePrefab == null)
            {
                Debug.LogWarning("[StormEventManager] ⚠️ [CLIENT] stormParticlePrefab is not assigned in Inspector!");
                return;
            }

            // Spawn ที่ตำแหน่ง Camera ของ client แต่ละคน ไม่ใช่ world origin
            Vector3 spawnPos = Camera.main != null
                ? Camera.main.transform.position
                : Vector3.zero;
            var go = Instantiate(stormParticlePrefab, spawnPos, Quaternion.identity);
            go.tag = "StormEffect";
            Debug.Log($"[StormEventManager] ✅ [CLIENT] Particle spawned at {spawnPos}");
        }

        [ClientRpc]
        private void StopStormEffectClientRpc()
        {
            var go = GameObject.FindWithTag("StormEffect");
            if (go != null)
            {
                Destroy(go);
                Debug.Log("[StormEventManager] ☀️ [CLIENT] StormEffect particle destroyed");
            }
            else
            {
                Debug.LogWarning("[StormEventManager] ⚠️ [CLIENT] StopStorm — no StormEffect found to destroy");
            }
        }
    }
}
