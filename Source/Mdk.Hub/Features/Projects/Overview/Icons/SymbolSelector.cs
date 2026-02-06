using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview.Icons;

/// <summary>
///     Data template selector that chooses the appropriate icon template based on project type.
/// </summary>
public class SymbolSelector : IDataTemplate
{
    /// <summary>
    ///     Gets the list of project type definitions that map project types to templates.
    /// </summary>
    [Content]
    public List<ProjectTypeDefinition> Definitions { get; } = new();

    /// <summary>
    ///     Builds a control for the specified project type.
    /// </summary>
    /// <param name="param">The project type to build a control for.</param>
    /// <returns>A control representing the project type icon, or null if not found.</returns>
    public Control? Build(object? param)
    {
        if (param is not ProjectType projectType)
            return null;

        foreach (var def in Definitions)
        {
            if (def.Type == projectType)
                return def.Template?.Build(param);
        }

        return null;
    }

    /// <summary>
    ///     Determines whether this template can handle the specified data.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a ProjectType.</returns>
    public bool Match(object? data) => data is ProjectType;
}
