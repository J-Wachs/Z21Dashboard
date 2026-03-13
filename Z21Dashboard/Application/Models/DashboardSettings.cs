namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents user-configurable settings for the dashboard, including temperature scale, model scale, and measurement
/// unit system.
/// 
/// Please note, that some of the settings can be set in the settings dialog, and other are retrieved from Windows.
/// </summary>
/// <remarks>This class provides a container for dashboard preferences and supports implicit conversion to and
/// from a corresponding storage type. Use this type to manage and persist user settings related to display and
/// measurement options.</remarks>
public class DashboardSettings
{
    public TemperatureScale TemperatureScale { get; set; } = TemperatureScale.Celsius;

    public ModelScale Scale { get; set; } = ModelScale.H0;

    public MeasurementUnitSystem UnitSystem { get; set; } = MeasurementUnitSystem.Metric;

    public static implicit operator DashboardSettings(DashboardSettingsStorage storage)
    {
        return new DashboardSettings
        {
            Scale = storage.Scale,
            TemperatureScale = storage.TemperatureScale
        };
    }

    public static implicit operator DashboardSettingsStorage(DashboardSettings settings)
    {
        return new DashboardSettingsStorage
        {
            Scale = settings.Scale,
            TemperatureScale = settings.TemperatureScale
        };
    }
}
