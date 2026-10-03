using System.Text.Json;
using Z21Dashboard.NmraDccStandard.Interfaces;
using Z21Dashboard.NmraDccStandard.Models;

namespace Z21Dashboard.NmraDccStandard.Services;

public class ManufacturerService : IManufacturerService
{
    /// <inheritdoc />
    public async Task<ManufacturerCollection.Manufacturer?> GetManufacturerAsync(byte id)
    {
#if WINDOWS
        // Open the file from the REssources/Raw folder
        using var stream = await FileSystem.OpenAppPackageFileAsync("NmraManufacturers.json");
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync();

        // Deserialize
        var collection = JsonSerializer.Deserialize<ManufacturerCollection>(json);
        var lookup = collection?.IDs.ToDictionary(x => x.Id, x => new ManufacturerCollection.Manufacturer(x.Id, x.Name, x.Type));

        if (lookup is not null && lookup.TryGetValue(id, out var manufacturer))
        {
            return manufacturer;
        }
#endif
        return null;
    }
}
