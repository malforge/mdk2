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
    readonly ILogger _logger;
    readonly bool _hasExplicitTitle;
    bool _isClosing;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HostWindow" /> class.
    /// </summary>
    /// <param name="title">Optional explicit window title.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public HostWindow(string? title, ILogger logger)
    {
        _logger = logger;
        _hasExplicitTitle = !string.IsNullOrEmpty(title);
        
        CanResize = true;

        // Set explicit title if provided
        if (_hasExplicitTitle)
            Title = title;

        // Use ContentControl to display the view
        Content = new ContentControl
        {
            [!ContentProperty] = this[!DataContextProperty]
        };
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // If no explicit title was set and ViewModel implements IHaveATitle, bind to it
        if (!_hasExplicitTitle && DataContext is IHaveATitle)
        {
            // Bind to the Title property on the DataContext
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
            // If the ViewModel supports close handling, ask it first
            if (DataContext is ISupportClosing supportClosing && !_isClosing)
            {
                // Cancel the default close
                e.Cancel = true;

                // Ask the ViewModel if it's OK to close
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