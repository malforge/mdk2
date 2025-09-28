using System;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Shell;

[Dependency<IShell>]
public class Shell(IDependencyContainer container, Lazy<ShellViewModel> lazyViewModel) : IShell
{
    readonly IDependencyContainer _container = container;
    readonly Lazy<ShellViewModel> _viewModel = lazyViewModel;

    public void Start() { }

    public void AddOverlay(OverlayModel model)
    {
        void onDismissed(object? sender, EventArgs e)
        {
            model.Dismissed -= onDismissed;
            _viewModel.Value.OverlayViews.Remove(model);
            if (model is IDisposable disposable) disposable.Dispose();
        }

        model.Dismissed += onDismissed;
        _viewModel.Value.OverlayViews.Add(model);
    }
}