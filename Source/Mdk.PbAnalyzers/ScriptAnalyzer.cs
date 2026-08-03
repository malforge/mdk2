// Mdk.ModAnalyzers
// 
// Copyright 2023-2026 The MDK² Authors

using System;
using System.Collections.Concurrent;
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
using Microsoft.CodeAnalysis.Text;
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

        internal static readonly DiagnosticDescriptor RuntimeUseOfTrimmedAttributeRule
            = new DiagnosticDescriptor("MDK04", "Runtime Use Of Trimmed Attribute", "Tooling-only attribute type '{0}' is used by runtime code. Attribute trimming removes this type from packed source.", "Attribute Trimming", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor UnavailableUsingDirectiveRule
            = new DiagnosticDescriptor("MDK05", "Using Directive Does Not Survive Packing",
                "{0} is not available to the packed script, because packing removes using directives.{1}", "Whitelist", DiagnosticSeverity.Warning, true,
                "Packing strips every using directive from the script, and the programmable block compiles the result with a fixed set of imported namespaces. "
                + "Namespace imports outside that set, aliases and static imports all lose their meaning, so whatever they shortened has to be written out in full.");

        internal static readonly DiagnosticDescriptor ScriptNamespaceReferenceRule
            = new DiagnosticDescriptor("MDK06", "Reference To A Script Namespace",
                "The namespace '{0}' only exists in your project and is removed during packing, so this reference will not compile ingame", "Whitelist", DiagnosticSeverity.Error, true,
                "The programmable block does not support namespaces, so packing unwraps every namespace your script declares. "
                + "Names qualified through one of those namespaces have nothing left to resolve against ingame. Refer to the type directly instead.");

        /// <summary>
        ///     The using directives the programmable block puts in front of every script, extracted from the game the same
        ///     way the whitelist is. Parsed as the C# it is, so that whichever directive forms the game emits are all
        ///     understood rather than only the ones anyone thought to look for.
        /// </summary>
        static readonly PbPrologue Prologue = PbPrologue.LoadEmbedded();

        readonly Whitelist _whitelist = new Whitelist();

        // readonly List<Uri> _ignoredFolders = new List<Uri>();
        // readonly List<Uri> _ignoredFiles = new List<Uri>();
        // Uri _basePath;
        HashSet<string> _allowedNamespaces;
        string _projectDir;
        Matcher _mdkIgnorePaths;
        ConcurrentDictionary<SyntaxTree, bool> _ignorableTrees;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                ProhibitedMemberRule,
                ProhibitedLanguageElementRule,
                InconsistentNamespaceDeclarationRule,
                RuntimeUseOfTrimmedAttributeRule,
                UnavailableUsingDirectiveRule,
                ScriptNamespaceReferenceRule);

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
            // Fresh per compilation, alongside the other per-compilation state on this analyzer.
            _ignorableTrees = new ConcurrentDictionary<SyntaxTree, bool>();
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
            // Per document rather than per node: deciding whether a using directive is actually needed takes the
            // compiler's own verdict on the whole file, which is worth asking for exactly once.
            context.RegisterSemanticModelAction(AnalyzeUsingDirectives);
            // Member access is only visited for the sake of namespace references. In expression position a namespace of
            // more than one segment, such as A.B in A.B.SomeType.Method(), is a member access rather than a qualified
            // name, so without this it would go unnoticed. The whitelist rules deliberately do not run here; they have
            // never looked at member access and this is not the place to change that.
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessNamespace,
                SyntaxKind.SimpleMemberAccessExpression);
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

        void AnalyzeUsingDirectives(SemanticModelAnalysisContext context)
        {
            var model = context.SemanticModel;
            if (IsIgnorableTree(model.SyntaxTree))
                return;

            var root = model.SyntaxTree.GetRoot(context.CancellationToken);

            // Using directives live directly under the compilation unit or a namespace, so there is no reason to walk
            // into type declarations looking for them.
            var usingDirectives = root
                .DescendantNodes(node => node is CompilationUnitSyntax || node is BaseNamespaceDeclarationSyntax)
                .OfType<UsingDirectiveSyntax>()
                .ToArray();
            if (usingDirectives.Length == 0)
                return;

            // A using directive the file does not actually need cannot break anything by disappearing during packing.
            // Unused imports are extremely common - old templates still seed System.Threading.Tasks, and a class nested
            // inside Program picks up its members without the static import that names them - so reporting them would be
            // noise on code that works perfectly.
            //
            // The usage test is deliberately left until a directive has otherwise earned a warning, since it walks the
            // file and almost every directive is ruled out before then by much cheaper checks.
            foreach (var usingDirective in usingDirectives)
                AnalyzeUsingDirective(context, usingDirective, root);
        }

        void AnalyzeUsingDirective(SemanticModelAnalysisContext context, UsingDirectiveSyntax usingDirective, SyntaxNode root)
        {
            var name = usingDirective.Name;
            if (name == null)
                return;

            // An alias is a name that exists nowhere but in this directive, so packing takes it away - unless the
            // programmable block happens to declare the very same alias itself.
            if (usingDirective.Alias != null)
            {
                if (Prologue.DeclaresAlias(usingDirective.Alias.Name.ToString(), name.ToString()))
                    return;

                // An alias that simply restates the type's own name changes nothing once it is gone, provided the type
                // is reachable anyway. Scripts write these to disambiguate the mod API from the ingame API while
                // editing - "using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock" - and the packed script
                // resolves that name through the block's own import regardless.
                if (IsRedundantAlias(usingDirective, context.SemanticModel, context.CancellationToken))
                    return;

                if (!DirectiveUsage.IsUsed(usingDirective, context.SemanticModel, root, context.CancellationToken))
                    return;

                context.ReportDiagnostic(Diagnostic.Create(UnavailableUsingDirectiveRule, usingDirective.Alias.Name.GetLocation(),
                    "The alias '" + usingDirective.Alias.Name + "'", ""));
                return;
            }

            // The programmable block imports namespaces, never types, so a static import is never reinstated.
            if (usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            {
                if (!DirectiveUsage.IsUsed(usingDirective, context.SemanticModel, root, context.CancellationToken))
                    return;

                context.ReportDiagnostic(Diagnostic.Create(UnavailableUsingDirectiveRule, name.GetLocation(),
                    "The static import of '" + name + "'", ""));
                return;
            }

            var namespaceSymbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol as INamespaceSymbol;
            if (namespaceSymbol == null)
                return;

            // Packing unwraps the script's own namespaces, so importing one of them is harmless.
            if (namespaceSymbol.IsInSource())
                return;

            var namespaceName = namespaceSymbol.ToDisplayString();
            if (Prologue.Provides(namespaceName))
                return;

            if (!DirectiveUsage.IsUsed(usingDirective, context.SemanticModel, root, context.CancellationToken))
                return;

            // The mod API namespaces mirror the ingame ones, and picking the wrong one is the usual reason to end up here.
            var hint = Prologue.Imports(namespaceName + ".Ingame")
                ? " Did you mean '" + namespaceName + ".Ingame'?"
                : "";

            context.ReportDiagnostic(Diagnostic.Create(UnavailableUsingDirectiveRule, name.GetLocation(),
                "The namespace '" + namespaceName + "'", hint));
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

            // Namespace references have to be handled before the qualified name shortcut below, because that shortcut
            // deliberately skips the namespace part of a qualified name.
            if (AnalyzeNamespaceReference(context))
                return;

            // We'll check the qualified names on their own.
            if (IsQualifiedName(node.Parent))
                return;

            var info = context.SemanticModel.GetSymbolInfo(node);
            if (info.Symbol == null)
                return;

            if (AttributeContext.IsSourceDefinedAttributeTypeReference(info.Symbol, context.SemanticModel, context.CancellationToken))
            {
                if (!AttributeContext.IsAllowed(node, context.SemanticModel, context.CancellationToken))
                {
                    diagnostic = Diagnostic.Create(
                        RuntimeUseOfTrimmedAttributeRule,
                        node.GetLocation(),
                        info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    context.ReportDiagnostic(diagnostic);
                }

                return;
            }

            if (AttributeContext.IsAllowed(node, context.SemanticModel, context.CancellationToken))
                return;

            // If they wrote it, they can have it.
            if (info.Symbol.IsInSource())
                return;

            if (_whitelist.IsWhitelisted(info.Symbol))
                return;
            diagnostic = Diagnostic.Create(ProhibitedMemberRule, node.GetLocation(), info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        ///     Whether the alias names a type that the packed script can reach under that same name without it.
        /// </summary>
        static bool IsRedundantAlias(UsingDirectiveSyntax usingDirective, SemanticModel model, CancellationToken cancellationToken)
        {
            var target = model.GetSymbolInfo(usingDirective.Name, cancellationToken).Symbol as ITypeSymbol;
            if (target == null)
                return false;

            if (usingDirective.Alias.Name.Identifier.ValueText != target.Name)
                return false;

            var containing = target.ContainingNamespace;
            if (containing == null || containing.IsGlobalNamespace)
                return false;

            // Either the block imports the namespace itself, or the script declares it and packing flattens the type
            // out to where a plain name finds it.
            return Prologue.Imports(containing.ToDisplayString()) || containing.IsInSource();
        }

        void AnalyzeMemberAccessNamespace(SyntaxNodeAnalysisContext context)
        {
            // Cheapest test first: member access is one of the most common nodes there is, and almost none of it sits
            // where a namespace qualifier could.
            if (!CouldQualifyANamespace(context.Node) || IsIgnorableNode(context))
                return;
            AnalyzeNamespaceReference(context);
        }

        /// <summary>
        ///     Reports names qualified through a namespace the script itself declares, since packing unwraps those
        ///     namespaces and leaves the qualification dangling.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the node resolved to a namespace, in which case it needs no further analysis.
        /// </returns>
        bool AnalyzeNamespaceReference(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            // Purely syntactic, and deliberately so: without it every identifier in the file would cost a symbol lookup,
            // including the ones the whitelist rules below skip outright. Anything ruled out here either cannot qualify a
            // namespace or is a segment whose enclosing node gets checked instead.
            if (!CouldQualifyANamespace(node))
                return false;

            var namespaceSymbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol as INamespaceSymbol;
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                return false;

            // Report only the outermost segment, so that A.B.C.SomeType reports once, against "A.B.C".
            if (node.Parent != null && context.SemanticModel.GetSymbolInfo(node.Parent, context.CancellationToken).Symbol is INamespaceSymbol)
                return true;

            // A namespace declaration names its namespace rather than referencing it, and using directives are MDK05's business.
            if (IsNamespaceDeclarationOrUsingDirectiveName(node))
                return true;

            // A namespace that also exists outside the project survives packing, so only purely script-declared ones break.
            if (!namespaceSymbol.IsInSource())
                return true;

            context.ReportDiagnostic(Diagnostic.Create(ScriptNamespaceReferenceRule, node.GetLocation(), namespaceSymbol.ToDisplayString()));
            return true;
        }

        /// <summary>
        ///     Whether a node sits where the leading, namespace-carrying part of a qualified name would sit.
        /// </summary>
        static bool CouldQualifyANamespace(SyntaxNode node)
        {
            switch (node.Parent)
            {
                case QualifiedNameSyntax qualified:
                    return qualified.Left == node;
                case MemberAccessExpressionSyntax memberAccess:
                    return memberAccess.Expression == node;
                // The alias and name halves say nothing on their own; the alias-qualified name as a whole is checked instead.
                case AliasQualifiedNameSyntax _:
                    return false;
                default:
                    return true;
            }
        }

        static bool IsNamespaceDeclarationOrUsingDirectiveName(SyntaxNode node)
        {
            var current = node;
            while (current.Parent is NameSyntax)
                current = current.Parent;

            switch (current.Parent)
            {
                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    return namespaceDeclaration.Name == current;
                case UsingDirectiveSyntax usingDirective:
                    return usingDirective.Name == current;
                default:
                    return false;
            }
        }

        bool IsIgnorableNode(SyntaxNodeAnalysisContext context) => IsIgnorableTree(context.Node.SyntaxTree);

        /// <summary>
        ///     Whether this file is outside the analyzer's remit. Cached per file: the answer cannot change within a
        ///     compilation, and the uncached version globs the path on every call, which the syntax node rules make many
        ///     thousands of times per file.
        /// </summary>
        bool IsIgnorableTree(SyntaxTree syntaxTree)
        {
            var cache = _ignorableTrees;
            if (cache == null)
                return IsIgnorableTreeUncached(syntaxTree);

            bool ignorable;
            if (cache.TryGetValue(syntaxTree, out ignorable))
                return ignorable;

            ignorable = IsIgnorableTreeUncached(syntaxTree);
            cache[syntaxTree] = ignorable;
            return ignorable;
        }

        bool IsIgnorableTreeUncached(SyntaxTree syntaxTree)
        {
            if (!_whitelist.IsEnabled || _whitelist.IsEmpty())
                return true;

            var fileName = Path.GetFileName(syntaxTree.FilePath);

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
            var filePath = syntaxTree.FilePath;
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
