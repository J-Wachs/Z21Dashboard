using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Shared.Dashboard.SystemWidgets;
using Z21Dashboard.Shared.Dashboard.Widgets;

namespace Z21Dashboard.Services;

public class DashboardStateService : IDashboardStateService
{
    private readonly IAppDataService _appDataService;
    private List<DashboardComponentState> _componentStates = [];
    private const string DashboardLayoutKey = "DashboardLayout";

    public event Action? OnLayoutChanged;

    public DashboardStateService(IAppDataService appDataService)
    {
        _appDataService = appDataService;
        LoadAndMergeState();
    }

    /// <inheritdoc />
    public List<DashboardComponentState> GetComponentStates()
    {
        return [.. _componentStates.OrderBy(s => s.Name)];
    }

    /// <inheritdoc />
    public List<DashboardComponentState> GetVisibleComponentStates()
    {
        return [.. _componentStates.Where(s => s.IsVisible || s.IsSystemComponent).OrderBy(s => s.ZIndex)];
    }

    /// <inheritdoc />
    public List<DashboardComponentState> GetUserSelectableComponents()
    {
        return [.. _componentStates.Where(s => s.IsSystemComponent is false).OrderBy(s => s.Name)];
    }

    /// <inheritdoc />
    public async Task ToggleVisibility(Guid componentId)
    {
        var component = _componentStates.FirstOrDefault(s => s.Id == componentId);
        if (component != null)
        {
            component.IsVisible = !component.IsVisible;
            await SaveStateAsync();
        }
    }

    /// <inheritdoc />
    public async Task UpdateLayout(List<DashboardComponentState> newLayout)
    {
        _componentStates = newLayout;
        await SaveStateAsync();
    }


    private static List<DashboardComponentState> GetDefaultComponentDefinitions()
    {
        // This is the MASTER LIST of component definitions.
        return
        [
            // System widgets. -1 means 0 :-)
            new() { Name = SharedResources.Connection, IsSystemComponent = true, PositionX = -1, Width = 525, ComponentType = typeof(Connection), ComponentTypeName = typeof(Connection).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.About, IsSystemComponent = true, PositionX = 600, Width = 500, ComponentType = typeof(About), ComponentTypeName = typeof(About).AssemblyQualifiedName ?? string.Empty },

            // User selectable widgets
            new() { Name = SharedResources.LocoControl, Width = 300, ComponentType = typeof(LocoControl), ComponentTypeName = typeof(LocoControl).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoControl2, Width = 300, ComponentType = typeof(LocoControl2), ComponentTypeName = typeof(LocoControl2).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoListView, Width = 800, ComponentType = typeof(LocoListView), ComponentTypeName = typeof(LocoListView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoSlotView, Width = 800, ComponentType = typeof(LocoSlotView), ComponentTypeName = typeof(LocoSlotView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.PowerChart, Width = 800, ComponentType = typeof(PowerChart), ComponentTypeName = typeof(PowerChart).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.RailComView, Width = 500, ComponentType = typeof(RailComView), ComponentTypeName = typeof(RailComView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.RBusView, Width = 530, ComponentType = typeof(RBusView), ComponentTypeName = typeof(RBusView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.ShowLogView, Width = 500, Height = 500, ComponentType = typeof(ShowLogView), ComponentTypeName = typeof(ShowLogView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.SystemStateView, Width = 260, ComponentType = typeof(SystemStateView), ComponentTypeName = typeof(SystemStateView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.SystemStateFullView, Width = 1100, ComponentType = typeof(SystemStateFullView), ComponentTypeName = typeof(SystemStateFullView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.TurnoutListView, Width = 450, ComponentType = typeof(TurnoutListView), ComponentTypeName = typeof(TurnoutListView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.TurnoutProtocolSelector, Width = 250, ComponentType = typeof(TurnoutProtocolSelector), ComponentTypeName = typeof(TurnoutProtocolSelector).AssemblyQualifiedName ?? string.Empty }
        ];
    }

    private void LoadAndMergeState()
    {
        // Load the lightweight storage objects from the JSON file.
        var storedState = _appDataService.GetData<List<DashboardComponentStorage>>(DashboardLayoutKey);
        var defaultDefinitions = GetDefaultComponentDefinitions();

        if (storedState == null || storedState.Count == 0)
        {
            // First run: Use default definitions, assign positions, and save.
            int yPos = 130;
            foreach (var comp in defaultDefinitions)
            {
                if (comp.IsSystemComponent)
                {
                    if (comp.PositionX is -1)
                    {
                        comp.PositionX = 0;
                    }
                }
                else
                {
                    comp.PositionX = 10;
                    comp.PositionY = yPos;
                    yPos += 50;
                }

            }
            _componentStates = defaultDefinitions;
            _ = SaveStateAsync();
        }
        else
        {
            // Existing user: Merge saved layout with current definitions.
            var mergedState = new List<DashboardComponentState>();

            foreach (var defaultComp in defaultDefinitions)
            {
                var userComp = storedState.FirstOrDefault(s => s.ComponentTypeName == defaultComp.ComponentTypeName);

                if (userComp != null)
                {
                    // Component exists: merge properties.
                    mergedState.Add(new DashboardComponentState
                    {
                        Name = defaultComp.Name,
                        Width = defaultComp.Width,
                        Height = defaultComp.Height,
                        ComponentTypeName = defaultComp.ComponentTypeName,
                        IsSystemComponent = defaultComp.IsSystemComponent,

                        Id = userComp.Id,
                        IsVisible = userComp.IsVisible,
                        PositionX = userComp.PositionX,
                        PositionY = userComp.PositionY,
                        ZIndex = userComp.ZIndex
                    });
                }
                else
                {
                    // New component: Add it with default layout.

                    defaultComp.PositionX = defaultComp.PositionX switch
                    {
                        -1 => 0,
                        0 => 10,
                        _ => defaultComp.PositionX
                    };

                    if (defaultComp.PositionY is 0)
                    {
                        defaultComp.PositionY = 10;
                    }
                    mergedState.Add(defaultComp);
                }
            }

            _componentStates = mergedState;

            // If the number of components differs, it means we added new ones, so we must save.
            if (_componentStates.Count != storedState.Count)
            {
                _ = SaveStateAsync();
            }
        }

        // Final step: Convert type names to Type objects.
        foreach (var state in _componentStates)
        {
            state.ComponentType = Type.GetType(state.ComponentTypeName);
        }
        _componentStates.RemoveAll(s => s.ComponentType == null);
    }

    private async Task SaveStateAsync()
    {
        // Convert the full state objects to lightweight storage objects before saving.
        var stateToStore = _componentStates.Select(s => new DashboardComponentStorage
        {
            Id = s.Id,
            ComponentTypeName = s.ComponentTypeName,
            PositionX = s.PositionX,
            PositionY = s.PositionY,
            IsVisible = s.IsVisible,
            ZIndex = s.ZIndex
        }).ToList();

        _appDataService.SaveData(DashboardLayoutKey, stateToStore);
        OnLayoutChanged?.Invoke();
        await Task.CompletedTask;
    }

}
