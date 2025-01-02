using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Mod.Pack.DefaultProcessors;

[XmlRoot("mdk")]
public sealed partial class MdkTrivia
{
    static readonly Regex MdkTriviaRegex = IsMdkTriviaRegex();
    static readonly XmlSerializer Serializer = new(typeof(MdkTrivia));

    [XmlAttribute("sortorder")]
    public int SortOrder { get; set; }

    [XmlIgnore]
    public bool SortOrderSpecified { get; set; }

    [GeneratedRegex(@"\A\s*//\s*(<mdk\s+.*?/>)\s*\z")]
    private static partial Regex IsMdkTriviaRegex();

    public static bool TryGetMdkTrivia(SyntaxTree tree, [MaybeNullWhen(false)] out MdkTrivia trivia)
    {
        trivia = null;
        var root = tree.GetRoot();
        var triviaList = root.GetLeadingTrivia();
        foreach (var triviaItem in triviaList)
        {
            if (!triviaItem.IsKind(SyntaxKind.SingleLineCommentTrivia))
                continue;
            var match = MdkTriviaRegex.Match(triviaItem.ToString());
            if (!match.Success)
                continue;
            var xmlPart = match.Groups[1].Value;
            try
            {
                using (var reader = new StringReader(xmlPart))
                    trivia = (MdkTrivia?)Serializer.Deserialize(reader);
                if (trivia is not null)
                    return true;
            }
            catch
            {
                // ignored                
            }
        }

        trivia = null;
        return false;
    }
}