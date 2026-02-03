using System;
using Velopack.Logging;

namespace Mdk.Hub.Features.Diagnostics;

/// <summary>
///     Adapter that forwards Velopack logging to our ILogger implementation.
/// </summary>
internal class VelopackLoggerAdapter(ILogger hubLogger) : IVelopackLogger
{
    readonly ILogger _hubLogger = hubLogger;

    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        var msg = $"[Velopack] {message ?? "(no message)"}";

        // Map Velopack's log levels to our log levels
        switch (logLevel)
        {
            case VelopackLogLevel.Information:
                _hubLogger.Info(msg);
                break;
            case VelopackLogLevel.Warning:
                _hubLogger.Warning(msg);
                break;
            case VelopackLogLevel.Error:
            case VelopackLogLevel.Critical:
                if (exception != null)
                    _hubLogger.Error(msg, exception);
                else
                    _hubLogger.Error(msg);
                break;
            case VelopackLogLevel.Trace:
            case VelopackLogLevel.Debug:
            default:
                _hubLogger.Debug(msg);
                break;
        }
    }
}
