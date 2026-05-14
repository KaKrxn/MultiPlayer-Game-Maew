using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative storage component สำหรับวัตถุ Vault หนึ่งใบในซีน
/// เก็บข้อมูล slot ทั้งหมดบน server ผ่าน NetworkList และให้ client เปิด/ปิด UI ผ่าน VaultUI
///
/// ใช้คู่กับ:
///  - Vault (AInteractable) -> เปิด UI เมื่อกด E
///  - VaultUI               -> ฝั่ง client สำหรับ render slots และรองรับ drag-drop
///  - InventoryNetworkHandler -> ฝั่ง player สำหรับ transfer ของข้ามไป-มา
/// </summary>
[DisallowMultipleComponent]
public class NetworkVault : NetworkBehaviour, IItemContainer
{
    public ContainerKind Kind => ContainerKind.Vault;

    [Header("Vault Settings")]
    [Tooltip("จำนวนช่องของ Vault ใบนี้ (12 = 4x3, 24 = 6x4)")]
    [SerializeField] private int slotCount = 12;

    /// <summary>
    /// Server-authoritative state ของ vault. Index สอดคล้องกับ slot ที่ VaultUI ใช้ render
    /// </summary>
    private NetworkList<NetworkInventorySlotData> _vaultData;

    public int SlotCount => slotCount;
    public NetworkList<NetworkInventorySlotData> VaultData => _vaultData;

    private void Awake()
    {
        _vaultData = new NetworkList<NetworkInventorySlotData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // เติม slot ว่างให้ครบจำนวน (ครั้งเดียวตอน spawn)
            if (_vaultData.Count == 0)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    _vaultData.Add(NetworkInventorySlotData.Empty);
                }
            }
        }
    }

    // ── Server RPCs ──────────────────────────────────────────────

    /// <summary>
    /// ย้ายของภายใน vault เดียวกัน (สลับช่อง)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveWithinVaultServerRpc(int fromIndex, int toIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex)) return;
        if (fromIndex == toIndex) return;

        var temp = _vaultData[toIndex];
        _vaultData[toIndex] = _vaultData[fromIndex];
        _vaultData[fromIndex] = temp;
    }

    /// <summary>
    /// ย้ายของจากกระเป๋าผู้เล่น (sender) มาที่ vault ช่อง vaultSlotIndex
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestTransferToVaultServerRpc(int playerSlotIndex, int vaultSlotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsValidIndex(vaultSlotIndex)) return;

        var sender = GetSenderInventory(rpcParams.Receive.SenderClientId);
        if (sender == null) return;

        var playerData = sender.ReadServerSlot(playerSlotIndex);
        if (playerData.isEmpty) return;

        // สลับ: เอาของ player ใส่ vault, ของเดิมใน vault ส่งกลับไปช่อง player
        var vaultExisting = _vaultData[vaultSlotIndex];
        _vaultData[vaultSlotIndex] = playerData;
        sender.TrySetServerSlotData(playerSlotIndex, vaultExisting);
    }

    /// <summary>
    /// ย้ายของจาก vault ช่อง vaultSlotIndex ไปกระเป๋าผู้เล่น (sender) ช่อง playerSlotIndex
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestTransferFromVaultServerRpc(int vaultSlotIndex, int playerSlotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsValidIndex(vaultSlotIndex)) return;

        var sender = GetSenderInventory(rpcParams.Receive.SenderClientId);
        if (sender == null) return;

        var vaultData = _vaultData[vaultSlotIndex];
        if (vaultData.isEmpty) return;

        var playerExisting = sender.ReadServerSlot(playerSlotIndex);
        if (!sender.TrySetServerSlotData(playerSlotIndex, vaultData)) return;
        _vaultData[vaultSlotIndex] = playerExisting;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool IsValidIndex(int index)
    {
        return _vaultData != null && index >= 0 && index < _vaultData.Count;
    }

    private InventoryNetworkHandler GetSenderInventory(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<InventoryNetworkHandler>();
    }
}
