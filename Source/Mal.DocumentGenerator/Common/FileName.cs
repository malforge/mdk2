using System;

namespace Mal.DocumentGenerator.Common;

public static class FileName
{
    public static string GenerateSafeFileName(string name)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Array.IndexOf(invalidChars, c) >= 0)
                sb.Append('_');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}