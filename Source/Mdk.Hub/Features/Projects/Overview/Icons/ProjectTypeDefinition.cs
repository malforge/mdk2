using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview.Icons;

public class ProjectTypeDefinition
{
    public ProjectType Type { get; set; }

    [Content]
    public IDataTemplate? Template { get; set; }
}