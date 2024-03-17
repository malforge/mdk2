using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

[RunBefore<PartialMerger>]
public partial class RegionAnnotator : IScriptPostprocessor
{
    public async Task<Document> ProcessAsync(Document document, ScriptProjectMetadata metadata)
    {
        var root = await document.GetSyntaxRootAsync();
        var rewriter = new MdkAnnotationRewriter(metadata.Macros);
        root = rewriter.Visit(root);
        if (root == null)
            throw new InvalidOperationException("Failed to rewrite document");
        return document.WithSyntaxRoot(root);
    }

    partial class MdkAnnotationRewriter : CSharpSyntaxRewriter
    {
        static readonly char[] TagSeparators = [' '];
        readonly Regex _macroRegex = GetMacroRegex();

        readonly IReadOnlyDictionary<string, string> _macros;
        readonly Regex _regionRegex = GetRegionRegex();
        readonly Stack<RegionInfo> _stack = new();

        public MdkAnnotationRewriter(IReadOnlyDictionary<string, string> macros) : base(true)
        {
            _macros = macros;
            _stack.Push(new RegionInfo());
        }

        string ReplaceMacros(string content) =>
            _macroRegex.Replace(content,
                match => _macros.GetValueOrDefault(match.Value, ""));

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            node = base.Visit(node);
            if (node == null)
                return null;
            var region = _stack.Peek();
            if (region.Annotation != null)
                return node.WithAdditionalAnnotations(region.Annotation);
            return node;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
            var region = _stack.Peek();
            if (token.IsKind(SyntaxKind.StringLiteralToken) && region.ExpandsMacros)
                token = SyntaxFactory.Literal(ReplaceMacros(token.ValueText));

            if (region.Annotation != null)
                token = token.WithAdditionalAnnotations(region.Annotation);

            if (token.HasStructuredTrivia)
            {
                if (token.HasLeadingTrivia)
                {
                    var originalTrivia = token.LeadingTrivia;
                    var trimmedTrivia = TrimTrivia(originalTrivia);
                    token = token.WithLeadingTrivia(trimmedTrivia);
                }

                if (token.HasTrailingTrivia)
                {
                    var originalTrivia = token.TrailingTrivia;
                    var trimmedTrivia = TrimTrivia(originalTrivia);
                    token = token.WithTrailingTrivia(trimmedTrivia);
                }
            }

            return token;
        }

        static SyntaxTriviaList TrimTrivia(SyntaxTriviaList source)
        {
            var list = new List<SyntaxTrivia>();
            foreach (var trivia in source)
            {
                if (trivia.HasStructure)
                {
                    if (trivia.GetStructure() is RegionDirectiveTriviaSyntax regionDirective)
                    {
                        list.AddRange(regionDirective.GetLeadingTrivia());
                        while (list.Count > 0 && list[^1].IsKind(SyntaxKind.WhitespaceTrivia))
                            list.RemoveAt(list.Count - 1);
                        list.AddRange(regionDirective.GetTrailingTrivia().SkipWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)));
                        continue;
                    }

                    if (trivia.GetStructure() is EndRegionDirectiveTriviaSyntax endRegionDirective)
                    {
                        list.AddRange(endRegionDirective.GetLeadingTrivia());
                        while (list.Count > 0 && list[^1].IsKind(SyntaxKind.WhitespaceTrivia))
                            list.RemoveAt(list.Count - 1);
                        list.AddRange(endRegionDirective.GetTrailingTrivia().SkipWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)));
                        continue;
                    }
                }

                list.Add(trivia);
            }

            return SyntaxFactory.TriviaList(list);
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            trivia = base.VisitTrivia(trivia);
            var region = _stack.Peek();
            if (region.ExpandsMacros && region.Annotation != null)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    return SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()))
                        .WithAdditionalAnnotations(region.Annotation);
                }

                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    //return SyntaxFactory.DocumentationCommentTrivia(ReplaceMacros(trivia.ToFullString()))
                    //    .WithAdditionalAnnotations(region.Annotation);
                }

                if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    return SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()))
                        .WithAdditionalAnnotations(region.Annotation);
                }

                if (trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)) { }
            }

            if (region.Annotation != null)
                trivia = trivia.WithAdditionalAnnotations(region.Annotation);
            return trivia;
        }

        public override SyntaxNode? VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            var newNode = (RegionDirectiveTriviaSyntax?)base.VisitRegionDirectiveTrivia(node);
            if (newNode == null)
                return null;
            var region = _stack.Peek();
            var content = node.ToString().Trim();
            var match = _regionRegex.Match(content);
            if (match.Success)
            {
                var tags = match.Groups[1].Value.Trim().Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries);
                var tagString = string.Join(" ", tags);
                if (region.Annotation != null)
                    tagString = region.Annotation.Data + " " + tagString;
                region = new RegionInfo(new SyntaxAnnotation("MDK", tagString));
                _stack.Push(region);
                return node;
            }

            _stack.Push(region.AsCopy());
            return region.Annotation != null ? node.WithAdditionalAnnotations(region.Annotation) : node;
        }

        public override SyntaxNode? VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            var region = _stack.Pop();
            if (region.IsDeclaration)
                return null;
            region = _stack.Peek();
            return region.Annotation != null ? node.WithAdditionalAnnotations(region.Annotation) : node;
        }

        [GeneratedRegex(@"\$\w+\$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex GetMacroRegex();

        [GeneratedRegex(@"\s*#region\s+mdk\s+([^\r\n]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex GetRegionRegex();

        readonly partial struct RegionInfo(SyntaxAnnotation? annotation, bool isDeclaration = true)
        {
            public SyntaxAnnotation? Annotation { get; } = annotation;
            public bool IsDeclaration { get; } = isDeclaration;
            public bool ExpandsMacros { get; } = annotation?.Data != null && GetMacrosRegex().IsMatch(annotation.Data);

            public RegionInfo AsCopy() => new(Annotation, false);

            [GeneratedRegex(@"\bmacros\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
            private static partial Regex GetMacrosRegex();
        }
    }
}