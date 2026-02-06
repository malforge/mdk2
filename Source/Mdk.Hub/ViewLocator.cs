using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Framework;

namespace Mdk.Hub;

/// <summary>
/// Locates and instantiates the appropriate view for a given view model using the ViewModelFor attribute.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Builds a control instance for the specified view model.
    /// </summary>
    /// <param name="param">The view model instance to build a view for.</param>
    /// <returns>A control instance with the view model set as its DataContext, or null if param is null.</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Special case for ToastViewModel - use ToastView
        if (param is ToastViewModel)
            return new ToastView { DataContext = param };

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

    /// <summary>
    /// Determines whether this data template can handle the specified data object.
    /// </summary>
    /// <param name="data">The data object to check.</param>
    /// <returns>True if the data is a ViewModel or ToastViewModel, otherwise false.</returns>
    public bool Match(object? data) => data is ViewModel || data is ToastViewModel;
}
