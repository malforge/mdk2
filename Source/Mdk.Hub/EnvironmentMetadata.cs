namespace Mdk.Hub;

/// <summary>
///     Centralized metadata about the MDK environment, including repository URLs, package names, and API documentation links.
///     All hardcoded environment-specific values should be defined here.
/// </summary>
public static class EnvironmentMetadata
{
    /// <summary>
    ///     The GitHub owner/organization name for the MDK repository.
    /// </summary>
    public const string GitHubOwner = "malforge";
    
    /// <summary>
    ///     The GitHub repository name for MDK.
    /// </summary>
    public const string GitHubRepo = "mdk2";
    
    /// <summary>
    ///     The full URL to the MDK GitHub repository.
    /// </summary>
    public const string GitHubRepoUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}";
    
    /// <summary>
    ///     The base URL for accessing raw file content from the main branch of the MDK repository.
    /// </summary>
    public const string GitHubRawContentBaseUrl = $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepo}/main";
    
    /// <summary>
    ///     The base URL for NuGet API v3 flat container operations.
    /// </summary>
    public const string NuGetApiBaseUrl = "https://api.nuget.org/v3-flatcontainer";
    
    /// <summary>
    ///     The package ID for MDK script templates.
    /// </summary>
    public const string TemplatePackageId = "Mal.Mdk2.ScriptTemplates";
    
    /// <summary>
    ///     The package ID for Programmable Block analyzers.
    /// </summary>
    public const string PbAnalyzersPackageId = "Mal.Mdk2.PbAnalyzers";
    
    /// <summary>
    ///     The package ID for Programmable Block packager.
    /// </summary>
    public const string PbPackagerPackageId = "Mal.Mdk2.PbPackager";
    
    /// <summary>
    ///     The package ID for mod analyzers.
    /// </summary>
    public const string ModAnalyzersPackageId = "Mal.Mdk2.ModAnalyzers";
    
    /// <summary>
    ///     The package ID for mod packager.
    /// </summary>
    public const string ModPackagerPackageId = "Mal.Mdk2.ModPackager";
    
    /// <summary>
    ///     The package ID for Space Engineers API references.
    /// </summary>
    public const string ReferencesPackageId = "Mal.Mdk2.References";
    
    /// <summary>
    ///     The package prefix used to identify all MDK packages.
    /// </summary>
    public const string PackagePrefix = "Mal.Mdk2.";
    
    /// <summary>
    ///     The URL for mod API documentation.
    /// </summary>
    public const string ModApiDocsUrl = "https://malforge.github.io/spaceengineers/modapi/";
    
    /// <summary>
    ///     The URL for Programmable Block API documentation.
    /// </summary>
    public const string PbApiDocsUrl = "https://malforge.github.io/spaceengineers/pbapi/";
    
    /// <summary>
    ///     Collection of all MDK package IDs for dependency management.
    /// </summary>
    public static readonly string[] AllPackageIds =
    [
        PbAnalyzersPackageId,
        PbPackagerPackageId,
        ModAnalyzersPackageId,
        ModPackagerPackageId,
        ReferencesPackageId
    ];
}

