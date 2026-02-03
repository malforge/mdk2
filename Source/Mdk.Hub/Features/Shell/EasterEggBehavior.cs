using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Mdk.Hub.Features.Shell;

/// <summary>
/// Attached property behavior that displays the easter egg on a control.
/// </summary>
public static class EasterEggBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(EasterEggBehavior));

    static readonly AttachedProperty<EasterEggAttachment?> AttachmentProperty =
        AvaloniaProperty.RegisterAttached<Control, EasterEggAttachment?>("Attachment", typeof(EasterEggBehavior));

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    static EasterEggBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property != IsEnabledProperty)
            return;

        var isEnabled = (bool)args.NewValue!;
        var attachment = control.GetValue(AttachmentProperty);

        if (isEnabled && attachment == null)
        {
            // Attach
            attachment = new EasterEggAttachment(control);
            control.SetValue(AttachmentProperty, attachment);
        }
        else if (!isEnabled && attachment != null)
        {
            // Detach
            attachment.Dispose();
            control.SetValue(AttachmentProperty, null);
        }
    }

    class EasterEggAttachment : IDisposable
    {
        readonly Control _host;
        readonly IEasterEggService _service;
        EasterEgg? _easterEgg;
        bool _isDisposed;

        public EasterEggAttachment(Control host)
        {
            _host = host;
            _service = App.Container.Resolve<IEasterEggService>();

            // Subscribe to service changes
            _service.ActiveChanged += OnActiveChanged;
            
            // Wait for control to be loaded, then show
            _host.AttachedToVisualTree += OnHostAttachedToVisualTree;
        }

        void OnHostAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _host.AttachedToVisualTree -= OnHostAttachedToVisualTree;
            _ = ShowAfterDelayAsync();
        }

        async Task ShowAfterDelayAsync()
        {
            if (_isDisposed)
                return;

            // Wait 1 second before showing
            await Task.Delay(1000);

            if (_isDisposed || !_service.IsActive)
                return;

            // Must run on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_isDisposed)
                    return;

                // Create and add the easter egg control
                _easterEgg = new EasterEgg { Opacity = 0 };

                // Add it to the host's visual tree
                if (_host is Panel panel)
                {
                    // Add to the beginning so it's behind other content
                    panel.Children.Insert(0, _easterEgg);
                }
                else if (_host is ContentControl contentControl && contentControl.Content == null)
                {
                    contentControl.Content = _easterEgg;
                }
                else if (_host is Decorator decorator)
                {
                    // Store existing child and wrap in a Grid
                    var existingChild = decorator.Child;
                    var grid = new Grid();
                    grid.Children.Add(_easterEgg);
                    if (existingChild != null)
                        grid.Children.Add(existingChild);
                    decorator.Child = grid;
                }

                UpdateVisibility();
            });
        }

        void OnActiveChanged(object? sender, EventArgs e) => UpdateVisibility();

        void UpdateVisibility()
        {
            if (_easterEgg == null || _isDisposed)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (_easterEgg == null || _isDisposed)
                    return;

                var shouldBeVisible = _service.IsActive;
                
                if (shouldBeVisible)
                {
                    _easterEgg.IsVisible = true;
                    
                    if (_easterEgg.Opacity < 1)
                    {
                        // Fade in
                        var animation = new Animation
                        {
                            Duration = TimeSpan.FromSeconds(2),
                            Easing = new QuadraticEaseInOut(),
                            FillMode = FillMode.Forward,
                            Children =
                            {
                                new KeyFrame
                                {
                                    Cue = new Cue(0),
                                    Setters = { new Setter(EasterEgg.OpacityProperty, 0.0) }
                                },
                                new KeyFrame
                                {
                                    Cue = new Cue(1),
                                    Setters = { new Setter(EasterEgg.OpacityProperty, 1.0) }
                                }
                            }
                        };
                        animation.RunAsync(_easterEgg);
                    }
                }
                else
                {
                    if (_easterEgg.Opacity > 0)
                    {
                        // Fade out
                        var animation = new Animation
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            Easing = new QuadraticEaseInOut(),
                            FillMode = FillMode.Forward,
                            Children =
                            {
                                new KeyFrame
                                {
                                    Cue = new Cue(0),
                                    Setters = { new Setter(EasterEgg.OpacityProperty, 1.0) }
                                },
                                new KeyFrame
                                {
                                    Cue = new Cue(1),
                                    Setters = { new Setter(EasterEgg.OpacityProperty, 0.0) }
                                }
                            }
                        };
                        var task = animation.RunAsync(_easterEgg);
                        task.ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (_easterEgg != null && !_isDisposed)
                                    _easterEgg.IsVisible = false;
                            });
                        });
                    }
                    else
                    {
                        _easterEgg.IsVisible = false;
                    }
                }
            });
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _host.AttachedToVisualTree -= OnHostAttachedToVisualTree;
            _service.ActiveChanged -= OnActiveChanged;

            if (_easterEgg == null)
                return;

            // Remove from visual tree
            if (_host is Panel panel)
            {
                panel.Children.Remove(_easterEgg);
            }
            else if (_host is ContentControl contentControl && contentControl.Content == _easterEgg)
            {
                contentControl.Content = null;
            }
            else if (_host is Decorator decorator && decorator.Child is Grid grid)
            {
                // Restore original structure
                Control? originalChild = null;
                foreach (var child in grid.Children)
                {
                    if (child != _easterEgg)
                    {
                        originalChild = child as Control;
                        break;
                    }
                }
                grid.Children.Clear();
                decorator.Child = originalChild;
            }

            _easterEgg = null;
        }
    }
}

