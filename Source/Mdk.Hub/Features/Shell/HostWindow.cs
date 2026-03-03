using System;
using Avalonia.Controls;
using Avalonia.Data;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     A window that hosts a ViewModel and delegates close handling to it.
/// </summary>
public class HostWindow : Window
{
    readonly bool _hasExplicitTitle;
    readonly ILogger _logger;
    readonly IWindowScope? _scope;
    bool _isClosing;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HostWindow" /> class.
    /// </summary>
    /// <param name="title">Optional explicit window title.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="scope">Optional window-scoped DI container. Disposed when the window closes.</param>
    public HostWindow(string? title, ILogger logger, IWindowScope? scope = null)
    {
        _logger = logger;
        _scope = scope;
        _hasExplicitTitle = !string.IsNullOrEmpty(title);

        CanResize = true;

        if (_hasExplicitTitle)
            Title = title;

        if (scope != null)
            WindowContainer.SetContainer(this, scope.Container);

        Content = new ContentControl
        {
            [!ContentProperty] = this[!DataContextProperty]
        };
    }

    /// <summary>
    ///     Gets whether the window has been closed.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (!_hasExplicitTitle && DataContext is IHaveATitle)
        {
            Bind(TitleProperty,
                new Binding(nameof(IHaveATitle.Title))
                {
                    Mode = BindingMode.OneWay
                });
        }
    }

    /// <inheritdoc />
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            if (DataContext is ISupportClosing supportClosing && !_isClosing)
            {
                e.Cancel = true;
                var canClose = await supportClosing.WillCloseAsync();
                if (canClose)
                {
                    _isClosing = true;
                    Close();
                }
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Error during window closing", exception);
        }
        finally
        {
            base.OnClosing(e);
        }
    }

    /// <inheritdoc />
    protected override async void OnClosed(EventArgs e)
    {
        try
        {
            IsClosed = true;

            if (DataContext is ISupportClosing supportClosing)
                await supportClosing.DidCloseAsync();

            switch (DataContext)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
            }

            _scope?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.Error("Error during window closed handling", exception);
        }
        finally
        {
            base.OnClosed(e);
        }
    }
}
