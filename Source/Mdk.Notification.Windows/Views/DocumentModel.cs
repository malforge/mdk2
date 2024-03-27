namespace Mdk.Notification.Windows.Views;

public class DocumentModel(string title, string markdown)
{
    public string Title { get; } = title;
    public string Markdown { get; } = markdown;
}