using System.Collections.Generic;

/// <summary>
/// Server-side snapshot of clients that were already connected when the host
/// started the game from the lobby.
/// </summary>
public static class LobbyClientRegistry
{
    private static readonly HashSet<ulong> s_LobbyClientIds = new HashSet<ulong>();

    public static IReadOnlyCollection<ulong> LobbyClientIds => s_LobbyClientIds;

    public static void CaptureLobbyClients(IEnumerable<ulong> clientIds)
    {
        s_LobbyClientIds.Clear();

        if (clientIds == null)
            return;

        foreach (ulong clientId in clientIds)
            s_LobbyClientIds.Add(clientId);
    }

    public static bool Contains(ulong clientId)
    {
        return s_LobbyClientIds.Contains(clientId);
    }

    public static void Clear()
    {
        s_LobbyClientIds.Clear();
    }
}
