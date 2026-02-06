using System.IO;
using System.Linq;
using System.Windows.Input;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

/// <summary>
///     View model for the new project creation dialog.
/// </summary>
[ViewModelFor<NewProjectDialogView>]
public class NewProjectDialogViewModel : OverlayModel
{
    readonly RelayCommand _cancelCommand;

    readonly RelayCommand _okCommand;
    string _location = "";
    string _projectName = "";
    string _validationError = "";
    string _validationWarning = "";

    /// <summary>
    ///     Initializes a new instance of the NewProjectDialogViewModel class.
    /// </summary>
    /// <param name="message">Message containing project type and default location.</param>
    public NewProjectDialogViewModel(NewProjectDialogMessage message)
    {
        Message = message;

        // Create commands first before setting properties that trigger validation
        _okCommand = new RelayCommand(Ok, CanOk);
        _cancelCommand = new RelayCommand(Cancel);

        // Set default project name based on project type
        ProjectName = message.ProjectType == ProjectType.ProgrammableBlock
            ? "MdkScriptProject"
            : "MdkModProject";

        // Now set Location which will trigger validation and NotifyCanExecuteChanged
        Location = message.DefaultLocation;
    }

    /// <summary>
    ///     Gets the message containing project creation parameters.
    /// </summary>
    public NewProjectDialogMessage Message { get; }

    /// <summary>
    ///     Gets the result of the dialog after user action.
    /// </summary>
    public NewProjectDialogResult? Result { get; private set; }

    /// <summary>
    ///     Gets or sets the name of the project to create.
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (SetProperty(ref _projectName, value))
            {
                OnPropertyChanged(nameof(FinalPath));
                ValidateInput();
                _okCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the parent directory where the project folder will be created.
    /// </summary>
    public string Location
    {
        get => _location;
        set
        {
            if (SetProperty(ref _location, value))
            {
                OnPropertyChanged(nameof(FinalPath));
                ValidateInput();
                _okCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    ///     Gets the full path where the project will be created (Location + ProjectName).
    /// </summary>
    public string FinalPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(Location))
                return "";

            try
            {
                return Path.Combine(Location, ProjectName);
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>
    ///     Gets the validation error message (empty if valid).
    /// </summary>
    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    /// <summary>
    ///     Gets the validation warning message (informational, doesn't block creation).
    /// </summary>
    public string ValidationWarning
    {
        get => _validationWarning;
        private set => SetProperty(ref _validationWarning, value);
    }

    /// <summary>
    ///     Gets the command to create the project.
    /// </summary>
    public ICommand OkCommand => _okCommand;
    
    /// <summary>
    ///     Gets the command to cancel project creation.
    /// </summary>
    public ICommand CancelCommand => _cancelCommand;

    void ValidateInput()
    {
        // Clear previous error and warning
        ValidationError = "";
        ValidationWarning = "";

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            ValidationError = "Project name is required.";
            return;
        }

        // Check for invalid path characters in project name
        if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ValidationError = "Project name contains invalid characters.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            ValidationError = "Location is required.";
            return;
        }

        if (!Directory.Exists(Location))
        {
            ValidationError = "Selected location does not exist.";
            return;
        }

        // Check if project folder would already exist
        var projectPath = Path.Combine(Location, ProjectName);
        if (Directory.Exists(projectPath))
        {
            ValidationError = "A folder with this name already exists in the selected location.";
            return;
        }

        // Warnings (only show if no errors)
        if (ProjectName.Contains(' '))
            ValidationWarning = "Project name contains spaces. This may cause issues with some tools.";
        else if (ProjectName.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.'))
            ValidationWarning = "Project name contains special characters. Consider using only letters, numbers, underscores, hyphens, and periods.";
    }

    bool CanOk() =>
        !string.IsNullOrWhiteSpace(ProjectName)
        && !string.IsNullOrWhiteSpace(Location)
        && string.IsNullOrEmpty(ValidationError);

    void Ok()
    {
        ValidateInput();
        if (!CanOk())
            return;

        Result = new NewProjectDialogResult
        {
            ProjectName = ProjectName.Trim(),
            Location = Location.Trim()
        };

        Dismiss();
    }

    void Cancel()
    {
        Result = null;
        Dismiss();
    }
}

public readonly struct NewProjectDialogMessage
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required ProjectType ProjectType { get; init; }
    public string DefaultLocation { get; init; }
    public string OkText { get; init; }
    public string CancelText { get; init; }
}

public readonly struct NewProjectDialogResult
{
    public required string ProjectName { get; init; }
    public required string Location { get; init; }
}
