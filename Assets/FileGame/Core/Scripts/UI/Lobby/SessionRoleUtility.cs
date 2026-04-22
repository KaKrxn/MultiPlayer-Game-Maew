using System.Reflection;
using Unity.Services.Multiplayer;

public static class SessionRoleUtility
{
    public static bool IsLocalPlayerHost(ISession session)
    {
        if (session == null || session.CurrentPlayer == null)
            return false;

        string localPlayerId = session.CurrentPlayer.Id;
        string hostPlayerId = ReadHostPlayerId(session);

        return !string.IsNullOrEmpty(localPlayerId) &&
               !string.IsNullOrEmpty(hostPlayerId) &&
               localPlayerId == hostPlayerId;
    }

    public static string ReadHostPlayerId(ISession session)
    {
        if (session == null)
            return string.Empty;

        PropertyInfo hostProp = session.GetType().GetProperty("Host");
        if (hostProp != null)
        {
            object value = hostProp.GetValue(session);
            if (value != null)
                return value.ToString();
        }

        try
        {
            IHostSession hostSession = session.AsHost();
            if (hostSession != null)
            {
                PropertyInfo hostSessionHostProp = hostSession.GetType().GetProperty("Host");
                if (hostSessionHostProp != null)
                {
                    object value = hostSessionHostProp.GetValue(hostSession);
                    if (value != null)
                        return value.ToString();
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }
}