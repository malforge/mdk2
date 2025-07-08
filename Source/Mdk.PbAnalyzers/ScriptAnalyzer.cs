// Mdk.ModAnalyzers
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mdk2.PbAnalyzers
{
    /// <summary>
    /// Analyzes ingame scripts for compliance with Space Engineers' restrictions and conventions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    public class ScriptAnalyzer : DiagnosticAnalyzer
    {
        const string DefaultNamespaceName = "IngameScript";

        /// <summary>
        /// Diagnostic rule for prohibited types or members.
        /// </summary>
        internal static readonly DiagnosticDescriptor ProhibitedMemberRule
            = new DiagnosticDescriptor("MDK01", "Prohibited Type Or Member", "The type or member '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        /// <summary>
        /// Diagnostic rule for prohibited types or members.
        /// </summary>
        internal static readonly DiagnosticDescriptor ProhibitedLanguageElementRule
            = new DiagnosticDescriptor("MDK02", "Prohibited Language Element", "The language element '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        /// <summary>
        /// Diagnostic rule for inconsistent namespace declarations.
        /// </summary>
        internal static readonly DiagnosticDescriptor InconsistentNamespaceDeclarationRule
            = new DiagnosticDescriptor("MDK03", "Inconsistent Namespace Declaration", "All ingame script code should be within the {0} namespace in order to avoid problems", "Whitelist", DiagnosticSeverity.Warning, true);

        /// <summary>
        /// Diagnostic rule for referencing custom namespaces.
        /// </summary>
        internal static readonly DiagnosticDescriptor ReferenceOfCustomNamespaceRule
            = new DiagnosticDescriptor("MDK04", "Reference of Custom Namespace", "The programmable block does not use namespaces, so referencing custom namespaces (like {0}) will cause errors", "Whitelist", DiagnosticSeverity.Error, true);
        
        readonly Whitelist _whitelist = new Whitelist();

        // readonly List<Uri> _ignoredFolders = new List<Uri>();
        // readonly List<Uri> _ignoredFiles = new List<Uri>();
        // Uri _basePath;
        readonly string _namespaceName = DefaultNamespaceName;
        string _projectDir;
        Matcher _mdkIgnorePaths;

        /// <summary>
        /// Gets the list of supported diagnostics for this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                ProhibitedMemberRule,
                ProhibitedLanguageElementRule,
                InconsistentNamespaceDeclarationRule);

        /// <summary>
        /// Initializes the analyzer and registers the necessary actions.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(RegisterActions);
        }

        bool TryLoadWhitelist(IEnumerable<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var whitelistCache = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("whitelist.cache", StringComparison.CurrentCultureIgnoreCase));
            var content = whitelistCache?.GetText(cancellationToken);
            if (content == null)
            {
                _whitelist.IsEnabled = false;
                return false;
            }

            _whitelist.IsEnabled = true;
            _whitelist.Load(content.Lines.Select(l => l.ToString()).ToArray());
            return true;
        }

        void LoadEmbeddedWhitelist()
        {
            string[] lines;
            using (var stream = GetType().Assembly.GetManifestResourceStream("pbwhitelist.dat"))
            {
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Error loading embedded whitelist cache")))
                    lines = reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            }

            _whitelist.IsEnabled = true;
            _whitelist.Load(lines);
        }

        void RegisterActions(CompilationStartAnalysisContext context)
        {
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out _projectDir);
            _projectDir = _projectDir ?? ".";
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.mdk-ignorepaths", out var ignorePaths);

            if (!string.IsNullOrEmpty(ignorePaths))
            {
                var paths = ignorePaths.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var matcher = new Matcher();
                foreach (var path in paths)
                {
                    try
                    {
                        matcher.AddInclude(Path.Combine(_projectDir, path));
                    }
                    catch
                    {
                        // Whatever.
                    }
                }

                _mdkIgnorePaths = matcher;
            }

            if (!TryLoadWhitelist(context.Options.AdditionalFiles, context.CancellationToken))
                LoadEmbeddedWhitelist();

            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName,
                SyntaxKind.DestructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeDeclaration,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.VariableDeclaration,
                SyntaxKind.Parameter);
            context.RegisterSyntaxNodeAction(AnalyzeNamespace,
                SyntaxKind.ClassDeclaration);
        }

        void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            if (IsIgnorableNode(context))
                return;
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.Parent is TypeDeclarationSyntax)
                return;

            var namespaceDeclaration = classDeclaration.Parent as NamespaceDeclarationSyntax;
            var namespaceName = namespaceDeclaration?.Name.ToString();
            if (_namespaceName.Equals(namespaceName, StringComparison.Ordinal))
                return;
            var diagnostic = Diagnostic.Create(InconsistentNamespaceDeclarationRule,
                namespaceDeclaration?.Name.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
                _namespaceName);
            context.ReportDiagnostic(diagnostic);
        }

        void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
                return;
            Diagnostic diagnostic;
            IdentifierNameSyntax identifier;

            switch (node.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                    identifier = ((PropertyDeclarationSyntax)node).Type as IdentifierNameSyntax;
                    break;
                case SyntaxKind.VariableDeclaration:
                    identifier = ((VariableDeclarationSyntax)node).Type as IdentifierNameSyntax;
                    break;
                case SyntaxKind.Parameter:
                    identifier = ((ParameterSyntax)node).Type as IdentifierNameSyntax;
                    break;
                default:
                    identifier = null;
                    break;
            }

            if (identifier == null)
                return;
            var name = identifier.Identifier.ToString();
            if (name != "dynamic")
                return;
            diagnostic = Diagnostic.Create(ProhibitedLanguageElementRule, identifier.Identifier.GetLocation(), name);
            context.ReportDiagnostic(diagnostic);
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
                return;

            Diagnostic diagnostic;
            // Destructors are unpredictable so they cannot be allowed
            if (node.IsKind(SyntaxKind.DestructorDeclaration))
            {
                var kw = ((DestructorDeclarationSyntax)node).Identifier;
                diagnostic = Diagnostic.Create(ProhibitedLanguageElementRule, kw.GetLocation(), kw.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // We'll check the qualified names on their own.
            if (IsQualifiedName(node.Parent))
                return;

            var info = context.SemanticModel.GetSymbolInfo(node);
            if (info.Symbol == null)
                return;

            // If they wrote it, they can have it.
            if (info.Symbol.IsInSource())
                return;

            if (_whitelist.IsWhitelisted(info.Symbol))
                return;
            diagnostic = Diagnostic.Create(ProhibitedMemberRule, node.GetLocation(), info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            context.ReportDiagnostic(diagnostic);
        }

        bool IsIgnorableNode(SyntaxNodeAnalysisContext context)
        {
            if (!_whitelist.IsEnabled || _whitelist.IsEmpty())
                return true;

            var fileName = Path.GetFileName(context.Node.SyntaxTree.FilePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            if (fileName.Contains(".NETFramework,Version="))
                return true;

            if (fileName.EndsWith(".debug", StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (fileName.IndexOf(".debug.", StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (_mdkIgnorePaths == null)
                return false;
            
            var result = _mdkIgnorePaths.Match(context.Node.SyntaxTree.FilePath);
            return result.HasMatches;
        }

        bool IsQualifiedName(SyntaxNode arg)
        {
            switch (arg.Kind())
            {
                case SyntaxKind.QualifiedName:
                case SyntaxKind.AliasQualifiedName:
                    return true;
            }

            return false;
        }
    }
}