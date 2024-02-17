using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine;

/// <summary>
///     Utilities for working with <see cref="Project" />.
/// </summary>
public static class ProjectExtensions
{
    /// <summary>
    ///     Attempts to get a document from the project with the specified path.
    /// </summary>
    /// <remarks>
    ///     Specify the path relative to the project file.
    /// </remarks>
    /// <param name="project"></param>
    /// <param name="documentPath"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public static bool TryGetDocument(this Project project, string documentPath, [MaybeNullWhen(false)] out TextDocument document)
    {
        var projectPath = Path.GetDirectoryName(project.FilePath);

        bool isMatch(TextDocument d)
        {
            return d.FilePath != null && string.Equals(projectPath == null ? d.FilePath : Path.GetRelativePath(projectPath, d.FilePath), documentPath, StringComparison.OrdinalIgnoreCase);
        }

        document = project.Documents.FirstOrDefault(isMatch) ?? project.AdditionalDocuments.FirstOrDefault(isMatch);
        return document != null;
    }

    /// <summary>
    ///     Attempts to get a document from the project with the specified file name.
    /// </summary>
    /// <remarks>
    ///     Do not specify the path, just the file name. Will return the first document with a matching file name
    /// </remarks>
    /// <param name="project"></param>
    /// <param name="fileName"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public static bool TryFindDocument(this Project project, string fileName, [MaybeNullWhen(false)] out TextDocument document)
    {
        bool isMatch(TextDocument d)
        {
            return d.FilePath != null && string.Equals(Path.GetFileName(d.FilePath), fileName, StringComparison.OrdinalIgnoreCase);
        }

        document = project.Documents.FirstOrDefault(isMatch) ?? project.AdditionalDocuments.FirstOrDefault(isMatch);
        return document != null;
    }
}