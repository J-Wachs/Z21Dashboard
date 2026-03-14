using System.Globalization;
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
    public event Action<DashboardSettings>? OnSettingsChanged;

    private RegionInfo? _regionInfo;

    public DashboardStateService(IAppDataService appDataService)
    {
        var cultureInfo = CultureInfo.CurrentUICulture;
        _regionInfo = new RegionInfo(cultureInfo.Name);

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

    /// <inheritdoc  />
    public async Task<DashboardSettings> GetSettings()
    {
        var settings = _appDataService.GetData<DashboardSettingsStorage>("DashboardSettings");
        if (settings is null)
        {
            settings = new()
            {
                TemperatureScale = GetRegionTemperatureScale()
            };
        }
        DashboardSettings fullSettings = settings;
        fullSettings.UnitSystem = _regionInfo is null || _regionInfo.IsMetric ? MeasurementUnitSystem.Metric : MeasurementUnitSystem.Imperial;
        return fullSettings;
    }

    /// <inheritdoc />
    public async Task SaveSettings(DashboardSettingsStorage dashboardSettings)
    {
        _appDataService.SaveData("DashboardSettings", dashboardSettings);
        OnSettingsChanged?.Invoke(dashboardSettings);
        await Task.CompletedTask;
    }


    /// <summary>
    /// Retrieves the default set of dashboard component definitions used to initialize the dashboard layout.
    /// The list build in this method, is the master list of widgets in the Z21Dashboard.
    /// </summary>
    /// <remarks>The returned list includes both system components and user-selectable widgets, each with
    /// predefined properties such as name, size, and component type. The order and configuration of these components
    /// determine the initial dashboard layout.</remarks>
    /// <returns>A list of <see cref="DashboardComponentState"/> objects representing the default configuration of system and
    /// user-selectable dashboard components.</returns>
    private static List<DashboardComponentState> GetDefaultComponentDefinitions()
    {
        // This is the MASTER LIST of component definitions.
        return
        [
            // System widgets. -1 means 0 because 0 will result in the X will be set to other value :-)
            new() { Name = SharedResources.Connection, IsSystemComponent = true, PositionX = -1, Width = 525, ComponentType = typeof(Connection), ComponentTypeName = typeof(Connection).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.About, IsSystemComponent = true, PositionX = 600, Width = 500, ComponentType = typeof(About), ComponentTypeName = typeof(About).AssemblyQualifiedName ?? string.Empty },

            // User selectable widgets
            new() { Name = SharedResources.LocoControl, Width = 300, ComponentType = typeof(LocoControl), ComponentTypeName = typeof(LocoControl).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoControl2, Width = 300, ComponentType = typeof(LocoControl2), ComponentTypeName = typeof(LocoControl2).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoControl3, Width = 300, ComponentType = typeof(LocoControl3), ComponentTypeName = typeof(LocoControl3).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoControl4, Width = 300, ComponentType = typeof(LocoControl4), ComponentTypeName = typeof(LocoControl4).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoListView, Width = 800, ComponentType = typeof(LocoListView), ComponentTypeName = typeof(LocoListView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.LocoSlotView, Width = 800, ComponentType = typeof(LocoSlotView), ComponentTypeName = typeof(LocoSlotView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.PowerChart, Width = 800, ComponentType = typeof(PowerChart), ComponentTypeName = typeof(PowerChart).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.RailComView, Width = 500, ComponentType = typeof(RailComView), ComponentTypeName = typeof(RailComView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.RBusView, Width = 530, ComponentType = typeof(RBusView), ComponentTypeName = typeof(RBusView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.ShowLogView, Width = 500, Height = 500, ComponentType = typeof(ShowLogView), ComponentTypeName = typeof(ShowLogView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.SpeedMeasurement, Width = 275, ComponentType = typeof(SpeedMeasure), ComponentTypeName = typeof(SpeedMeasure).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.SystemStateView, Width = 260, ComponentType = typeof(SystemStateView), ComponentTypeName = typeof(SystemStateView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.SystemStateFullView, Width = 1100, ComponentType = typeof(SystemStateFullView), ComponentTypeName = typeof(SystemStateFullView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.TurnoutListView, Width = 450, ComponentType = typeof(TurnoutListView), ComponentTypeName = typeof(TurnoutListView).AssemblyQualifiedName ?? string.Empty },
            new() { Name = SharedResources.TurnoutProtocolSelector, Width = 250, ComponentType = typeof(TurnoutProtocolSelector), ComponentTypeName = typeof(TurnoutProtocolSelector).AssemblyQualifiedName ?? string.Empty },
        ];
    }

    /// <summary>
    /// Loads the dashboard component state from persistent storage and merges it with the current default component
    /// definitions. This ensures that new widgets gets visible to the user.
    /// </summary>
    /// <remarks>This method ensures that the dashboard layout reflects both the user's saved configuration
    /// and any new or updated default components. If no saved state exists, it initializes the layout with default
    /// positions and persists the state. If new components are added or removed, the method updates the stored state
    /// accordingly. Components with invalid type information are excluded from the final state.</remarks>
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

    /// <summary>
    /// Saves the state to persistent storage and issues en event that the 
    /// layout has changed.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Get the most likely temperature scale for the region.
    /// </summary>
    /// <returns></returns>
    private TemperatureScale GetRegionTemperatureScale()
    {
        if (_regionInfo is not null)
        {
            return _regionInfo.TwoLetterISORegionName switch
            {
                "US" or "LR" or "MM" => TemperatureScale.Fahrenheit,
                _ => TemperatureScale.Celsius,
            };
        }
        return TemperatureScale.Celsius;
    }
}
