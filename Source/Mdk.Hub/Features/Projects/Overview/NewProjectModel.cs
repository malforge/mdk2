namespace Mdk.Hub.Features.Projects.Overview;

public class NewProjectModel : ProjectListItem
{
    bool _canMakeMod;
    bool _canMakeScript;

    public bool CanMakeScript
    {
        get => _canMakeScript;
        set => SetProperty(ref _canMakeScript, value);
    }

    public bool CanMakeMod
    {
        get => _canMakeMod;
        set => SetProperty(ref _canMakeMod, value);
    }

    public override bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript)
    {
        CanMakeScript = !mustBeMod;
        CanMakeMod = !mustBeScript;
        return true;
    }
}