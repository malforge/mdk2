using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mdk.Hub.Framework;

namespace Mdk.Hub;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewModelForAttribute = param.GetType().GetCustomAttribute<ViewModelForAttribute>();
        if (viewModelForAttribute is null)
            throw new InvalidOperationException($"ViewModelForAttribute is missing on {param.GetType().FullName}");

        var viewType = viewModelForAttribute.ViewType;
        var container = App.Container;
        if (!container.TryResolve(viewType, out var viewObj))
            throw new InvalidOperationException($"Could not resolve view of type {viewType.FullName}");
        if (viewObj is not Control view)
            throw new InvalidOperationException($"Resolved view is not a Control. Type: {viewObj.GetType().FullName}");
        view.DataContext = param;
        return view;
    }

    public bool Match(object? data) => data is ViewModel;
}