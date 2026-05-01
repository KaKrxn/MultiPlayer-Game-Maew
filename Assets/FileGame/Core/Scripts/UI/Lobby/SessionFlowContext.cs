using Unity.Services.Multiplayer;

public static class SessionFlowContext
{
    public static ISession CurrentSession { get; private set; }
    public static bool IsHost { get; private set; }

    public static void SetCurrentSession(ISession session, bool isHost)
    {
        CurrentSession = session;
        IsHost = isHost;
    }

    public static void Clear()
    {
        CurrentSession = null;
        IsHost = false;
        RoomRuntimeState.Clear();
    }
}