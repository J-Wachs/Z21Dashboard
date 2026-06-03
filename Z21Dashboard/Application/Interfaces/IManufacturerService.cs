using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

public interface IManufacturerService
{
    /// <summary>
    /// Gets information about a manufacturer.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ManufacturerCollection.Manufacturer?> GetManufacturerAsync(byte id);
}
