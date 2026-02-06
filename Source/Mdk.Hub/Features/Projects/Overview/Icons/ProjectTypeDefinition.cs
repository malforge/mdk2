using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview.Icons;

/// <summary>
///     Maps a project type to its visual icon template.
/// </summary>
public class ProjectTypeDefinition
{
    /// <summary>
    ///     Gets or sets the project type this definition applies to.
    /// </summary>
    public ProjectType Type { get; set; }

    /// <summary>
    ///     Gets or sets the data template used to render the icon for this project type.
    /// </summary>
    [Content]
    public IDataTemplate? Template { get; set; }
}
