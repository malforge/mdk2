using System;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
/// Extra options for the minifier which can be used to modify how minification is performed.
/// </summary>
[Flags]
public enum MinifierExtraOptions
{
    /// <summary>
    /// No special options.
    /// </summary>
    None,
    
    /// <summary>
    /// The <see cref="MinifierLevel.Trim"/> level will not remove unused members (fields, properties, methods, constructors).
    /// </summary>
    NoMemberTrimming
}
