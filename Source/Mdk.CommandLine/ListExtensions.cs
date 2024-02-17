using System;
using System.Collections.Generic;

namespace Mdk.CommandLine;

/// <summary>
///     Extension methods for <see cref="List{T}" />.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    ///     Attempts to dequeue an item from the list, returning <c>false</c> if the list is empty.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryDequeue<T>(this List<T> list, out T item)
    {
        if (list.Count == 0)
        {
            item = default!;
            return false;
        }

        item = list[0];
        list.RemoveAt(0);
        return true;
    }

    /// <summary>
    ///     Dequeues an item from the list, throwing an exception if the list is empty.
    /// </summary>
    /// <param name="list"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Dequeue<T>(this List<T> list)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        var item = list[0];
        list.RemoveAt(0);
        return item;
    }
}