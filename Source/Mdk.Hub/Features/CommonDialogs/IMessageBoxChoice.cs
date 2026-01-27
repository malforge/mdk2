using System;
using System.Windows.Input;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Defines the interface for a choice presented in a message box dialog.
///     Provides a textual representation and an associated command for interacting with user selections.
///     Includes support for connecting the choice to a specific <see cref="MessageBoxViewModel" /> instance.
/// </summary>
public interface IMessageBoxChoice
{
    /// <summary>
    ///     Gets the text representing the label or description of an option in a message box dialog.
    ///     This property is used to display the content of an interactive choice to the user.
    /// </summary>
    string Text { get; }

    /// <summary>
    ///     Gets the command associated with a specific action or selection within a message box dialog.
    ///     This command encapsulates the logic to execute when the corresponding choice is triggered by the user.
    /// </summary>
    ICommand? Command { get; }

    /// <summary>
    ///     Indicates whether this choice is the default option in a message box dialog.
    ///     When set to true, this property designates the choice as the default action
    ///     triggered when the user presses the Enter key.
    /// </summary>
    bool IsDefault { get; }

    /// <summary>
    ///     Indicates whether the choice represents a cancel action in a message box dialog.
    ///     If set to true, selecting this choice triggers the cancellation of the dialog action.
    /// </summary>
    bool IsCancel { get; }

    /// <summary>
    ///     Connects the specified <see cref="MessageBoxViewModel" /> to the current choice,
    ///     enabling interaction and value selection in the message box dialog.
    /// </summary>
    /// <param name="model">The <see cref="MessageBoxViewModel" /> instance to associate with this choice.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="model" /> is null.</exception>
    void Connect(MessageBoxViewModel model);

    /// <summary>
    ///     Disconnects the current <see cref="MessageBoxChoice" /> from the associated <see cref="MessageBoxViewModel" />,
    ///     effectively disabling interaction and value selection for this choice in the message box dialog.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when attempting to disconnect a choice that is not currently connected
    ///     or has already been disconnected.
    /// </exception>
    void Disconnect();
}