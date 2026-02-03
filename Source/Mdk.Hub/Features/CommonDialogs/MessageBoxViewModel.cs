using System.Collections.Immutable;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Represents the view model for a message box dialog.
///     Provides properties such as the title, message, and a set of choices for user interaction.
///     Inherits from <see cref="OverlayModel" />.
/// </summary>
[ViewModelFor<MessageBoxView>]
public class MessageBoxViewModel : OverlayModel
{
    readonly ImmutableArray<IMessageBoxChoice> _choices = ImmutableArray<IMessageBoxChoice>.Empty;
    object? _selectedValue;

    /// <summary>
    ///     Gets or initializes the title of the message box dialog.
    ///     Represents the heading displayed to the user in the dialog interface.
    /// </summary>
    public required string? Title { get; init; }

    /// <summary>
    ///     Gets or initializes the message content of the message box dialog.
    ///     Represents the primary text or information displayed to the user in the dialog interface.
    /// </summary>
    public required string? Message { get; init; }

    /// <summary>
    ///     Gets or initializes the collection of choices available in the message box dialog.
    ///     Each choice represents an actionable option that can be selected by the user,
    ///     encapsulated by <see cref="MessageBoxChoice" /> instances.
    /// </summary>
    public required ImmutableArray<IMessageBoxChoice> Choices
    {
        get => _choices;
        init
        {
            if (!_choices.IsDefaultOrEmpty)
            {
                foreach (var choice in _choices)
                    choice.Disconnect();
            }
            _choices = value;
            if (!_choices.IsDefaultOrEmpty)
            {
                foreach (var choice in _choices)
                    choice.Connect(this);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the value representing the user's selected choice from the available options.
    ///     Used to determine the specific option chosen within the message box dialog.
    /// </summary>
    public object? SelectedValue
    {
        get => _selectedValue;
        set => SetProperty(ref _selectedValue, value);
    }
}
