using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// วาง Component นี้บน GameObject ที่มี Trigger Collider เพื่อสร้างพื้นที่กันลม
    /// จะ Register ตัวเองกับ StormEventManager อัตโนมัติเมื่อ spawn
    /// รองรับ dynamic spawn จาก Train และ Tile Map
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StormSafeZone : MonoBehaviour
    {
        private readonly HashSet<ulong> _playersInside = new HashSet<ulong>();
        private Collider _zoneCollider;

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider>();
            if (_zoneCollider != null) _zoneCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            // ลงทะเบียนกับ StormEventManager ทันทีที่ zone นี้ activate
            // ทำงานได้ทั้งกรณี pre-placed และ dynamic spawn (Train/Tile)
            if (StormEventManager.Instance != null)
                StormEventManager.Instance.RegisterSafeZone(this);
            else
                // กรณี StormEventManager ยังไม่ spawn (load order) — รอแล้วลองใหม่
                StartCoroutine(RegisterWhenReady());
        }

        private void OnDisable()
        {
            // ถอนตัวเองออกเมื่อ zone ถูก despawn หรือ destroy
            // ครอบคลุมกรณี Tile ที่ถูก recycle หรือ Train ที่ออกจากแมพ
            if (StormEventManager.Instance != null)
                StormEventManager.Instance.UnregisterSafeZone(this);
        }

        private System.Collections.IEnumerator RegisterWhenReady()
        {
            // รอจนกว่า StormEventManager จะ spawn (max 10 วิ)
            float waited = 0f;
            while (StormEventManager.Instance == null && waited < 10f)
            {
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }

            if (StormEventManager.Instance != null)
                StormEventManager.Instance.RegisterSafeZone(this);
            else
                Debug.LogWarning($"[StormSafeZone] StormEventManager not found after waiting — {gameObject.name} will not protect players.");
        }

        private void OnTriggerEnter(Collider other)
        {
            // GetComponentInParent ป้องกันกรณี Collider อยู่บน child object
            // แต่ NetworkObject อยู่บน root ของ player
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null && _playersInside.Add(netObj.OwnerClientId))
                Debug.Log($"[StormSafeZone] Client {netObj.OwnerClientId} entered {gameObject.name}");
        }

        private void OnTriggerExit(Collider other)
        {
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null && _playersInside.Remove(netObj.OwnerClientId))
                Debug.Log($"[StormSafeZone] Client {netObj.OwnerClientId} exited {gameObject.name}");
        }

        public bool IsPlayerInside(ulong clientId) => _playersInside.Contains(clientId) || IsPlayerCurrentlyOverlapping(clientId);
        public int PlayerCount => _playersInside.Count;

        private bool IsPlayerCurrentlyOverlapping(ulong clientId)
        {
            if (_zoneCollider == null ||
                NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) ||
                client.PlayerObject == null)
            {
                return false;
            }

            Vector3 playerPosition = client.PlayerObject.transform.position;
            if (_zoneCollider.bounds.Contains(playerPosition) ||
                Vector3.Distance(_zoneCollider.ClosestPoint(playerPosition), playerPosition) <= 0.05f)
            {
                return true;
            }

            Collider[] playerColliders = client.PlayerObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider playerCollider = playerColliders[i];
                if (playerCollider == null || !playerCollider.enabled)
                {
                    continue;
                }

                if (_zoneCollider.bounds.Intersects(playerCollider.bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
