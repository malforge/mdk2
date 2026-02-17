using System.Collections.Immutable;

namespace Mdk.Hub.Features.Projects.MacroEditor;

/// <summary>
/// Result from the macro editor dialog.
/// </summary>
public record MacroEditorDialogResult(ImmutableDictionary<string, string>? Macros);
