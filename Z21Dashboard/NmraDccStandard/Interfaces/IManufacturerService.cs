using Z21Dashboard.NmraDccStandard.Models;

namespace Z21Dashboard.NmraDccStandard.Interfaces;

public interface IManufacturerService
{
    /// <summary>
    /// Gets information about a manufacturer.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ManufacturerCollection.Manufacturer?> GetManufacturerAsync(byte id);
}
