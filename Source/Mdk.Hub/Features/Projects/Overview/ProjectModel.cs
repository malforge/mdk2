using System;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel(ProjectType type, string name, DateTimeOffset lastReferenced) : ProjectListItem
{
    DateTimeOffset _lastReferenced = lastReferenced;
    string _name = name;
    ProjectType _type = type;

    public ProjectType Type
    {
        get => _type;
        private set => SetProperty(ref _type, value);
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public DateTimeOffset LastReferenced
    {
        get => _lastReferenced;
        private set => SetProperty(ref _lastReferenced, value);
    }

    public override bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript)
    {
        if (mustBeMod && Type != ProjectType.Mod)
            return false;
        if (mustBeScript && Type != ProjectType.IngameScript)
            return false;
        if (!string.IsNullOrWhiteSpace(searchText) && !Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}