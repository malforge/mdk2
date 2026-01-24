using System.Collections.Generic;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
/// Service for displaying snackbar notifications with action buttons.
/// </summary>
public interface ISnackbarService
{
    /// <summary>
    /// Shows a snackbar notification with the specified message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="timeout">Timeout in milliseconds. 0 or negative means no timeout.</param>
    void Show(string message, int timeout = 15000);
    
    /// <summary>
    /// Shows a snackbar notification with the specified message and actions.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="actions">Actions the user can take.</param>
    /// <param name="timeout">Timeout in milliseconds. 0 or negative means no timeout.</param>
    void Show(string message, IReadOnlyList<SnackbarAction> actions, int timeout = 15000);
}
