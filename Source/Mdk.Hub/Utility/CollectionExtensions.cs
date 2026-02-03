using System;
using System.Collections.Generic;

namespace Mdk.Hub.Utility;

/// <summary>
///     Extension methods for collection types.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    ///     Searches for an element that matches the conditions defined by the specified predicate,
    ///     starting at the specified index and searching forward to the end of the collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="match">The predicate that defines the conditions of the element to search for.</param>
    /// <returns>
    ///     The zero-based index of the first occurrence of an element that matches the conditions
    ///     defined by <paramref name="match"/>, if found; otherwise, -1.
    /// </returns>
    public static int FindIndex<T>(this IReadOnlyList<T> list, int startIndex, Predicate<T> match)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        if (startIndex < 0 || startIndex > list.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        for (int i = startIndex; i < list.Count; i++)
        {
            if (match(list[i]))
                return i;
        }

        return -1;
    }
}

