using System.Reflection;
using Unity.Services.Multiplayer;

public enum RoomFlowState
{
    None,
    Lobby,
    InGame
}

public static class RoomRuntimeState
{
    public static RoomFlowState CurrentState { get; private set; } = RoomFlowState.None;
    public static bool IsLocked { get; private set; }
    public static string RoomName { get; private set; } = string.Empty;
    public static string JoinCode { get; private set; } = string.Empty;

    public static void EnterLobby(ISession session, bool isLocked)
    {
        CurrentState = RoomFlowState.Lobby;
        IsLocked = isLocked;
        RoomName = session != null ? session.Name : string.Empty;
        JoinCode = ReadJoinCode(session);
    }

    public static void StartGame(bool isLocked)
    {
        CurrentState = RoomFlowState.InGame;
        IsLocked = isLocked;
    }

    public static void SetLocked(bool isLocked)
    {
        IsLocked = isLocked;
    }

    public static void Clear()
    {
        CurrentState = RoomFlowState.None;
        IsLocked = false;
        RoomName = string.Empty;
        JoinCode = string.Empty;
    }

    private static string ReadJoinCode(ISession session)
    {
        if (session == null)
            return "-";

        string[] names = { "Code", "SessionCode", "JoinCode" };

        foreach (string name in names)
        {
            PropertyInfo prop = session.GetType().GetProperty(name);
            if (prop == null)
                continue;

            object value = prop.GetValue(session);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString();
        }

        return "-";
    }
}