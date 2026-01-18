using System;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class ProjectInfoAction : ActionItem
{
    public ProjectInfoAction(ProjectModel project)
    {
        Project = project;
        
        // TODO: Replace with actual async file scanning
        LastChanged = DateTimeOffset.Now.AddHours(-3);
        
        // TODO: Replace with actual deployment detection
        IsDeployed = true;
        LastDeployed = IsDeployed ? DateTimeOffset.Now.AddHours(-5) : null;
        
        // TODO: Replace with actual script size calculation (for scripts only)
        if (IsScript)
        {
            // ScriptSizeCharacters = 87543; // Fake: under limit
            ScriptSizeCharacters = 105000; // Fake: over limit for testing
        }
    }

    public ProjectModel Project { get; }

    public bool IsScript => Project.Type == ProjectType.IngameScript;

    public string ProjectTypeName => Project.Type == ProjectType.IngameScript 
        ? "Programmable Block Script" 
        : "Mod";

    /// <summary>
    /// When the project files were last modified (placeholder/fake for now).
    /// </summary>
    public DateTimeOffset LastChanged { get; }

    /// <summary>
    /// Whether the project has been deployed to the output folder (placeholder/fake for now).
    /// </summary>
    public bool IsDeployed { get; }

    /// <summary>
    /// When the project was last deployed (null if not deployed, placeholder/fake for now).
    /// </summary>
    public DateTimeOffset? LastDeployed { get; }

    /// <summary>
    /// Size of the compiled script in characters (null for mods, placeholder/fake for now).
    /// </summary>
    public int? ScriptSizeCharacters { get; }

    /// <summary>
    /// Whether the script exceeds Space Engineers' 100k character limit.
    /// </summary>
    public bool IsScriptTooLarge => ScriptSizeCharacters.HasValue && ScriptSizeCharacters.Value > 100_000;

    public override string? Category => "Project";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Only show if a project is selected
        return selectedProject is ProjectModel;
    }
}
