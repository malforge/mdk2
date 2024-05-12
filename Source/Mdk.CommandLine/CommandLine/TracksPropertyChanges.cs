using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mdk.CommandLine.CommandLine;

/// <summary>
///     A base class for objects that track which properties have been set.
/// </summary>
public abstract class TracksPropertyChanges
{
    readonly HashSet<string> _setProperties = new();
    
    /// <summary>
    ///     Determines if the specified property has been set.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static bool IsSet(TracksPropertyChanges obj, string propertyName) => obj._setProperties.Contains(propertyName);
    
    /// <summary>
    ///     Unsets the specified property. Does not actually change the value of the property, just marks it as unset.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    public static void Unset(TracksPropertyChanges obj, string propertyName) => obj._setProperties.Remove(propertyName);
    
    /// <summary>
    ///     Sets the specified property to the specified value. Marks the property as set.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <param name="propertyName"></param>
    /// <typeparam name="T"></typeparam>
    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        _setProperties.Add(propertyName!);
    }
}