using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Mdk.Hub.Views;

public class StyleableWindow: Window
{
    public StyleableWindow()
    {
        this.CloseCommand = new RelayCommand(CloseFromButton, CanCloseFromButton);
        this.MinifyCommand = new RelayCommand(MinifyFromButton, CanMinifyFromButton);
    }
    
    public static readonly StyledProperty<bool> CanMinifyProperty = AvaloniaProperty.Register<StyleableWindow, bool>(nameof(CanMinify), true);
    
    public bool CanMinify
    {
        get => GetValue(CanMinifyProperty);
        set => SetValue(CanMinifyProperty, value);
    }

    bool CanMinifyFromButton() => CanMinify;

    void MinifyFromButton()
    {
        if (!CanMinify) return;
        WindowState = WindowState.Minimized;
    }

    bool CanCloseFromButton()
    {
        this.Hand
    }

    void CloseFromButton()
    {
        throw new System.NotImplementedException();
    }

    public ICommand CloseCommand { get; }
    
    public ICommand MinifyCommand { get; }
}