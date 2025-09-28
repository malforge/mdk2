using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview;

public class TemplateSelector : IDataTemplate
{
    [Content]
    public List<TypeMap> Definitions { get; } = new();

    public Control? Build(object? param)
    {
        var type = param?.GetType();
        if (type == null || !typeof(ProjectListItem).IsAssignableFrom(type))
            return null;

        foreach (var def in Definitions)
        {
            if (def.Type == type)
                return def.Template?.Build(param);
        }

        return null;
    }

    public bool Match(object? data) => data is ProjectType;
}