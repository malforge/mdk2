namespace Mdk.Hub.Utility;

/// <summary>
///     Provides a set of static methods for string operations that take into account human-centric factors such as
///     ignoring case and trimming leading/trailing whitespace.
/// </summary>
public static class HumanStringExtensions
{
    /// <summary>
    ///     Compares two strings for equality, taking into account human factors such as trimming whitespace and ignoring case
    ///     differences.
    /// </summary>
    /// <param name="str1">The first string to compare.</param>
    /// <param name="str2">The second string to compare.</param>
    /// <param name="ignoreCase">
    ///     A value indicating whether to ignore case differences during comparison. Default is
    ///     <c>true</c>.
    /// </param>
    /// <returns><c>true</c> if the strings are considered equal based on the specified comparison; otherwise, <c>false</c>.</returns>
    public static bool EqualsWhileHumanAware(this string? str1, string? str2, bool ignoreCase = true)
    {
        if (str1 is null && str2 is null)
            return true;
        if (str1 is null || str2 is null)
            return false;
        var ptr1 = new TextPtr(str1).TrimForward();
        var ptr2 = new TextPtr(str2).TrimForward();
        while (!ptr1.IsOutOfBounds() && !ptr2.IsOutOfBounds())
        {
            if (ignoreCase)
            {
                if (char.ToUpperInvariant(ptr1[0]) != char.ToUpperInvariant(ptr2[0]))
                    return false;
            }
            else
            {
                if (ptr1[0] != ptr2[0])
                    return false;
            }
            ptr1 = (ptr1 + 1).TrimForward();
            ptr2 = (ptr2 + 1).TrimForward();
        }
        return true;
    }
}
