using System;

namespace Mdk.Hub.Features.Projects.ListEditor;

/// <summary>
/// Message to open the list editor dialog.
/// </summary>
/// <param name="Title">Dialog title.</param>
/// <param name="Description">Description explaining what is being edited.</param>
/// <param name="FieldLabel">Label for the input field.</param>
/// <param name="FieldWatermark">Watermark text for the input field.</param>
/// <param name="InitialItems">Initial list of items.</param>
/// <param name="ValidateItem">Optional validation function that returns error message or null if valid.</param>
public record ListEditorDialogMessage(
    string Title,
    string Description,
    string FieldLabel,
    string FieldWatermark,
    string[]? InitialItems,
    Func<string, string?>? ValidateItem = null);
