namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
///     Represents a configuration value along with which layer it came from.
///     Useful for tracking whether a value came from mdk.ini, mdk.local.ini, or is a default.
/// </summary>
/// <typeparam name="T">The type of the configuration value.</typeparam>
public readonly struct ConfigurationValue<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigurationValue{T}"/> struct.
    /// </summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="source">The source layer from which the value came.</param>
    public ConfigurationValue(T value, SourceLayer source)
    {
        Value = value;
        Source = source;
    }

    /// <summary>
    ///     The actual configuration value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    ///     Which layer this value came from.
    /// </summary>
    public SourceLayer Source { get; }

    /// <summary>
    ///     True if this value was explicitly set (not a default).
    /// </summary>
    public bool IsExplicit => Source != SourceLayer.Default;

    /// <summary>
    ///     Returns a string representation of this configuration value.
    /// </summary>
    /// <returns>A string containing the value and its source.</returns>
    public override string ToString() => $"{Value} ({Source})";
}
