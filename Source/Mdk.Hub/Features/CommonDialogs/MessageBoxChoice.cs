using System;
using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Represents a selectable choice within a message box dialog.
///     Provides the text to display, associated value, and command execution logic for user interaction.
///     Implements <see cref="IMessageBoxChoice" /> to allow integration with <see cref="MessageBoxViewModel" />.
/// </summary>
public class MessageBoxChoice : IMessageBoxChoice
{
    readonly RelayCommand _command;
    bool _isEnabled = true;

    /// <summary>
    ///     Represents a selectable choice within a message box dialog, providing display text,
    ///     an associated value, and the logic for command execution.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be connected to a <see cref="MessageBoxViewModel" /> to manage
    ///     user interactions and value selection effectively. Implements <see cref="IMessageBoxChoice" />.
    /// </remarks>
    public MessageBoxChoice()
    {
        _command = new RelayCommand(OnSelected, () => IsEnabled);
    }

    /// <summary>
    ///     Gets the view model associated with the message box choice.
    ///     Represents the parent <see cref="MessageBoxViewModel" /> that manages the choice and its interaction logic.
    ///     This property is assigned when the choice is connected to the view model and cleared upon disconnection.
    /// </summary>
    protected MessageBoxViewModel? ViewModel { get; private set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the choice is enabled for interaction.
    ///     Determines if the associated command can be executed and whether the choice is selectable within the message box
    ///     dialog.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            _command.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    ///     Gets or initializes the value associated with the message box choice.
    ///     Represents the outcome or data returned when the choice is selected in the message box interface.
    /// </summary>
    public required object? Value { get; init; }

    /// <summary>
    ///     Gets or initializes the text of the message box choice.
    ///     Represents the label or description displayed to the user for this selectable option.
    /// </summary>
    public required string Text { get; init; }

    ICommand IMessageBoxChoice.Command => _command;

    /// <inheritdoc />
    public bool IsDefault { get; init; }

    /// <inheritdoc />
    public bool IsCancel { get; init; }

    void IMessageBoxChoice.Connect(MessageBoxViewModel model) => OnConnect(model);

    void IMessageBoxChoice.Disconnect() => OnDisconnect();

    /// <summary>
    ///     Establishes a connection between the current choice and the specified <see cref="MessageBoxViewModel" />.
    ///     This method is invoked to associate the choice with its owning view model, allowing it to manage
    ///     user interactions and selections effectively.
    /// </summary>
    /// <param name="model">
    ///     The <see cref="MessageBoxViewModel" /> instance to associate this choice with.
    ///     Must not be null; otherwise, an <see cref="ArgumentNullException" /> is thrown.
    /// </param>
    protected virtual void OnConnect(MessageBoxViewModel model) => ViewModel = model ?? throw new ArgumentNullException(nameof(model));

    /// <summary>
    ///     Disconnects the choice from its associated <see cref="MessageBoxViewModel" />,
    ///     ensuring the choice is no longer linked to a dialog or UI context.
    /// </summary>
    /// <remarks>
    ///     This method must be called when the choice is no longer needed or before disposal
    ///     to prevent unintended interactions or memory retention. Throws
    ///     <see cref="InvalidOperationException" /> if the choice is not connected or has already been disconnected.
    /// </remarks>
    protected virtual void OnDisconnect()
    {
        if (ViewModel == null)
            throw new InvalidOperationException("Cannot disconnect a choice that is not connected or already disconnected.");
        ViewModel = null;
    }

    /// <summary>
    ///     Handles the selection logic when a choice within the message box dialog is selected by the user.
    ///     Updates the selection state and communicates the selection to the associated <see cref="MessageBoxViewModel" />.
    /// </summary>
    /// <remarks>
    ///     This method disables the current choice and, if linked to a <see cref="MessageBoxViewModel" />,
    ///     sets the selected value and dismisses the message box dialog. Can be overridden in derived classes
    ///     to customize the selection behavior.
    /// </remarks>
    protected virtual void OnSelected()
    {
        IsEnabled = false;
        if (ViewModel != null)
        {
            ViewModel.SelectedValue = Value;
            ViewModel.Dismiss();
        }
    }
}