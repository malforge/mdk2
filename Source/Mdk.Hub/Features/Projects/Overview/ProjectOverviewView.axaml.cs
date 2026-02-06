using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using JetBrains.Annotations;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
///     Main view displaying the list of registered MDK projects.
/// </summary>
[Singleton]
[UsedImplicitly]
public partial class ProjectOverviewView : UserControl
{
    bool _updatingSelection;

    /// <summary>
    ///     Initializes a new instance of the ProjectOverviewView class.
    /// </summary>
    public ProjectOverviewView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ProjectOverviewViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.ShowProjectRequested += OnShowProjectRequested;
            
            // Sync initial selection if one exists
            if (viewModel.SelectedProjects.Length > 0)
                SyncListBoxSelection();
        }
    }
    
    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectOverviewViewModel.SelectedProjects))
        {
            SyncListBoxSelection();
        }
    }
    
    void OnSelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_updatingSelection || DataContext is not ProjectOverviewViewModel viewModel)
            return;
            
        _updatingSelection = true;
        try
        {
            var selected = ProjectListBox.SelectedItems?.Cast<ProjectModel>() ?? Enumerable.Empty<ProjectModel>();
            viewModel.SelectedProjects = selected.ToImmutableArray();
        }
        finally
        {
            _updatingSelection = false;
        }
    }
    
    void SyncListBoxSelection()
    {
        if (_updatingSelection || DataContext is not ProjectOverviewViewModel viewModel || ProjectListBox == null)
            return;
            
        _updatingSelection = true;
        try
        {
            var selected = viewModel.SelectedProjects;
            
            // Single-select mode: set SelectedItem
            if (ProjectListBox.SelectionMode == SelectionMode.Single)
            {
                ProjectListBox.SelectedItem = selected.Length > 0 ? selected[0] : null;
            }
            // Multi-select mode: sync SelectedItems
            else
            {
                ProjectListBox.SelectedItems?.Clear();
                foreach (var item in selected)
                    ProjectListBox.SelectedItems?.Add(item);
            }
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    void OnShowProjectRequested(object? sender, ShowProjectEventArgs e) => ScrollToItem(e.Project);

    void ScrollToItem(ProjectModel item)
    {
        if (ProjectListBox == null)
            return;

        ProjectListBox.ScrollIntoView(item);
    }
}
