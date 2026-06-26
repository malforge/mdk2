using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed class AttributeTrimmingProcessor : IProjectProcessor
{
    public async Task<Project> ProcessAsync(Project project, IPackContext context, CancellationToken cancellationToken = default)
    {
        var result = await ProcessWithResultAsync(project, context, cancellationToken);
        if (result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            throw new CommandLineException(-2, "Attribute trimming failed.");

        return result.Project;
    }

    public async Task<AttributeTrimmingResult> ProcessWithResultAsync(Project project, IPackContext context, CancellationToken cancellationToken = default)
    {
        context.Console.Trace("Analyzing attributes for trimming");
        var compilation = await project.GetCompilationAsync(cancellationToken) as CSharpCompilation
                          ?? throw new CommandLineException(-1, "Failed to compile the project for attribute trimming.");

        var systemAttribute = compilation.GetTypeByMetadataName("System.Attribute")
                              ?? throw new CommandLineException(-1, "Failed to resolve System.Attribute for attribute trimming.");

        var plan = await AnalyzeAsync(project, compilation, systemAttribute, cancellationToken);
        if (plan.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            return await CompleteAsync(project, plan, context);

        if (!HasTrimmedNodes(plan))
            return await CompleteAsync(project, plan, context);

        context.Console.Trace("Trimming source-defined attributes");
        var usingDirectives = await FindUsingDirectivesToRemoveAsync(project, plan.SourceDefinedAttributeTypes, cancellationToken);
        project = await RewriteAsync(project, plan, usingDirectives, cancellationToken);

        var validationDiagnostics = await ValidateAsync(project, cancellationToken);
        if (validationDiagnostics.Length > 0)
            plan = plan with { Diagnostics = plan.Diagnostics.AddRange(validationDiagnostics) };

        context.Console.Trace($"Trimmed {plan.Report.TrimmedApplications.Length} attribute applications and {plan.Report.TrimmedDeclarations.Length} attribute declarations");
        return await CompleteAsync(project, plan, context);
    }

    static bool HasTrimmedNodes(AttributeTrimmingPlan plan)
    {
        return plan.AttributeApplications.Values.Any(spans => spans.Count > 0)
               || plan.AttributeDeclarations.Values.Any(spans => spans.Count > 0);
    }

    async Task<AttributeTrimmingPlan> AnalyzeAsync(Project project, CSharpCompilation compilation, INamedTypeSymbol systemAttribute, CancellationToken cancellationToken)
    {
        var applicationSpans = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableHashSet<TextSpan>>();
        var declarationSpans = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableHashSet<TextSpan>>();
        var sourceAttributeTypes = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(AttributeTrimmingPlan.SymbolComparer);
        var diagnostics = ImmutableArray.CreateBuilder<AttributeTrimmingDiagnostic>();
        var trimmedApplications = ImmutableArray.CreateBuilder<AttributeTrimmingReportEntry>();
        var trimmedDeclarations = ImmutableArray.CreateBuilder<AttributeTrimmingReportEntry>();
        var preservedApplications = ImmutableArray.CreateBuilder<AttributeTrimmingReportEntry>();

        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var model = await document.GetSemanticModelAsync(cancellationToken);
            if (root == null || model == null)
                continue;

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var symbol = model.GetDeclaredSymbol(classDeclaration, cancellationToken);
                if (symbol == null || !IsAttributeType(symbol, systemAttribute))
                    continue;

                sourceAttributeTypes.Add(symbol);
                AddSpan(declarationSpans, document.Id, classDeclaration.Span);
                trimmedDeclarations.Add(CreateEntry(symbol, document, classDeclaration.Identifier.GetLocation(), "SourceDefined"));
            }
        }

        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var model = await document.GetSemanticModelAsync(cancellationToken);
            if (root == null || model == null)
                continue;

            foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var attributeType = ResolveAttributeType(model, attribute, cancellationToken);
                if (attributeType == null)
                    continue;

                if (sourceAttributeTypes.Contains(attributeType))
                {
                    AddSpan(applicationSpans, document.Id, attribute.Span);
                    trimmedApplications.Add(CreateEntry(attributeType, document, attribute.GetLocation(), "SourceDefined"));
                    continue;
                }

                preservedApplications.Add(CreateEntry(attributeType, document, attribute.GetLocation(), "External"));
            }
        }

        diagnostics.AddRange(await FindRuntimeReferenceDiagnosticsAsync(project, sourceAttributeTypes.ToImmutable(), applicationSpans.ToImmutableDictionary(), declarationSpans.ToImmutableDictionary(), cancellationToken));

        return new AttributeTrimmingPlan(
            applicationSpans.ToImmutableDictionary(),
            declarationSpans.ToImmutableDictionary(),
            sourceAttributeTypes.ToImmutable(),
            diagnostics.ToImmutable(),
            new AttributeTrimmingReport(trimmedApplications.ToImmutable(), trimmedDeclarations.ToImmutable(), preservedApplications.ToImmutable()));
    }

    async Task<Project> RewriteAsync(
        Project project,
        AttributeTrimmingPlan plan,
        ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>> usingDirectives,
        CancellationToken cancellationToken)
    {
        var affectedDocumentIds = plan.AttributeApplications.Keys
            .Concat(plan.AttributeDeclarations.Keys)
            .Concat(usingDirectives.Keys)
            .Distinct();

        foreach (var documentId in affectedDocumentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = project.GetDocument(documentId);
            var root = await document?.GetSyntaxRootAsync(cancellationToken)!;
            if (document == null || root == null)
                continue;

            var applicationSpans = plan.AttributeApplications.TryGetValue(documentId, out var applications) ? applications : ImmutableHashSet<TextSpan>.Empty;
            var declarationSpans = plan.AttributeDeclarations.TryGetValue(documentId, out var declarations) ? declarations : ImmutableHashSet<TextSpan>.Empty;
            var usingSpans = usingDirectives.TryGetValue(documentId, out var usings) ? usings : ImmutableHashSet<TextSpan>.Empty;
            var newRoot = new AttributeTrimmingRewriter(applicationSpans, declarationSpans, usingSpans).Rewrite(root);

            project = document.WithSyntaxRoot(newRoot).Project;
        }

        return project;
    }

    async Task<ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>>> FindUsingDirectivesToRemoveAsync(
        Project project,
        ImmutableHashSet<INamedTypeSymbol> sourceAttributeTypes,
        CancellationToken cancellationToken)
    {
        var usingSpans = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableHashSet<TextSpan>>();
        if (sourceAttributeTypes.Count == 0)
            return usingSpans.ToImmutable();

        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var model = await document.GetSemanticModelAsync(cancellationToken);
            if (root == null || model == null)
                continue;

            foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = ResolveUsingTarget(model, usingDirective, cancellationToken);
                var shouldRemove = target is INamedTypeSymbol namedType
                                   && sourceAttributeTypes.Contains(namedType);

                if (shouldRemove)
                    AddSpan(usingSpans, document.Id, usingDirective.Span);
            }
        }

        return usingSpans.ToImmutable();
    }

    static ISymbol? ResolveUsingTarget(SemanticModel model, UsingDirectiveSyntax usingDirective, CancellationToken cancellationToken)
    {
        if (usingDirective.Name == null)
            return null;

        var info = model.GetSymbolInfo(usingDirective.Name, cancellationToken);
        var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        return symbol is IAliasSymbol alias ? alias.Target : symbol;
    }

    async Task<ImmutableArray<AttributeTrimmingDiagnostic>> FindRuntimeReferenceDiagnosticsAsync(
        Project project,
        ImmutableHashSet<INamedTypeSymbol> sourceAttributeTypes,
        ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>> applicationSpans,
        ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>> declarationSpans,
        CancellationToken cancellationToken)
    {
        if (sourceAttributeTypes.Count == 0)
            return ImmutableArray<AttributeTrimmingDiagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<AttributeTrimmingDiagnostic>();
        var reportedSpans = new HashSet<(DocumentId DocumentId, TextSpan Span)>();

        foreach (var document in project.Documents)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var model = await document.GetSemanticModelAsync(cancellationToken);
            if (root == null || model == null)
                continue;

            var trimmedApplications = applicationSpans.TryGetValue(document.Id, out var applications) ? applications : ImmutableHashSet<TextSpan>.Empty;
            var trimmedDeclarations = declarationSpans.TryGetValue(document.Id, out var declarations) ? declarations : ImmutableHashSet<TextSpan>.Empty;

            foreach (var name in root.DescendantNodes().OfType<NameSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var referencedType = ResolveReferencedAttributeType(model, name, cancellationToken);
                if (referencedType == null || !sourceAttributeTypes.Contains(referencedType))
                    continue;

                if (IsAllowedToolingReference(name, trimmedApplications, trimmedDeclarations))
                    continue;

                if (!reportedSpans.Add((document.Id, name.Span)))
                    continue;

                diagnostics.Add(new AttributeTrimmingDiagnostic(
                    "MDK04",
                    $"Tooling-only attribute type '{referencedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' is used by runtime code. Attribute trimming removes this type from packed source.",
                    name.GetLocation()));
            }
        }

        return diagnostics.ToImmutable();
    }

    static async Task<ImmutableArray<AttributeTrimmingDiagnostic>> ValidateAsync(Project project, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
            return [new AttributeTrimmingDiagnostic("MDK04", "Attribute trimming could not validate the transformed project.", null)];

        return [
            ..compilation.GetDiagnostics(cancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => new AttributeTrimmingDiagnostic("MDK04", $"Attribute trimming produced invalid source: {diagnostic}", diagnostic.Location))
        ];
    }

    async Task<AttributeTrimmingResult> CompleteAsync(Project project, AttributeTrimmingPlan plan, IPackContext context)
    {
        if (context.Console.TraceEnabled)
            await context.FileSystem.WriteTraceAsync("attribute-trimming.json", JsonSerializer.Serialize(plan.Report, new JsonSerializerOptions { WriteIndented = true }));

        return new AttributeTrimmingResult(project, plan.Diagnostics, plan.Report);
    }

    static bool IsAttributeType(INamedTypeSymbol symbol, INamedTypeSymbol systemAttribute)
    {
        for (var current = symbol; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, systemAttribute))
                return true;
        }

        return false;
    }

    static INamedTypeSymbol? ResolveAttributeType(SemanticModel model, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var info = model.GetSymbolInfo(attribute, cancellationToken);
        if (info.Symbol is IMethodSymbol methodSymbol)
            return methodSymbol.ContainingType;

        if (info.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Error })
            return null;

        if (info.Symbol is INamedTypeSymbol namedTypeSymbol)
            return namedTypeSymbol;

        foreach (var candidate in info.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod)
                return candidateMethod.ContainingType;
            if (candidate is INamedTypeSymbol candidateType)
                return candidateType;
        }

        var typeInfo = model.GetTypeInfo(attribute, cancellationToken);
        return typeInfo.Type is INamedTypeSymbol { TypeKind: not TypeKind.Error } typeSymbol ? typeSymbol : null;
    }

    static INamedTypeSymbol? ResolveReferencedAttributeType(SemanticModel model, NameSyntax name, CancellationToken cancellationToken)
    {
        var info = model.GetSymbolInfo(name, cancellationToken);
        var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        if (symbol is IAliasSymbol aliasSymbol)
            symbol = aliasSymbol.Target;

        return symbol switch
        {
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
            IMethodSymbol { MethodKind: MethodKind.Constructor } methodSymbol => methodSymbol.ContainingType,
            { ContainingType: INamedTypeSymbol containingType } => containingType,
            _ => null
        };
    }

    static bool IsAllowedToolingReference(SyntaxNode node, ImmutableHashSet<TextSpan> trimmedApplications, ImmutableHashSet<TextSpan> trimmedDeclarations)
    {
        if (node.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().Any())
            return true;

        if (node.AncestorsAndSelf().OfType<AttributeSyntax>().Any(attribute => trimmedApplications.Contains(attribute.Span)))
            return true;

        if (node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(declaration => trimmedDeclarations.Contains(declaration.Span)))
            return true;

        return false;
    }

    static AttributeTrimmingReportEntry CreateEntry(ISymbol symbol, Document document, Location location, string reason)
    {
        var lineSpan = location.GetLineSpan();
        return new AttributeTrimmingReportEntry(
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            document.FilePath,
            lineSpan.StartLinePosition.Line + 1,
            reason);
    }

    static void AddSpan(IDictionary<DocumentId, ImmutableHashSet<TextSpan>> spansByDocument, DocumentId documentId, TextSpan span)
    {
        spansByDocument.TryGetValue(documentId, out var spans);
        spansByDocument[documentId] = (spans ?? ImmutableHashSet<TextSpan>.Empty).Add(span);
    }
}
