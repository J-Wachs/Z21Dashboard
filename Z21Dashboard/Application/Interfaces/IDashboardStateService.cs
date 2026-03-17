using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines a service for managing the state of the user-configurable dashboard,
/// including which components are visible and their position in a grid layout.
/// </summary>
public interface IDashboardStateService
{
    /// <summary>
    /// Event that is triggered when the dashboard layout changes.
    /// </summary>
    event Action? OnLayoutChanged;

    /// <summary>
    /// Occurs when the dashboard settings have changed.
    /// </summary>
    /// <remarks>Subscribe to this event to be notified whenever the dashboard settings are updated. The event
    /// provides the new settings as a parameter to the event handler.</remarks>
    event Action<DashboardSettings>? OnSettingsChanged;

    /// <summary>
    /// Gets the full list of all available dashboard components, including hidden ones,
    /// sorted for display in a settings list.
    /// </summary>
    /// <returns>A list of DashboardComponentState objects.</returns>
    List<DashboardComponentState> GetComponentStates();

    /// <summary>
    /// Gets the Z21Dashboard settings. Please note that some of the settings are set in the 
    /// settings dialog and others are retrieved from Windows (e.g. measure system).
    /// </summary>
    /// <returns>A settings object.</returns>
    Task<DashboardSettings> GetSettings();

    /// <summary>
    /// Gets a filtered list of components that are currently visible,
    /// sorted by their grid position.
    /// </summary>
    /// <returns>An ordered list of visible DashboardComponentState objects.</returns>
    List<DashboardComponentState> GetVisibleComponentStates();

    /// <summary>
    /// Gets a list of components that the user can select to show or hide on the dashboard.
    /// </summary>
    /// <returns>An ordered list of visible DashboardComponentState objects.</returns>
    List<DashboardComponentState> GetUserSelectableComponents();

    /// <summary>
    /// Toggles the visibility of a specific dashboard component.
    /// </summary>
    /// <param name="componentId">The unique ID of the component to toggle.</param>
    /// <returns></returns>
    Task ToggleVisibility(Guid componentId);

    /// <summary>
    /// Updates and saves the entire dashboard layout, including positions and sizes.
    /// </summary>
    /// <param name="newLayout">A list representing the complete new layout of the dashboard.</param>
    /// <returns></returns>
    Task UpdateLayout(List<DashboardComponentState> newLayout);

    /// <summary>
    /// Saves the specified dashboard settings to persistent storage.
    /// </summary>
    /// <param name="dashboardSettings">The dashboard settings to be saved. Cannot be null.</param>
    /// <returns></returns>
    Task SaveSettings(DashboardSettingsStorage dashboardSettings);
}