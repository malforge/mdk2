namespace Mdk.Hub;

/// <summary>
///     Centralized metadata about the MDK environment, including repository URLs, package names, and API documentation links.
///     All hardcoded environment-specific values should be defined here.
/// </summary>
public static class EnvironmentMetadata
{
    // GitHub Repository
    public const string GitHubOwner = "malforge";
    public const string GitHubRepo = "mdk2";
    public const string GitHubRepoUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}";
    public const string GitHubRawContentBaseUrl = $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepo}/main";
    
    // NuGet Packages
    public const string NuGetApiBaseUrl = "https://api.nuget.org/v3-flatcontainer";
    public const string TemplatePackageId = "Mal.Mdk2.ScriptTemplates";
    public const string PbAnalyzersPackageId = "Mal.Mdk2.PbAnalyzers";
    public const string PbPackagerPackageId = "Mal.Mdk2.PbPackager";
    public const string ModAnalyzersPackageId = "Mal.Mdk2.ModAnalyzers";
    public const string ModPackagerPackageId = "Mal.Mdk2.ModPackager";
    public const string ReferencesPackageId = "Mal.Mdk2.References";
    
    // Package prefix for detecting MDK packages
    public const string PackagePrefix = "Mal.Mdk2.";
    
    // API Documentation
    public const string ModApiDocsUrl = "https://malforge.github.io/spaceengineers/modapi/";
    public const string PbApiDocsUrl = "https://malforge.github.io/spaceengineers/pbapi/";
    
    // All MDK package IDs
    public static readonly string[] AllPackageIds =
    [
        PbAnalyzersPackageId,
        PbPackagerPackageId,
        ModAnalyzersPackageId,
        ModPackagerPackageId,
        ReferencesPackageId
    ];
}
