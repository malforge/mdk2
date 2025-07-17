namespace Mdk.DocGen3.Pages.Base;

public interface IGenerator
{
    string Render();
    string Render(IGenerator content);
    string Render(IReadOnlyDictionary<string, IGenerator> content);
}