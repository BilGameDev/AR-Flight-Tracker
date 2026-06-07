using System;
using System.Collections.Generic;

namespace Viridian.Utils
{
public static class EnumUtility
{
    private static readonly Dictionary<Type, Array> _cache = new();

    public static bool TryParse<T>(string value, bool ignoreCase, out T result) where T : Enum
    {
        var type = typeof(T);
        if (!_cache.TryGetValue(type, out Array values))
        {
            values = Enum.GetValues(type);
            _cache[type] = values;
        }

        foreach (var val in values)
        {
            if (string.Equals(val.ToString(), value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                result = (T)val;
                return true;
            }
        }

        result = default;
        return false;
    }
    
    public static T GetRandomEnum<T>() where T : Enum
    {
        var type = typeof(T);
        if (!_cache.TryGetValue(type, out Array values))
        {
            values = Enum.GetValues(type);
            _cache[type] = values;
        }
        
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }
}
}