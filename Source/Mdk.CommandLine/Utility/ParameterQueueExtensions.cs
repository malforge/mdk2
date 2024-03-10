using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
                if (int.TryParse(queue.Dequeue(), NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
                {
                    value = (T)(object)intValue;
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
                if (!typeof(T).IsEnum)
                    throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
                if (Enum.TryParse(typeof(T), queue.Dequeue(), true, out var result))
                {
                    value = (T)result;
                    return true;
                }
                value = default;
                return false;
        }
    }
}