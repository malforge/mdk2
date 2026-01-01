// Mdk.ModAnalyzers
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mdk2.ModAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    public class ScriptAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor ProhibitedMemberRule
            = new DiagnosticDescriptor("MDK01", "Prohibited Type Or Member", "The type or member '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        readonly Whitelist _whitelist = new Whitelist();
        Matcher _mdkIgnorePaths;
        Uri _projectDirUri;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(ProhibitedMemberRule);

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
            using (var stream = GetType().Assembly.GetManifestResourceStream("modwhitelist.dat"))
            {
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Error loading embedded whitelist cache")))
                    lines = reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            }

            _whitelist.IsEnabled = true;
            _whitelist.Load(lines);
        }

        void RegisterActions(CompilationStartAnalysisContext context)
        {
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir);
            _projectDirUri = new Uri(Path.GetFullPath(projectDir ?? "."));
            
            // Try to load ignore paths from ini files in AdditionalFiles first
            var ignorePathsFromIni = TryLoadIgnorePathsFromIni(context.Options.AdditionalFiles, context.CancellationToken);
            
            // Fall back to MSBuild property if no ini files found
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
                        // Don't use full paths - Matcher expects relative patterns
                        matcher.AddInclude(path.Trim());
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
        }
        
        string TryLoadIgnorePathsFromIni(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var ignoresList = new List<string>();
            
            // Find all .ini files
            var iniFiles = additionalFiles.Where(file => 
                file.Path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!iniFiles.Any())
                return null;
            
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
                
                // Simple ini parsing - look for ignores= line under [mdk] section
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
                    
                    if (inMdkSection && trimmed.StartsWith("ignores=", StringComparison.OrdinalIgnoreCase))
                    {
                        var ignoresValue = trimmed.Substring("ignores=".Length);
                        var patterns = ignoresValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        ignoresList.AddRange(patterns.Select(p => p.Trim()));
                        break;
                    }
                }
            }
            
            ProcessIniFile(localIni);
            ProcessIniFile(mainIni);
            
            return ignoresList.Any() ? string.Join(";", ignoresList.Distinct()) : null;
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
                return;

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

            var diagnostic = Diagnostic.Create(ProhibitedMemberRule, node.GetLocation(), info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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

            var fileUri = new Uri(context.Node.SyntaxTree.FilePath);
            var result = _mdkIgnorePaths.Match(_projectDirUri.MakeRelativeUri(fileUri).ToString());
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