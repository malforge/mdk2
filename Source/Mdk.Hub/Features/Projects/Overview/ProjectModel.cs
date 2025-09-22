using System;
using Mal.DependencyInjection;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel(ProjectType type, string name, DateTimeOffset lastReferenced) : ViewModel
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
}