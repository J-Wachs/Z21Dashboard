using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// The settings that are stored for the dashboard.
/// </summary>
public class DashboardSettingsStorage
{
    public ModelScale Scale { get; set; } = ModelScale.H0;

    public TemperatureScale TemperatureScale { get; set; } = TemperatureScale.Celsius;

    public string? LocalLocoDecoderFolder { get; set; }

    public string? LocalLocomotiveFolder { get; set; }
}

