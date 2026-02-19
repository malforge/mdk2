using System;
using System.Collections.Generic;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Registry that maps file extensions to editor ViewModel types.
/// </summary>
[Singleton]
public class FileEditorRegistry
{
    readonly Dictionary<string, Type> _editorTypes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileEditorRegistry"/> class.
    /// </summary>
    public FileEditorRegistry()
    {
        // Register file type handlers
        RegisterEditor(".mdknodes", typeof(NodeScript.NodeScriptEditorViewModel));
        
        // Future editors can be registered here:
        // RegisterEditor(".mdkconfig", typeof(ConfigEditorViewModel));
        // RegisterEditor(".mdktemplate", typeof(TemplateEditorViewModel));
    }

    /// <summary>
    ///     Registers an editor type for a specific file extension.
    /// </summary>
    /// <param name="extension">File extension (e.g., ".mdknodes").</param>
    /// <param name="editorType">ViewModel type that implements IFileEditor.</param>
    public void RegisterEditor(string extension, Type editorType)
    {
        if (!typeof(IFileEditor).IsAssignableFrom(editorType) || !typeof(ViewModel).IsAssignableFrom(editorType))
            throw new ArgumentException($"Editor type must implement IFileEditor and extend ViewModel: {editorType.Name}");

        _editorTypes[extension] = editorType;
    }

    /// <summary>
    ///     Gets the editor type for a file path based on its extension.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Editor ViewModel type, or null if no editor is registered for this extension.</returns>
    public Type? GetEditorType(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath);
        return string.IsNullOrEmpty(extension) ? null : _editorTypes.GetValueOrDefault(extension);
    }

    /// <summary>
    ///     Checks if an editor is registered for the given file path.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>True if an editor is registered for this file type.</returns>
    public bool HasEditorFor(string filePath)
    {
        return GetEditorType(filePath) != null;
    }
}
