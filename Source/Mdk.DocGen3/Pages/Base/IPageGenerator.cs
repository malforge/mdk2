namespace Mdk.DocGen3.Pages.Base;

public interface IPageGenerator
{
    object? Model { get; }
    string Render();
    string RenderSegment();
}