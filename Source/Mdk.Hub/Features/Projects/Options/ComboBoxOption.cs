namespace Mdk.Hub.Features.Projects.Options;

/// <summary>
/// Represents an option in a combo box with a value and display text.
/// </summary>
/// <param name="Value">The underlying value of the option.</param>
/// <param name="Display">The display text shown to the user.</param>
public record ComboBoxOption(string Value, string Display);