using System.ComponentModel.DataAnnotations;

namespace Z21Dashboard.Application.Models;

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
    public ModelScale Scale { get; set; } = ModelScale.H0;

    [Required]
    public decimal Distance1To2 { get; set; } = 0.0m;

    [Required]
    public decimal Distance2To1 { get; set; } = 0.0m;
}
