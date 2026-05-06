using System;
using System.Collections.Generic;
using Blocks.Gameplay.Core;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public struct PlayerNameEntry : INetworkSerializable, IEquatable<PlayerNameEntry>
{
    public ulong ClientId;
    public FixedString64Bytes Name;

    public PlayerNameEntry(ulong clientId, string name)
    {
        ClientId = clientId;
        Name = new FixedString64Bytes(PlayerNameRegistry.SanitizeName(name));
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
    }

    public bool Equals(PlayerNameEntry other)
    {
        return ClientId == other.ClientId && Name.Equals(other.Name);
    }
}

public class PlayerNameRegistry : NetworkBehaviour
{
    public const string PlayerPrefsKey = "PlayerName";
    public const string PlayerNamePropertyKey = "PlayerName";
    public const string FallbackName = "Player";

    public static PlayerNameRegistry Instance { get; private set; }

    public event Action<ulong, string> NameChanged;

    private readonly NetworkList<PlayerNameEntry> m_Names = new NetworkList<PlayerNameEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerNameRegistry] Multiple registries are active. Keeping the newest scene instance.");
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        m_Names.OnListChanged += HandleNameListChanged;

        if (IsServer)
            EnsureNameForClient(NetworkManager.LocalClientId, GetSavedLocalPlayerName());

        if (IsClient)
            SubmitLocalNameServerRpc(GetSavedLocalPlayerName());
    }

    public override void OnNetworkDespawn()
    {
        m_Names.OnListChanged -= HandleNameListChanged;

        if (Instance == this)
            Instance = null;
    }

    public static string GetSavedLocalPlayerName()
    {
        string savedName = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(PlayerRuntimeData.LocalPlayerName) &&
            PlayerRuntimeData.LocalPlayerName != FallbackName)
        {
            savedName = PlayerRuntimeData.LocalPlayerName;
        }

        return SanitizeName(savedName);
    }

    public static void SaveLocalPlayerName(string playerName)
    {
        string sanitized = SanitizeName(playerName);
        PlayerPrefs.SetString(PlayerPrefsKey, sanitized);
        PlayerPrefs.Save();
        PlayerRuntimeData.LocalPlayerName = sanitized;
    }

    public static string SanitizeName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return FallbackName;

        string sanitized = playerName.Trim();
        return sanitized.Length > 20 ? sanitized.Substring(0, 20) : sanitized;
    }

    public static string ReadSessionPlayerName(IReadOnlyPlayer player)
    {
        if (player == null)
            return FallbackName;

        if (player.Properties != null &&
            player.Properties.TryGetValue(PlayerNamePropertyKey, out PlayerProperty property) &&
            property != null &&
            !string.IsNullOrWhiteSpace(property.Value))
        {
            return SanitizeName(property.Value);
        }

        string unityPlayerName = player.GetPlayerName();
        return !string.IsNullOrWhiteSpace(unityPlayerName)
            ? SanitizeName(unityPlayerName)
            : FallbackName;
    }

    public string GetName(ulong clientId)
    {
        int index = IndexOf(clientId);
        if (index >= 0)
            return SanitizeName(m_Names[index].Name.ToString());

        return $"{FallbackName} {clientId}";
    }

    public bool TryGetName(ulong clientId, out string playerName)
    {
        int index = IndexOf(clientId);
        if (index >= 0)
        {
            playerName = SanitizeName(m_Names[index].Name.ToString());
            return true;
        }

        playerName = string.Empty;
        return false;
    }

    public void EnsureNameForClient(ulong clientId, string fallbackName = null)
    {
        if (!IsServer)
            return;

        if (IndexOf(clientId) >= 0)
            return;

        SetName(clientId, string.IsNullOrWhiteSpace(fallbackName) ? $"{FallbackName} {clientId}" : fallbackName);
    }

    public void RegisterPlayerObject(ulong clientId, NetworkObject playerObject)
    {
        if (!IsServer)
            return;

        EnsureNameForClient(clientId);
        Debug.Log($"[PlayerNameRegistry] Player object linked | ClientId={clientId} | Name='{GetName(clientId)}' | NetworkObjectId={(playerObject != null ? playerObject.NetworkObjectId.ToString() : "null")}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitLocalNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        SetName(rpcParams.Receive.SenderClientId, playerName);
    }

    private void SetName(ulong clientId, string playerName)
    {
        if (!IsServer)
            return;

        PlayerNameEntry entry = new PlayerNameEntry(clientId, playerName);
        int index = IndexOf(clientId);

        if (index >= 0)
            m_Names[index] = entry;
        else
            m_Names.Add(entry);

        Debug.Log($"[PlayerNameRegistry] SetName | ClientId={clientId} | Name='{entry.Name}'");
    }

    private int IndexOf(ulong clientId)
    {
        for (int i = 0; i < m_Names.Count; i++)
        {
            if (m_Names[i].ClientId == clientId)
                return i;
        }

        return -1;
    }

    private void HandleNameListChanged(NetworkListEvent<PlayerNameEntry> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<PlayerNameEntry>.EventType.Clear)
            return;

        string playerName = SanitizeName(changeEvent.Value.Name.ToString());
        NameChanged?.Invoke(changeEvent.Value.ClientId, playerName);
    }

    public IReadOnlyDictionary<ulong, string> Snapshot()
    {
        Dictionary<ulong, string> snapshot = new Dictionary<ulong, string>();
        for (int i = 0; i < m_Names.Count; i++)
            snapshot[m_Names[i].ClientId] = SanitizeName(m_Names[i].Name.ToString());

        return snapshot;
    }
}
