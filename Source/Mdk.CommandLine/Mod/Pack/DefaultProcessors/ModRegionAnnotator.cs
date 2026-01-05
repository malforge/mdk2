using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mdk.CommandLine.Mod.Pack.Api;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.Mod.Pack.DefaultProcessors;

[RunAfter<PreprocessorConditionals>]
public partial class ModRegionAnnotator : IDocumentProcessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        var root = await document.GetSyntaxRootAsync();
        var rewriter = new MdkAnnotationRewriter(context.Parameters.PackVerb.Macros);
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
        readonly bool _hasMacros;
        readonly Regex _regionRegex = GetRegionRegex();
        readonly Stack<RegionInfo> _stack = new();

        public MdkAnnotationRewriter(IReadOnlyDictionary<string, string> macros) : base(true)
        {
            _macros = macros;
            _hasMacros = macros.Count > 0;
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
            if (_hasMacros && token.IsKind(SyntaxKind.StringLiteralToken))
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
            if (_hasMacros)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    trivia = SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()));
                }

                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    trivia = ReplaceDocumentationTrivia(trivia);
                }

                if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    trivia = SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()));
                }

                if (trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    trivia = ReplaceDocumentationTrivia(trivia);
                }
            }

            if (region.Annotation != null)
                trivia = trivia.WithAdditionalAnnotations(region.Annotation);
            return trivia;
        }

        SyntaxTrivia ReplaceDocumentationTrivia(SyntaxTrivia trivia)
        {
            var replaced = ReplaceMacros(trivia.ToFullString());
            var parsed = SyntaxFactory.ParseLeadingTrivia(replaced);
            return parsed.Count > 0 ? parsed[0] : SyntaxFactory.Comment(replaced);
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
