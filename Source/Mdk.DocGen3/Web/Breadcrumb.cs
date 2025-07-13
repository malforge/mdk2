namespace Mdk.DocGen3.Web;

public class Breadcrumb(string slug, string name) 
{
    public string Slug { get; } = slug;
    public string Name { get; } = name;
}