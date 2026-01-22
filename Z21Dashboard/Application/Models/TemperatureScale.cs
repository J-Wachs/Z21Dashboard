namespace Z21Dashboard.Application.Models;

/// <summary>
/// Specifies the temperature scales supported for temperature values.
/// </summary>
/// <remarks>Use this enumeration to indicate whether a temperature value is expressed in degrees Celsius or
/// degrees Fahrenheit. This is commonly used in methods or properties that require explicit temperature scale
/// specification to avoid ambiguity.</remarks>
public enum TemperatureScale
{
    Celsius,
    Fahrenheit
}
