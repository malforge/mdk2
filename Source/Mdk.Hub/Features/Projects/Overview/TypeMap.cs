using System;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
/// Represents a mapping between a Type and its corresponding data template.
/// </summary>
public class TypeMap
{
    /// <summary>
    /// Gets or sets the CLR type associated with this mapping.
    /// </summary>
    /// <remarks>
    /// This property is used to specify the type that corresponds to the data template in the mapping.
    /// It is typically used in conjunction with <see cref="Template"/> to define a template for a specific type.
    /// </remarks>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets the data template associated with this mapping.
    /// </summary>
    /// <remarks>
    /// This property represents the template that is used to render data of the specified type.
    /// It is typically defined alongside <see cref="Type"/> to associate a specific template with a corresponding type.
    /// </remarks>
    [Content]
    public IDataTemplate? Template { get; set; }
}