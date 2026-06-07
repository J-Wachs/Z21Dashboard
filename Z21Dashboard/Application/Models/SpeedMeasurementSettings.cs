using System.ComponentModel.DataAnnotations;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// The settings for the Speed Measurement feature, which allows users to
/// measure the speed of their model trains using two sensors placed at a known distance apart.
/// </summary>
public class SpeedMeasurementSettings
{
    [Required]
    public int Sensor1Module { get; set; } = 1;

    [Required]
    public int Sensor1Port { get; set; } = 1;

    [Required]
    public int Sensor2Module { get; set; } = 1;

    [Required]
    public int Sensor2Port { get; set; } = 2;

    [Required]
    public decimal Distance1To2 { get; set; } = 0.0m;

    [Required]
    public decimal Distance2To1 { get; set; } = 0.0m;
}
