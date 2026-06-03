namespace Z21Dashboard.Application.Models;

/// <summary>
/// Record to represent information about a decoder manufacturer, including the manufacturer code, name, and version number.
/// </summary>
/// <param name="ManufacturerCode"></param>
/// <param name="ManufacturerName"></param>
/// <param name="VersionNumber"></param>
public record DecoderManufacturerInfo(byte ManufacturerCode, string ManufacturerName, byte VersionNumber);
