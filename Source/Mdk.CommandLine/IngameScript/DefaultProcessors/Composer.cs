using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     The default script composer.
/// </summary>
public class Composer : IScriptComposer
{
    /// <inheritdoc />
    public async Task<StringBuilder> ComposeAsync(Document document, IConsole console, ScriptProjectMetadata metadata)
    {
        var builder = new StringBuilder();
        if (await document.GetSyntaxRootAsync() is not CompilationUnitSyntax root)
            throw new InvalidOperationException("Failed to get the syntax root");

        var programClass = root.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == "Program");
        if (programClass == null)
            throw new InvalidOperationException("Failed to find the Program class");

        WriteProgramClass(root, programClass, builder, metadata);
        WriteEverythingElse(root, programClass, builder);

        return builder.Replace("\r\n", "\n");
    }

    static void WriteProgramClass(CompilationUnitSyntax root, ClassDeclarationSyntax programClass, StringBuilder builder, ScriptProjectMetadata metadata)
    {
        var contentSpan = new TextSpan(programClass.OpenBraceToken.SpanStart, programClass.CloseBraceToken.Span.End - programClass.OpenBraceToken.SpanStart);
        var programLines = root.GetText().GetSubText(contentSpan).Lines.Select(l => l.ToString()).ToList();
        DeleteLeadingBrace(programLines);
        DeleteTrailingBrace(programLines);

        var tmpBuffer = new StringBuilder();
        foreach (var line in programLines)
            tmpBuffer.Append(line).Append('\n');
        tmpBuffer.Unindent(metadata.IndentSize);
        builder.Append(tmpBuffer);
    }

    static void WriteEverythingElse(CompilationUnitSyntax root, ClassDeclarationSyntax programClass, StringBuilder builder)
    {
        var everythingElse = root.Members.Where(m => m != programClass).ToList();
        if (everythingElse.Count > 0)
        {
            builder.Append("}\n");
            foreach (var member in everythingElse)
            {
                builder.Append(member.ToFullString());
            }
        }
        
        for (var i = builder.Length - 1; i >= 0; i--)
        {
            if (builder[i] == '}')
            {
                builder.Length = i;
                break;
            }
            if (!char.IsWhiteSpace(builder[i]))
                break;
        }
    }

    static void DeleteTrailingBrace(List<string> programLines)
    {
        if (programLines.Count == 0)
            return;
        var lastLine = programLines[^1];
        var braceCount = 0;
        var deleteFrom = lastLine.Length - 1;
        for (var i = lastLine.Length - 1; i >= 0; i--)
        {
            if (lastLine[i] == '}')
            {
                if (braceCount == 0)
                {
                    deleteFrom--;
                    braceCount++;
                    continue;
                }
                break;
            }
            if (!char.IsWhiteSpace(lastLine[i]))
                break;
            deleteFrom--;
        }
        if (deleteFrom < lastLine.Length - 1)
        {
            lastLine = lastLine.Substring(0, deleteFrom + 1);
            if (string.IsNullOrWhiteSpace(lastLine))
                programLines.RemoveAt(programLines.Count - 1);
            else
                programLines[^1] = lastLine;
        }
    }

    static void DeleteLeadingBrace(List<string> programLines)
    {
        if (programLines.Count == 0)
            return;
        var firstLine = programLines[0];
        var braceCount = 0;
        var deleteTo = 0;
        for (var i = 0; i < firstLine.Length; i++)
        {
            if (firstLine[i] == '{')
            {
                if (braceCount == 0)
                {
                    deleteTo++;
                    braceCount++;
                    continue;
                }
                break;
            }
            if (!char.IsWhiteSpace(firstLine[i]))
                break;
            deleteTo++;
        }
        if (deleteTo > 0)
        {
            firstLine = firstLine.Substring(deleteTo);
            if (string.IsNullOrWhiteSpace(firstLine))
                programLines.RemoveAt(0);
            else
                programLines[0] = firstLine;
        }
    }
}