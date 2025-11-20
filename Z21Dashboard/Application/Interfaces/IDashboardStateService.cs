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
    /// Gets the full list of all available dashboard components, including hidden ones,
    /// sorted for display in a settings list.
    /// </summary>
    /// <returns>A list of DashboardComponentState objects.</returns>
    List<DashboardComponentState> GetComponentStates();

    /// <summary>
    /// Gets a filtered list of components that are currently visible,
    /// sorted by their grid position.
    /// </summary>
    /// <returns>An ordered list of visible DashboardComponentState objects.</returns>
    List<DashboardComponentState> GetVisibleComponentStates();

    /// <summary>
    /// Toggles the visibility of a specific dashboard component.
    /// </summary>
    /// <param name="componentId">The unique ID of the component to toggle.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task ToggleVisibility(Guid componentId);

    /// <summary>
    /// Updates and saves the entire dashboard layout, including positions and sizes.
    /// </summary>
    /// <param name="newLayout">A list representing the complete new layout of the dashboard.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task UpdateLayout(List<DashboardComponentState> newLayout);
}