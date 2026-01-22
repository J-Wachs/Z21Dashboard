namespace Z21Dashboard.Application.Models;

public class DashboardSettingsStorage
{
    public ModelScale Scale { get; set; } = ModelScale.H0;

    public TemperatureScale TemperatureScale { get; set; } = TemperatureScale.Celsius;
}

