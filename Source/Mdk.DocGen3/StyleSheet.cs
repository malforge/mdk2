namespace Mdk.DocGen3;

public static class StyleSheet
{
    public static void Write(string fileName)
    {
        // Get the css file from the embedded resource "style.css"
        var assembly = typeof(StyleSheet).Assembly;
        using var stream = assembly.GetManifestResourceStream("Mdk.DocGen3.style.css");
        if (stream is null)
        {
            throw new InvalidOperationException("Could not find embedded resource 'Mdk.DocGen3.style.css'.");
        }
        using var reader = new StreamReader(stream);
        var cssContent = reader.ReadToEnd();
        // Write the css content to the specified file
        using var writer = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);
        writer.Write(cssContent);
        Console.WriteLine($"Wrote stylesheet to {fileName}");
    }
}