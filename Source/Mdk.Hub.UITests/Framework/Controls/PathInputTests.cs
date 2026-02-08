using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mdk.Hub.Framework.Controls;

namespace Mdk.Hub.UITests.Framework.Controls;

[TestFixture]
public class PathInputTests
{
    [AvaloniaTest]
    public void PathInput_InitializesWithEmptyPath()
    {
        var control = new PathInput();
        var window = new Window { Content = control };
        window.Show();
        
        // Flush dispatcher queue to ensure layout completes
        Dispatcher.UIThread.RunJobs();
        
        Assert.That(control.Path, Is.EqualTo(string.Empty));
        Assert.That(control.HasError, Is.False);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_AcceptsEmptyPathWhenNoDefault()
    {
        // Empty path is valid only when there's no default specified
        var control = new PathInput(); // No DefaultPath set
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        control.Path = string.Empty;
        Assert.That(control.HasError, Is.False);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_RejectsEmptyPathWhenDefaultExists()
    {
        // Empty path should be invalid when a default is available
        var control = new PathInput { DefaultPath = "C:\\default" };
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null);
        
        // Clear the text
        textBox!.Text = "";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.True);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_RejectsInvalidCharacters()
    {
        var control = new PathInput();
        var window = new Window { Content = control };
        window.Show();
        
        // Force template application
        control.ApplyTemplate();
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Simulate typing invalid characters
        textBox!.Text = "C:\\test<>file";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.True);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_NormalizesWindowsPathOnCommit()
    {
        var control = new TestablePathInput { ForcedPlatform = true }; // Force Windows behavior
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Type path with forward slashes and extra spaces
        textBox!.Text = "C:/Program Files///Game  ";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        // Should not error (normalized version is valid)
        Assert.That(control.HasError, Is.False);
        
        // Simulate losing focus (commit)
        textBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
        
        // Path should be normalized
        Assert.That(control.Path, Is.EqualTo("C:\\Program Files\\Game"));
        Assert.That(textBox.Text, Is.EqualTo("C:\\Program Files\\Game"));
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_NormalizesUnixPathOnCommit()
    {
        var control = new TestablePathInput { ForcedPlatform = false }; // Force Unix behavior
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Type path with extra slashes
        textBox!.Text = "/home/user///documents/  ";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.False);
        
        textBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
        
        Assert.That(control.Path, Is.EqualTo("/home/user/documents"));
        Assert.That(textBox.Text, Is.EqualTo("/home/user/documents"));
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_WindowsRejectsReservedNames()
    {
        var control = new TestablePathInput { ForcedPlatform = true }; // Force Windows behavior
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Test reserved name
        textBox!.Text = "C:\\CON\\test";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.True);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_WindowsRejectsMultipleColons()
    {
        var control = new TestablePathInput { ForcedPlatform = true }; // Force Windows behavior
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Colon only valid at position 1
        textBox!.Text = "C:\\test:file";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.True);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_AllowsDefaultValueWithoutValidation()
    {
        // Sentinel values like "auto" should be allowed as defaults even if not valid paths
        var control = new PathInput { DefaultPath = "auto" };
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Type the default value
        textBox!.Text = "auto";
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        // Should not error even though "auto" isn't a valid path format
        Assert.That(control.HasError, Is.False);
        
        // Commit should work
        textBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
        Assert.That(control.Path, Is.EqualTo("auto"));
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_ResetButtonRestoresDefault()
    {
        var control = new PathInput
        {
            Path = "C:\\custom\\path",
            DefaultPath = "C:\\default",
            CanReset = true
        };
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var resetButton = control.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => b.Name == "PART_ResetButton");
        
        Assert.That(resetButton, Is.Not.Null, "Reset button not found in template");
        
        // Reset button should be enabled (path != default)
        Assert.That(control.CanResetToDefault, Is.True);
        Assert.That(resetButton!.IsEnabled, Is.True);
        
        // Click reset
        resetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        
        // Path should be reset
        Assert.That(control.Path, Is.EqualTo("C:\\default"));
        
        // Reset button should now be disabled
        Assert.That(control.CanResetToDefault, Is.False);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_ResetButtonHiddenWhenCanResetFalse()
    {
        var control = new PathInput { CanReset = false };
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var resetButton = control.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => b.Name == "PART_ResetButton");
        
        Assert.That(resetButton, Is.Not.Null, "Reset button not found in template");
        Assert.That(resetButton!.IsVisible, Is.False);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_CommitsOnEnterKey()
    {
        var control = new PathInput();
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Focus the textbox
        textBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        
        // Type a path
        textBox.Text = "C:\\test\\path";
        
        // Simulate Enter key on the textbox
        textBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Enter
        });
        
        Dispatcher.UIThread.RunJobs();
        
        // Path should be committed
        Assert.That(control.Path, Is.EqualTo("C:\\test\\path"));
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_RejectsPathTooLong()
    {
        var control = new PathInput();
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Create a path longer than 4096 chars
        var longPath = "C:\\" + new string('a', 5000);
        textBox!.Text = longPath;
        textBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        
        Assert.That(control.HasError, Is.True);
        
        window.Close();
    }

    [AvaloniaTest]
    public void PathInput_UpdatesTextBoxWhenPathSetExternally()
    {
        var control = new PathInput();
        var window = new Window { Content = control };
        window.Show();
        
        Dispatcher.UIThread.RunJobs();
        
        var textBox = control.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox");
        
        Assert.That(textBox, Is.Not.Null, "TextBox not found in template");
        
        // Set path from outside (e.g., ViewModel binding)
        control.Path = "C:\\external\\path";
        
        // TextBox should reflect the change
        Assert.That(textBox!.Text, Is.EqualTo("C:\\external\\path"));
        
        window.Close();
    }
}
