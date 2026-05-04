using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerAction : NetworkBehaviour
{
    [Header("Settings")]
    public float reviveDistance = 3f;
    public string graveyardTag = "Graveyard";

    [Header("Camera Settings")]
    public Transform cameraTarget; 

    // ตัวแปรสถานะ
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);

    // 🛑 [เพิ่มใหม่] ตัวแปรสำหรับซิงค์พิกัดที่ถูกต้องหลังจากการวาร์ป
    public NetworkVariable<Vector3> actualBodyPosition = new NetworkVariable<Vector3>(Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);

    private CinemachineCamera _vCam;
    private Transform _myOriginalTarget;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
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
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    private void OnDeathStateChanged(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;

        if (newValue == true)
        {
            Debug.Log("💀 [Local] You died! Spectating...");
            StartSpectating();
        }
        else
        {
            Debug.Log("😇 [Local] You revived!");
            StopSpectating();
        }
    }

    private void StartSpectating()
    {
        if (_vCam == null) _vCam = GameObject.FindAnyObjectByType<CinemachineCamera>();
        if (_vCam == null) return;

        PlayerAction[] allPlayers = FindObjectsByType<PlayerAction>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
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

    public void LocalDie()
    {
        if (!IsOwner || isDead.Value) return;

        isDead.Value = true;
        
        GameObject graveyard = GameObject.FindGameObjectWithTag(graveyardTag);
        if (graveyard != null)
        {
            Vector3 gravePos = graveyard.transform.position;
            
            // 🛑 1. อัปเดตพิกัดว่า "ศพฉันอยู่ที่สุสานนะ" ส่งให้ทุกคนใน Network รู้
            actualBodyPosition.Value = gravePos;
            
            // 🛑 2. สั่งวาร์ปผ่าน Server เพื่อให้ NetworkTransform ไม่ดึงกลับไปที่เดิม
            TeleportServerRpc(gravePos);
        }
        else
        {
            actualBodyPosition.Value = transform.position;
        }
    }

    // --- ระบบชุบชีวิต ---
    public void LocalRevive()
    {
        if (!IsOwner || isDead.Value) return; 

        PlayerAction[] allPlayers = FindObjectsByType<PlayerAction>(FindObjectsSortMode.None);
        
        foreach (var targetPlayer in allPlayers)
        {
            if (targetPlayer != this && targetPlayer.isDead.Value)
            {
                // 🛑 ใช้ `actualBodyPosition` ที่อัปเดตเป็นตำแหน่งสุสานแล้วมาคำนวณ
                Vector2 myPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 targetPos = new Vector2(targetPlayer.actualBodyPosition.Value.x, targetPlayer.actualBodyPosition.Value.z);
                
                float distance = Vector2.Distance(myPos, targetPos);
                
                Debug.Log($"📏 [Check] ห่างจาก {targetPlayer.gameObject.name}: {distance:F2} ม. (พิกัดใน Network: {targetPlayer.actualBodyPosition.Value})");

                if (distance <= reviveDistance)
                {
                    Debug.Log("✨ [Success] อยู่ในระยะชุบ! ส่งคำสั่งไปที่ Server");
                    targetPlayer.RequestReviveServerRpc();
                    return;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestReviveServerRpc()
    {
        ReceiveReviveClientRpc();
    }

    [ClientRpc]
    private void ReceiveReviveClientRpc()
    {
        if (IsOwner) isDead.Value = false;
    }

    // -------------------------------------------------------------------
    // 🛑 ระบบวาร์ปฉบับ Multiplayer (บังคับวาร์ปทุกเครื่องป้องกันภาพลวงตา)
    // -------------------------------------------------------------------
    [ServerRpc(RequireOwnership = false)]
    private void TeleportServerRpc(Vector3 targetPos)
    {
        TeleportClientRpc(targetPos);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 targetPos)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        transform.position = targetPos;
        
        if (cc != null) cc.enabled = true;
        
        Debug.Log($"🚀 [Warp] อัปเดตพิกัดโมเดลสำเร็จไปที่: {targetPos}");
    }
}