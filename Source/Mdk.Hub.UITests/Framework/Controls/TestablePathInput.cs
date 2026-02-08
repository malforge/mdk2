using Mdk.Hub.Framework.Controls;

namespace Mdk.Hub.UITests.Framework.Controls;

/// <summary>
///     Test-specific PathInput that allows overriding platform detection.
///     This enables testing platform-specific validation logic regardless of the OS running the tests.
/// </summary>
public class TestablePathInput : PathInput
{
    bool? _forcedPlatform;

    /// <summary>
    ///     Forces the control to behave as if running on Windows (true) or Unix (false).
    ///     If null, uses actual platform.
    /// </summary>
    public bool? ForcedPlatform
    {
        get => _forcedPlatform;
        set => _forcedPlatform = value;
    }

    protected override bool IsWindowsPlatform()
    {
        return _forcedPlatform ?? base.IsWindowsPlatform();
    }
}
