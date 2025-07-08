using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Malforge.Mdk2.Setup.Foundation;

public abstract class Model : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) => SetField(ref field, value, EqualityComparer<T>.Default, propertyName);

    protected void SetField<T>(ref T field, T value, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
    {
        if (comparer.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public abstract class InstallerStep(string name) : Model
{
    string? _currentOperation;
    float? _progress;

    public string Name { get; } = name;

    public string? CurrentOperation
    {
        get => _currentOperation;
        set => SetField(ref _currentOperation, value);
    }

    public float? Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }
    
    public abstract Task RunAsync(CancellationToken cancellationToken = default);
}