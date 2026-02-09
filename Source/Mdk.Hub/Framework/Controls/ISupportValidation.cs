namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     Interface for controls that provide their own validation state and messages.
///     Controls implementing this interface handle their own error styling.
/// </summary>
public interface ISupportValidation
{
    /// <summary>
    ///     Gets the current validation error message, or null if validation passes.
    /// </summary>
    string? ValidationError { get; }
}
