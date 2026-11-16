using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jednoduchá cache pro Resources.Load, aby se stejné sprity/zvuky během hry nehledaly opakovaně.
/// </summary>
public static class ResourceCache
{
    private static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => cache.Clear();

    public static T Load<T>(string path) where T : Object
    {
        if (cache.TryGetValue(path, out var obj) && obj != null)
            return obj as T;

        var loaded = Resources.Load<T>(path);
        if (loaded != null) cache[path] = loaded;
        return loaded;
    }
}
