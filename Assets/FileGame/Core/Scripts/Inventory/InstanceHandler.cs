using System.Collections.Generic;

public static class InstanceHandler
{
    private static Dictionary<System.Type, object> instances = new Dictionary<System.Type, object>();

    public static void RegisterInstance<T>(T instance)
    {
        instances[typeof(T)] = instance;
    }

    public static void UnregisterInstance<T>()
    {
        instances.Remove(typeof(T));
    }

    public static bool TryGetInstance<T>(out T instance)
    {
        if (instances.TryGetValue(typeof(T), out object obj))
        {
            instance = (T)obj;
            return true;
        }

        instance = default;
        return false;
    }
}