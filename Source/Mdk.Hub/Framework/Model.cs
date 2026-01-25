using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mdk.Hub.Framework;

/// <summary>
/// Base class for models that implement <see cref="INotifyPropertyChanged"/> for data binding.
/// Provides helper methods for property change notification.
/// </summary>
public abstract class Model : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the property to the specified value and raises <see cref="PropertyChanged"/> if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property (automatically provided by the compiler).</param>
    /// <returns><c>true</c> if the value was changed; otherwise, <c>false</c>.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the property to the specified value using a custom equality comparer and raises <see cref="PropertyChanged"/> if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="comparer">The equality comparer to use for comparison.</param>
    /// <param name="propertyName">The name of the property (automatically provided by the compiler).</param>
    /// <returns><c>true</c> if the value was changed; otherwise, <c>false</c>.</returns>
    protected bool SetProperty<T>(ref T field, T value, IEqualityComparer<T?> comparer, [CallerMemberName] string? propertyName = null)
    {
        if (!comparer.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed (automatically provided by the compiler).</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}