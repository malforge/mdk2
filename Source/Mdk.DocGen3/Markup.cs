using System.Web;

namespace Mdk.DocGen3;

public class Markup(string tag, string? content)
{
    readonly Markup? _previous;
    readonly string _tag = tag;
    readonly string? _content = HttpUtility.HtmlEncode(content);
    Markup? _next;

    Markup(Markup previous, string s, string content) : this(s, content)
    {
        _previous = previous;
    }

    public Markup ThenIfNotEmpty(string tag, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return this;

        _next = new Markup(this, tag, content);
        return this;
    }

    public Markup Then(string tag, string? content)
    {
        _next = new Markup(this, tag, content ?? string.Empty);
        return this;
    }

    public override string ToString() => _previous?.ToString() ?? Generate();

    string Generate()
    {
        return "<" + _tag + ">" + _content + "</" + _tag + ">" + _next?.Generate();
    }
}