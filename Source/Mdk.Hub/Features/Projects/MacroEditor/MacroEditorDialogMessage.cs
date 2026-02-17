using System.Collections.Immutable;

namespace Mdk.Hub.Features.Projects.MacroEditor;

/// <summary>
/// Message to open the macro editor dialog.
/// </summary>
/// <param name="Description">Description explaining what is being edited.</param>
/// <param name="InitialMacros">Initial macro dictionary.</param>
public record MacroEditorDialogMessage(
    string Description,
    ImmutableDictionary<string, string>? InitialMacros);
