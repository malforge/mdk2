using System.Collections.Immutable;

namespace Mdk.Hub.Features.Projects.MacroEditor;

/// <summary>
/// Message to open the macro editor dialog.
/// </summary>
public record MacroEditorDialogMessage(ImmutableDictionary<string, string>? InitialMacros);
