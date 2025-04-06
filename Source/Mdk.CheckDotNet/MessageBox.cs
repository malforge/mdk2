namespace CheckDotNet;

public static partial class MessageBox
{
    public static bool Confirm(string title, string message)
    {
        if (OperatingSystem.IsWindows()) return ConfirmWindows(title, message);
        if (OperatingSystem.IsLinux()) return ConfirmLinux(title, message);
        return false;
    }
}