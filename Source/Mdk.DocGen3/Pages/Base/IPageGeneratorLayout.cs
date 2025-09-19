namespace Mdk.DocGen3.Pages.Base;

public interface IPageGeneratorLayout
{
    object? Model { get; }
    string Render(IReadOnlyDictionary<string, IPageGenerator> content);
}