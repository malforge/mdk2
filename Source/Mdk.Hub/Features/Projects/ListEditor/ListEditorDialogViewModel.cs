using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.ListEditor;

/// <summary>
/// View model for the list editor dialog.
/// </summary>
[ViewModelFor<ListEditorDialogView>]
public class ListEditorDialogViewModel : OverlayModel
{
    readonly RelayCommand _cancelCommand;
    readonly RelayCommand<ListEntryViewModel> _deleteItemCommand;
    readonly RelayCommand _saveCommand;
    string _validationError = string.Empty;

    /// <summary>
    /// Initializes a new instance of the ListEditorDialogViewModel class.
    /// </summary>
    /// <param name="message">Message containing dialog configuration and initial items.</param>
    public ListEditorDialogViewModel(ListEditorDialogMessage message)
    {
        Message = message;
        _saveCommand = new RelayCommand(Save, CanSave);
        _cancelCommand = new RelayCommand(Cancel);
        _deleteItemCommand = new RelayCommand<ListEntryViewModel>(DeleteItem!);

        // Load initial items
        if (message.InitialItems != null && message.InitialItems.Length > 0)
        {
            foreach (var item in message.InitialItems)
            {
                var entry = new ListEntryViewModel 
                { 
                    Value = item,
                    Watermark = message.FieldWatermark,
                    DeleteCommand = _deleteItemCommand
                };
                entry.PropertyChanged += OnEntryChanged;
                Items.Add(entry);
            }
        }

        // Always ensure there's an empty row at the end
        EnsureEmptyRow();
    }

    /// <summary>
    /// Gets the message containing dialog configuration.
    /// </summary>
    public ListEditorDialogMessage Message { get; }

    /// <summary>
    /// Gets the result after user saves or cancels.
    /// </summary>
    public ListEditorDialogResult? Result { get; private set; }

    /// <summary>
    /// Gets the collection of list items.
    /// </summary>
    public ObservableCollection<ListEntryViewModel> Items { get; } = new();

    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    /// <summary>
    /// Gets the command to save changes.
    /// </summary>
    public ICommand SaveCommand => _saveCommand;

    /// <summary>
    /// Gets the command to cancel editing.
    /// </summary>
    public ICommand CancelCommand => _cancelCommand;

    /// <summary>
    /// Gets the command to delete an item.
    /// </summary>
    public ICommand DeleteItemCommand => _deleteItemCommand;

    void DeleteItem(ListEntryViewModel entry)
    {
        if (entry.IsEmpty)
            return; // Don't delete the empty row

        entry.PropertyChanged -= OnEntryChanged;
        Items.Remove(entry);
        ValidateAll();
        _saveCommand.NotifyCanExecuteChanged();
    }

    void OnEntryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When user types in the empty row, ensure there's a new empty row
        EnsureEmptyRow();
        ValidateAll();
        _saveCommand.NotifyCanExecuteChanged();
    }

    void EnsureEmptyRow()
    {
        // Check if there's already an empty row at the end
        if (Items.Count == 0 || !Items[^1].IsEmpty)
        {
            var emptyEntry = new ListEntryViewModel
            {
                Watermark = Message.FieldWatermark,
                DeleteCommand = _deleteItemCommand
            };
            emptyEntry.PropertyChanged += OnEntryChanged;
            Items.Add(emptyEntry);
        }
    }

    void ValidateAll()
    {
        ValidationError = string.Empty;

        var nonEmptyEntries = Items.Where(m => !m.IsEmpty).ToList();

        // Check for empty values (shouldn't happen due to IsEmpty check, but be safe)
        if (nonEmptyEntries.Any(e => string.IsNullOrWhiteSpace(e.Value)))
        {
            ValidationError = "Items cannot be empty.";
            return;
        }

        // Custom validation if provided
        if (Message.ValidateItem != null)
        {
            foreach (var entry in nonEmptyEntries)
            {
                var error = Message.ValidateItem(entry.Value);
                if (error != null)
                {
                    ValidationError = error;
                    return;
                }
            }
        }

        // Check for duplicates (case-insensitive)
        var duplicateValue = nonEmptyEntries
            .GroupBy(e => e.Value, System.StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1)
            ?.Key;

        if (duplicateValue != null)
        {
            ValidationError = $"Duplicate item: {duplicateValue}";
            return;
        }
    }

    bool CanSave() => string.IsNullOrEmpty(ValidationError);

    void Save()
    {
        ValidateAll();
        if (!CanSave())
            return;

        // Filter out empty entries and build result array
        var nonEmptyEntries = Items.Where(e => !e.IsEmpty).Select(e => e.Value).ToArray();

        Result = new ListEditorDialogResult(nonEmptyEntries.Length > 0 ? nonEmptyEntries : null);
        Dismiss();
    }

    void Cancel()
    {
        Result = null;
        Dismiss();
    }
}
