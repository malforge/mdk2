namespace CheckDotNet;

partial class MessageBox
{
    static bool ConfirmWindows(string title, string message)
    {
        return Win32.MessageBox(IntPtr.Zero, message, title, 1) == 1;
    }
}