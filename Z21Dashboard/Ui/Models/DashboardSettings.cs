using Z21Dashboard.Ui.Models;

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

    /// <summary>
    /// Holds the path to the folder where the users definition files for loco decoders
    /// are stored.
    /// </summary>
    public string? LocalLocoDecoderFolder { get; set; }

    /// <summary>
    /// Holds the path to the folder where the users locomotive definitions files
    /// are stored.
    /// </summary>
    public string? LocalLocomotiveFolder { get; set; }

    /// <summary>
    /// Automtic mapping from DashbordSettingsStorage.
    /// </summary>
    /// <param name="storage"></param>
    public static implicit operator DashboardSettings(DashboardSettingsStorage storage)
    {
        return new DashboardSettings
        {
            Scale = storage.Scale,
            TemperatureScale = storage.TemperatureScale,
            LocalLocoDecoderFolder = storage.LocalLocoDecoderFolder,
            LocalLocomotiveFolder = storage.LocalLocomotiveFolder
        };
    }

    /// <summary>
    /// Automatic mapping to DashSettingsStorage.
    /// </summary>
    /// <param name="settings"></param>
    public static implicit operator DashboardSettingsStorage(DashboardSettings settings)
    {
        return new DashboardSettingsStorage
        {
            Scale = settings.Scale,
            TemperatureScale = settings.TemperatureScale,
            LocalLocoDecoderFolder = settings.LocalLocoDecoderFolder,
            LocalLocomotiveFolder= settings.LocalLocomotiveFolder
        };
    }
}
