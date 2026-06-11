namespace Mdk.CommandLine.Shared.Api;

/// <summary>
///     The output configuration for a single git branch, declared by an <c>[mdk-branch:&lt;branch&gt;]</c>
///     section in the ini file.
/// </summary>
/// <param name="Pattern">
///     The output folder name pattern to use when packing on this branch. Supports the same macros as
///     <see cref="IParameters.IPackVerbParameters.Macros" /> (notably <c>$MDK_PROJECT$</c> and
///     <c>$MDK_BRANCH$</c>).
/// </param>
/// <param name="Watermark">
///     Whether to stamp a watermark onto the deployed thumbnail for this branch. Defaults to <c>true</c> -
///     a branch redirect implies a non-release build worth marking; set watermark=false to opt out.
/// </param>
/// <param name="WatermarkText">
///     The text to stamp when <paramref name="Watermark" /> is enabled. When null/empty, the branch name is
///     used. Supports macros.
/// </param>
public sealed record BranchOutput(string Pattern, bool Watermark = true, string? WatermarkText = null);
