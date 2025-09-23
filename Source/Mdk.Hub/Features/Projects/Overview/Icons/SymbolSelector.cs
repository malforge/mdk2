using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview.Icons;

public class SymbolSelector : IDataTemplate
{
    [Content]
    public List<ProjectTypeDefinition> Definitions { get; } = new();

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

    public bool Match(object? data) => data is ProjectType;
}