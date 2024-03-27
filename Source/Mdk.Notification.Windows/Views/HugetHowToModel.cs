using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Windows.Input;

namespace Mdk.Notification.Windows.Views;

public class HugetHowToModel : Model
{
    const string MdkSite = "https://github.com/malware-dev/MDK-SE/issues";

    public HugetHowToModel()
    {
        var resources = GetType().Assembly.GetManifestResourceNames()
            .Where(x => x.StartsWith("NugetHowToDocuments/") && x.EndsWith(".md"))
            .ToArray();

        Documents = resources.Select(ToDocumentModel).Where(x => x is not null).OrderBy(d => d!.Title).ToImmutableArray()!;
    }

    public ImmutableArray<DocumentModel> Documents { get; }

    public ICommand OpenWebPageCommand => new ModelCommand(OpenWebPage);

    void OpenWebPage()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(MdkSite)
            {
                UseShellExecute = true,
                Verb = "open"
            }
        };
        try
        {
            process.Start();
        }
        catch
        {
            // ignored
        }
    }

    DocumentModel? ToDocumentModel(string resource)
    {
        using var stream = GetType().Assembly.GetManifestResourceStream(resource);
        if (stream == null)
            return null;
        using var reader = new StreamReader(stream);
        var markdownTxt = reader.ReadToEnd();
        var title = HttpUtility.HtmlDecode(Path.GetFileNameWithoutExtension(resource));
        return new DocumentModel(title, markdownTxt);
    }
}