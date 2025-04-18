using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mdk.DocGen3.Features;

namespace Mdk.DocGen3;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var control = Application.Current?.ViewForModel(param.GetType()) ?? throw new InvalidOperationException("No application instance found.");
        control.DataContext = param;
        return control;
    }

    public bool Match(object? data) => data is ViewModelBase;
}