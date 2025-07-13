namespace Mdk.DocGen3.Web;

public class TestNamespaceLayout
{
    public string? Title { get; set; }
    public string? CssSlug { get; set; }
    public string? IndexSlug { get; set; }
    public Namespace Namespace { get; set; } = new Namespace();

    public Html Render(Func<HtmlElement> content)
    {
        return Html.Document(
            Html.Head()
                .Title(Title)
                .Charset("utf8")
                .StyleSheet(CssSlug),
            Html.Body(
                Html.Div(
                    Html.Aside(
                        Html.Div(
                            Html.A(IndexSlug, Title ?? "")
                        ),
                        Html.ForEach(Namespace.Types,
                            t => Html.Div(
                                Html.A(t.Slug, t.Name ?? "")
                            )
                        )
                    ).WithClass("sidebar")
                ).WithClass("layout"),
                Html.Main(
                    content()
                ).WithClass("content")
            )
        );
    }
}