using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Internal utility class for MDK script template updates via dotnet CLI.
///     Cross-platform: dotnet CLI works identically on Windows and Linux.
/// </summary>
internal class TemplateUpdater
{
    readonly ILogger _logger;

    public TemplateUpdater(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Updates the MDK script templates to the latest version.
    /// </summary>
    public async Task<UpdateResult> UpdateAsync(TemplateVersionInfo versionInfo, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(new UpdateProgress { Message = "Updating templates...", PercentComplete = 0 });

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new install {EnvironmentMetadata.TemplatePackageId} --force",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            progress?.Report(new UpdateProgress { Message = "Installing template package...", PercentComplete = 30 });

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Error("Failed to start dotnet process for template update");
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start template update process"
                };
            }

            progress?.Report(new UpdateProgress { Message = "Waiting for installation...", PercentComplete = 60 });

            await process.WaitForExitAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("Template update cancelled");
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Process may have already exited
                }
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Update cancelled by user"
                };
            }

            progress?.Report(new UpdateProgress { Message = "Finalizing...", PercentComplete = 90 });

            if (process.ExitCode == 0)
            {
                _logger.Info($"Templates updated successfully to version {versionInfo.LatestVersion}");
                return new UpdateResult
                {
                    Success = true,
                    UpdatedItems = new[] { $"Template package {EnvironmentMetadata.TemplatePackageId}" }
                };
            }

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            _logger.Error($"Template update failed with exit code {process.ExitCode}: {error}");
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = $"Template update failed: {error}"
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Template update cancelled");
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Update cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.Error("Template update failed", ex);
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }
}

