using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mdk.Notification.Windows;

/// <summary>
///     A base class for bindable models.
/// </summary>
public abstract class Model : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Raises the PropertyChanged event.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///     Sets the field and raises the PropertyChanged event if the value has changed.
    /// </summary>
    protected bool SetField<T>(ref T field, T value, IEqualityComparer<T>? comparer = null, [CallerMemberName] string? propertyName = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        if (comparer.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}