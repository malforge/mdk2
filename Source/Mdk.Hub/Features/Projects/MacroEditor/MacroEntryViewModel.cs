using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.MacroEditor;

/// <summary>
/// View model for a single macro key-value pair in the editor.
/// </summary>
public class MacroEntryViewModel : Model
{
    string _key = string.Empty;
    string _value = string.Empty;

    /// <summary>
    /// Gets or sets the macro key (without $ delimiters).
    /// </summary>
    public string Key
    {
        get => _key;
        set
        {
            if (SetProperty(ref _key, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

    /// <summary>
    /// Gets or sets the macro value.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

    /// <summary>
    /// Gets whether this entry is empty (both key and value are empty).
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Gets or sets the delete command.
    /// </summary>
    public ICommand? DeleteCommand { get; set; }
}
