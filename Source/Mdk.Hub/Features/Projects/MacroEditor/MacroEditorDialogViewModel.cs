using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.MacroEditor;

/// <summary>
/// View model for the macro editor dialog.
/// </summary>
[ViewModelFor<MacroEditorDialogView>]
public partial class MacroEditorDialogViewModel : OverlayModel
{
    static readonly Regex MacroKeyPattern = GenerateMacroKeyRegex();

    readonly RelayCommand _cancelCommand;
    readonly RelayCommand<MacroEntryViewModel> _deleteMacroCommand;
    readonly RelayCommand _saveCommand;
    string _validationError = string.Empty;

    /// <summary>
    /// Initializes a new instance of the MacroEditorDialogViewModel class.
    /// </summary>
    /// <param name="message">Message containing initial macro values.</param>
    public MacroEditorDialogViewModel(MacroEditorDialogMessage message)
    {
        Message = message;
        _saveCommand = new RelayCommand(Save, CanSave);
        _cancelCommand = new RelayCommand(Cancel);
        _deleteMacroCommand = new RelayCommand<MacroEntryViewModel>(DeleteMacro!);

        // Load initial macros
        if (message.InitialMacros != null && message.InitialMacros.Count > 0)
        {
            foreach (var (key, value) in message.InitialMacros)
            {
                var entry = new MacroEntryViewModel
                {
                    Key = StripDelimiters(key),
                    Value = value
                };
                entry.PropertyChanged += OnEntryChanged;
                Macros.Add(entry);
            }
        }

        // Always ensure there's an empty row at the end
        EnsureEmptyRow();
    }

    /// <summary>
    /// Gets the message containing initial macro values.
    /// </summary>
    public MacroEditorDialogMessage Message { get; }

    /// <summary>
    /// Gets the result after user saves or cancels.
    /// </summary>
    public MacroEditorDialogResult? Result { get; private set; }

    /// <summary>
    /// Gets the collection of macro entries.
    /// </summary>
    public ObservableCollection<MacroEntryViewModel> Macros { get; } = new();

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
    /// Gets the command to delete a macro entry.
    /// </summary>
    public ICommand DeleteMacroCommand => _deleteMacroCommand;

    void DeleteMacro(MacroEntryViewModel entry)
    {
        if (entry.IsEmpty)
            return; // Don't delete the empty row

        entry.PropertyChanged -= OnEntryChanged;
        Macros.Remove(entry);
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
        if (Macros.Count == 0 || !Macros[^1].IsEmpty)
        {
            var emptyEntry = new MacroEntryViewModel();
            emptyEntry.PropertyChanged += OnEntryChanged;
            Macros.Add(emptyEntry);
        }
    }

    void ValidateAll()
    {
        ValidationError = string.Empty;

        var nonEmptyEntries = Macros.Where(m => !m.IsEmpty).ToList();

        // Check for empty keys
        var entryWithEmptyKey = nonEmptyEntries.FirstOrDefault(m => string.IsNullOrWhiteSpace(m.Key));
        if (entryWithEmptyKey != null)
        {
            ValidationError = "Macro keys cannot be empty.";
            return;
        }

        // Check for invalid key format
        var invalidEntry = nonEmptyEntries.FirstOrDefault(m => !MacroKeyPattern.IsMatch(m.Key));
        if (invalidEntry != null)
        {
            ValidationError = $"Invalid macro key '{invalidEntry.Key}'. Keys must start with a letter or underscore, followed by letters, numbers, or underscores.";
            return;
        }

        // Check for duplicate keys (case-insensitive)
        var duplicateKey = nonEmptyEntries
            .GroupBy(m => m.Key, System.StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1)
            ?.Key;

        if (duplicateKey != null)
        {
            ValidationError = $"Duplicate macro key: {duplicateKey}";
            return;
        }
    }

    bool CanSave() => string.IsNullOrEmpty(ValidationError);

    void Save()
    {
        ValidateAll();
        if (!CanSave())
            return;

        // Filter out empty entries and build result dictionary
        var nonEmptyEntries = Macros.Where(m => !m.IsEmpty).ToList();

        ImmutableDictionary<string, string>? resultDict = null;
        if (nonEmptyEntries.Count > 0)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var entry in nonEmptyEntries)
            {
                // Add $ delimiters when saving
                builder.Add(AddDelimiters(entry.Key), entry.Value);
            }
            resultDict = builder.ToImmutable();
        }

        Result = new MacroEditorDialogResult(resultDict);
        Dismiss();
    }

    void Cancel()
    {
        Result = null;
        Dismiss();
    }

    static string StripDelimiters(string key)
    {
        if (key.StartsWith('$') && key.EndsWith('$') && key.Length > 2)
            return key[1..^1];
        return key;
    }

    static string AddDelimiters(string key) => $"${key}$";

    [GeneratedRegex(@"^[A-Z_][A-Z0-9_]*$", RegexOptions.IgnoreCase)]
    private static partial Regex GenerateMacroKeyRegex();
}
