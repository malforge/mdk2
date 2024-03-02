using System;

namespace Mdk.CommandLine.Interactive;

/// <summary>
/// A hyperlink to be displayed in a toast window.
/// </summary>
/// <param name="text"></param>
/// <param name="callback"></param>
public readonly struct ToastHyperlink(string text, Func<ToastHyperlink, bool> callback)
{
    public readonly string Text = text;
    public readonly Func<ToastHyperlink, bool> Callback = callback;
}