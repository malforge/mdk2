using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

[ViewModelFor<NewProjectDialogView>]
public class NewProjectDialogViewModel : OverlayModel
{
    string _projectName = "";
    string _location = "";
    string _validationError = "";
    string _validationWarning = "";

    public NewProjectDialogViewModel(NewProjectDialogMessage message)
    {
        Message = message;
        
        // Create commands first before setting properties that trigger validation
        _okCommand = new RelayCommand(Ok, CanOk);
        _cancelCommand = new RelayCommand(Cancel);
        
        // Set default project name based on project type
        ProjectName = message.ProjectType == ProjectType.IngameScript 
            ? "MdkScriptProject" 
            : "MdkModProject";
        
        // Now set Location which will trigger validation and NotifyCanExecuteChanged
        Location = message.DefaultLocation;
    }

    public NewProjectDialogMessage Message { get; }
    
    public NewProjectDialogResult? Result { get; private set; }

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

    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    public string ValidationWarning
    {
        get => _validationWarning;
        private set => SetProperty(ref _validationWarning, value);
    }

    readonly RelayCommand _okCommand;
    public ICommand OkCommand => _okCommand;

    readonly RelayCommand _cancelCommand;
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
        {
            ValidationWarning = "Project name contains spaces. This may cause issues with some tools.";
        }
        else if (ProjectName.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.'))
        {
            ValidationWarning = "Project name contains special characters. Consider using only letters, numbers, underscores, hyphens, and periods.";
        }
    }

    bool CanOk()
    {
        return !string.IsNullOrWhiteSpace(ProjectName) 
            && !string.IsNullOrWhiteSpace(Location)
            && string.IsNullOrEmpty(ValidationError);
    }

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
