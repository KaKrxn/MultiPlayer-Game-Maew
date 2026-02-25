using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TrainRepairSystem : NetworkBehaviour
{
    [SerializeField] private TrainNetworkController train;
    [SerializeField] private float baseRepairPerSecond = 5f;

    // รายชื่อผู้เล่นที่กำลังช่วยซ่อม (ฝั่ง Server)
    private readonly HashSet<ulong> _repairingPlayers = new();

    [ServerRpc(RequireOwnership = false)]
    public void StartRepairServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // TODO: Validate ว่ามีวัสดุพอไหม, อยู่ในตำแหน่งที่ซ่อมได้ ฯลฯ
        _repairingPlayers.Add(senderId);

        if (!isRepairing)
        {
            isRepairing = true;
            StartCoroutine(RepairCoroutine());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopRepairServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        _repairingPlayers.Remove(senderId);
    }

    private bool isRepairing;

    private IEnumerator RepairCoroutine()
    {
        while (isRepairing)
        {
            yield return null;
            if (!IsServer || train == null) yield break;

            int playersCount = _repairingPlayers.Count;
            if (playersCount == 0)
            {
                isRepairing = false;
                yield break;
            }

            float dt = Time.deltaTime;
            float repairAmount = baseRepairPerSecond * playersCount * dt;

            // เรียกผ่านเมทอดฝั่ง Server ของ TrainNetworkController
            float maxHealth = train.MaxHealth;
            float newHealth = Mathf.Min(
                train.Health.Value + repairAmount,
                maxHealth
            );
            train.Health.Value = newHealth;

            if (train.Health.Value > 0f)
                train.SetShutdown(false);

            // ถ้า HP เต็มแล้ว → เลิกซ่อม
            if (newHealth >= maxHealth)
            {
                isRepairing = false;
                yield break;
            }
        }
    }
}