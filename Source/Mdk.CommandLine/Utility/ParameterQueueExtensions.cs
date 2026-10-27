using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Mdk.CommandLine.Utility;

public static class ParameterQueueExtensions
{
    public static bool TryDequeue(this Queue<string> queue, string required)
    {
        if (queue.Count == 0)
            return false;
        var value = queue.Peek();
        if (!string.Equals(value, required, StringComparison.OrdinalIgnoreCase))
            return false;
        queue.Dequeue();
        return true;
    }
    
    public static bool TryDequeue<T>(this Queue<string> queue, [MaybeNullWhen(false)] out T value)
    {
        if (queue.Count == 0)
        {
            value = default;
            return false;
        }
        var typeCode = Type.GetTypeCode(typeof(T));
        switch (typeCode)
        {
            case TypeCode.String:
                value = (T)(object)queue.Dequeue();
                return true;
            case TypeCode.Int32:
                if (!typeof(T).IsEnum)
                    throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
                var token = queue.Dequeue();
                if (Enum.TryParse(typeof(T), token, true, out var result) && IsKnownEnumValue(typeof(T), token, result))
                {
                    value = (T)result;
                    return true;
                }
                value = default;
                return false;
            case TypeCode.Int64:
                if (long.TryParse(queue.Dequeue(), NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                {
                    value = (T)(object)longValue;
                    return true;
                }
                value = default;
                return false;
            case TypeCode.Single:
                if (float.TryParse(queue.Dequeue(), NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                {
                    value = (T)(object)floatValue;
                    return true;
                }
                value = default;
                return false;
            case TypeCode.Double:
                if (double.TryParse(queue.Dequeue(), NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    value = (T)(object)doubleValue;
                    return true;
                }
                value = default;
                return false;
            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    ///     Determines whether a parsed enum value is one the enum defines: a single named value, or for a
    ///     <see cref="FlagsAttribute" /> enum, a combination of its named flags.
    /// </summary>
    static bool IsKnownEnumValue(Type enumType, string token, object value)
    {
        if (!enumType.IsDefined(typeof(FlagsAttribute), false))
            return !token.Contains(',') && Enum.IsDefined(enumType, value);

        var definedBits = Enum.GetValues(enumType).Cast<object>().Aggregate(0L, (combined, flag) => combined | Convert.ToInt64(flag));
        var bits = Convert.ToInt64(value);
        return bits >= 0 && (bits & ~definedBits) == 0;
    }
}