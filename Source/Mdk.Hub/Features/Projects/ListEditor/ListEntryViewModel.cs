using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.ListEditor;

/// <summary>
/// View model for a single list item in the editor.
/// </summary>
public class ListEntryViewModel : Model
{
    string _value = string.Empty;

    /// <summary>
    /// Gets or sets the item value.
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
    /// Gets whether this entry is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Gets or sets the watermark text for the value field.
    /// </summary>
    public string Watermark { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delete command.
    /// </summary>
    public ICommand? DeleteCommand { get; set; }
}
