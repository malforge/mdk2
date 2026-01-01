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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    public class ScriptAnalyzer : DiagnosticAnalyzer
    {
        const string DefaultNamespaceName = "IngameScript";

        internal static readonly DiagnosticDescriptor ProhibitedMemberRule
            = new DiagnosticDescriptor("MDK01", "Prohibited Type Or Member", "The type or member '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor ProhibitedLanguageElementRule
            = new DiagnosticDescriptor("MDK02", "Prohibited Language Element", "The language element '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor InconsistentNamespaceDeclarationRule
            = new DiagnosticDescriptor("MDK03", "Inconsistent Namespace Declaration", "All ingame script code should be within the {0} namespace in order to avoid problems", "Whitelist", DiagnosticSeverity.Warning, true);

        readonly Whitelist _whitelist = new Whitelist();

        // readonly List<Uri> _ignoredFolders = new List<Uri>();
        // readonly List<Uri> _ignoredFiles = new List<Uri>();
        // Uri _basePath;
        HashSet<string> _allowedNamespaces;
        string _projectDir;
        Matcher _mdkIgnorePaths;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                ProhibitedMemberRule,
                ProhibitedLanguageElementRule,
                InconsistentNamespaceDeclarationRule);

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
            
            // Load settings from ini files (ignores and namespaces)
            LoadSettingsFromIni(context.Options.AdditionalFiles, context.CancellationToken, 
                out var ignorePathsFromIni, out var namespacesFromIni);
            
            // Fall back to MSBuild property for ignores if no ini files found
            if (string.IsNullOrEmpty(ignorePathsFromIni))
            {
                context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.mdk-ignorepaths", out ignorePathsFromIni);
            }

            if (!string.IsNullOrEmpty(ignorePathsFromIni))
            {
                var paths = ignorePathsFromIni.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var matcher = new Matcher();
                foreach (var path in paths)
                {
                    try
                    {
                        // Don't use Path.Combine - Matcher expects relative patterns
                        matcher.AddInclude(path.Trim());
                    }
                    catch
                    {
                        // Whatever.
                    }
                }

                _mdkIgnorePaths = matcher;
            }
            
            // Setup allowed namespaces
            if (!string.IsNullOrEmpty(namespacesFromIni))
            {
                _allowedNamespaces = new HashSet<string>(
                    namespacesFromIni.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ns => ns.Trim()),
                    StringComparer.Ordinal);
            }
            else
            {
                // Default to IngameScript if not specified
                _allowedNamespaces = new HashSet<string>(StringComparer.Ordinal) { DefaultNamespaceName };
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
        
        void LoadSettingsFromIni(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken,
            out string ignorePathsResult, out string namespacesResult)
        {
            var ignoresList = new List<string>();
            var namespacesList = new List<string>();
            
            // Find all .ini files
            var iniFiles = additionalFiles.Where(file => 
                file.Path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!iniFiles.Any())
            {
                ignorePathsResult = null;
                namespacesResult = null;
                return;
            }
            
            // Process local ini first, then main ini (matches Parameters.ParseAndLoadConfigs behavior)
            var localIni = iniFiles.FirstOrDefault(f => f.Path.IndexOf(".local.ini", StringComparison.OrdinalIgnoreCase) >= 0);
            var mainIni = iniFiles.FirstOrDefault(f => 
                f.Path.EndsWith("mdk.ini", StringComparison.OrdinalIgnoreCase) || 
                (f.Path.EndsWith(".mdk.ini", StringComparison.OrdinalIgnoreCase) && f.Path.IndexOf(".local.ini", StringComparison.OrdinalIgnoreCase) < 0));
            
            void ProcessIniFile(AdditionalText iniFile)
            {
                if (iniFile == null) return;
                
                var content = iniFile.GetText(cancellationToken)?.ToString();
                if (string.IsNullOrWhiteSpace(content)) return;
                
                // Simple ini parsing - look for ignores= and namespaces= lines under [mdk] section
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool inMdkSection = false;
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("["))
                    {
                        inMdkSection = trimmed.Equals("[mdk]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    
                    if (!inMdkSection)
                        continue;
                    
                    if (trimmed.StartsWith("ignores=", StringComparison.OrdinalIgnoreCase))
                    {
                        var ignoresValue = trimmed.Substring("ignores=".Length);
                        var patterns = ignoresValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        ignoresList.AddRange(patterns.Select(p => p.Trim()));
                    }
                    else if (trimmed.StartsWith("namespaces=", StringComparison.OrdinalIgnoreCase))
                    {
                        var namespacesValue = trimmed.Substring("namespaces=".Length);
                        var namespaces = namespacesValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        namespacesList.AddRange(namespaces.Select(ns => ns.Trim()));
                    }
                }
            }
            
            ProcessIniFile(localIni);
            ProcessIniFile(mainIni);
            
            ignorePathsResult = ignoresList.Any() ? string.Join(";", ignoresList.Distinct()) : null;
            namespacesResult = namespacesList.Any() ? string.Join(";", namespacesList.Distinct()) : null;
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
            
            // Check if namespace is in the allowed list
            if (_allowedNamespaces != null && !string.IsNullOrEmpty(namespaceName) && _allowedNamespaces.Contains(namespaceName))
                return;
            
            // If no namespace, report with suggestion for first allowed namespace
            var suggestedNamespace = _allowedNamespaces?.FirstOrDefault() ?? DefaultNamespaceName;
            var diagnostic = Diagnostic.Create(InconsistentNamespaceDeclarationRule,
                namespaceDeclaration?.Name.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
                string.Join(", ", _allowedNamespaces ?? new HashSet<string> { DefaultNamespaceName }));
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
            
            // Get relative path from project directory for matching
            var filePath = context.Node.SyntaxTree.FilePath;
            var relativePath = filePath;
            if (!string.IsNullOrEmpty(_projectDir) && filePath.StartsWith(_projectDir, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = filePath.Substring(_projectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            var result = _mdkIgnorePaths.Match(relativePath);
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