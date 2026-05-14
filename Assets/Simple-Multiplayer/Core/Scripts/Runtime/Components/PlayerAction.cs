using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages player death, spectator mode, and revival logic.
    /// Death and revival are server-authoritative to prevent client exploitation.
    /// </summary>
    public class PlayerAction : NetworkBehaviour
    {
        [Header("Settings")]
        public float reviveDistance = 3f; // ปรับเป็นค่า Default ของคุณได้เลย เช่น FileGame.Core.GameConstants.DefaultReviveDistance
        public string graveyardTag = "Graveyard";

        [Header("Camera Settings")]
        public Transform cameraTarget; 

        // Server-authoritative death state (prevents client-side death toggle exploits)
        public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);

        // Server-authoritative body position for accurate revive distance checks
        public NetworkVariable<Vector3> actualBodyPosition = new NetworkVariable<Vector3>(Vector3.zero, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);

        private CinemachineCamera _vCam;
        private Transform _myOriginalTarget;

        // Cache player list to avoid FindObjectsByType every frame
        private static readonly System.Collections.Generic.List<PlayerAction> _allPlayers = new();

        // 🛑 เปลี่ยนเป็น public static เพื่อให้เรียก PlayerAction.instance ได้จากทุกที่
        public static PlayerAction instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            _allPlayers.Add(this);

            if (IsOwner)
            {
                // 🛑 ให้ instance จำเฉพาะร่างที่เป็นของเครื่องเราเท่านั้น
                instance = this;

                _vCam = GameObject.FindAnyObjectByType<CinemachineCamera>();
                if (_vCam != null)
                {
                    _myOriginalTarget = cameraTarget != null ? cameraTarget : transform; 
                    _vCam.Follow = _myOriginalTarget;
                    _vCam.LookAt = _myOriginalTarget;
                }
            }
            isDead.OnValueChanged += OnDeathStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                // 🛑 ล้างค่าทิ้งเมื่อออกเกม หรือตัวละครถูกทำลาย
                instance = null;
            }

            _allPlayers.Remove(this);
            isDead.OnValueChanged -= OnDeathStateChanged;
        }

        private void OnDeathStateChanged(bool previousValue, bool newValue)
        {
            if (!IsOwner) return;

            if (newValue)
            {
                Debug.Log("[PlayerAction] You died! Spectating...");
                StartSpectating();
            }
            else
            {
                Debug.Log("[PlayerAction] You revived!");
                StopSpectating();
            }
        }

        private void StartSpectating()
        {
            if (_vCam == null) _vCam = GameObject.FindAnyObjectByType<CinemachineCamera>();
            if (_vCam == null) return;

            // Use cached player list instead of FindObjectsByType
            foreach (var player in _allPlayers)
            {
                if (player != this && !player.isDead.Value)
                {
                    Transform target = player.cameraTarget != null ? player.cameraTarget : player.transform;
                    _vCam.Follow = target;
                    _vCam.LookAt = target;
                    return; 
                }
            }
        }

        private void StopSpectating()
        {
            if (_vCam != null && _myOriginalTarget != null)
            {
                _vCam.Follow = _myOriginalTarget;
                _vCam.LookAt = _myOriginalTarget;
            }
        }

        /// <summary>
        /// Called by local owner to request death. Server handles the actual state change.
        /// </summary>
        public void LocalDie()
        {
            UnityEngine.Debug.Log("[PlayerAction] LocalDie called. ++++++++");
            if (!IsOwner || isDead.Value) return;

            GameObject graveyard = GameObject.FindGameObjectWithTag(graveyardTag);
            Vector3 gravePos = graveyard != null ? graveyard.transform.position : transform.position;

            // Request server to set death state and teleport
            RequestDieServerRpc(gravePos);
        }

        [ServerRpc]
        private void RequestDieServerRpc(Vector3 graveyardPosition)
        {
            if (isDead.Value) return;

            isDead.Value = true;
            actualBodyPosition.Value = graveyardPosition;

            // Teleport player to graveyard on all clients
            TeleportClientRpc(graveyardPosition);
        }

        /// <summary>
        /// Called by a living player to revive a nearby dead player.
        /// </summary>
        public void LocalRevive()
        {
            if (!IsOwner || isDead.Value) return;

            // Use cached player list instead of FindObjectsByType
            foreach (var targetPlayer in _allPlayers)
            {
                if (targetPlayer != this && targetPlayer.isDead.Value)
                {
                    Vector2 myPos = new Vector2(transform.position.x, transform.position.z);
                    Vector2 targetPos = new Vector2(
                        targetPlayer.actualBodyPosition.Value.x, 
                        targetPlayer.actualBodyPosition.Value.z);
                    
                    float distance = Vector2.Distance(myPos, targetPos);
                    
                    Debug.Log($"[PlayerAction] Distance to {targetPlayer.gameObject.name}: {distance:F2}m (Network pos: {targetPlayer.actualBodyPosition.Value})");

                    if (distance <= reviveDistance)
                    {
                        Debug.Log("[PlayerAction] In revive range! Sending request to server.");
                        targetPlayer.RequestReviveServerRpc();
                        return;
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc()
        {
            if (!isDead.Value) return;

            isDead.Value = false;
            Debug.Log($"[PlayerAction] Server revived player {OwnerClientId}.");
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 targetPos)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            transform.position = targetPos;
            
            if (cc != null) cc.enabled = true;
            
            Debug.Log($"[PlayerAction] Teleported to: {targetPos}");
        }


        /// <summary>
        /// เช็คสถานะการตายของตัวละครนี้ (ใช้ผ่าน instance)
        /// </summary>
        public bool CheckIsDead()
        {
            return isDead.Value;
        }

        /// <summary>
        /// (แนะนำ) ฟังก์ชัน Static สำหรับเช็คว่า Local Player ของเราตายอยู่หรือไม่
        /// เรียกใช้ได้เลยโดยไม่ต้องเช็ค null instance ให้วุ่นวาย
        /// </summary>
        public static bool IsLocalPlayerDead()
        {
            if (instance != null)
            {
                return instance.isDead.Value;
            }
            
            // ถ้าตัวละครยังไม่เกิด (instance เป็น null) ให้คืนค่า false ไปก่อน
            return false; 
        }
    }
}
