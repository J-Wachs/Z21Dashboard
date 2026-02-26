using System.Text.Json;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Services;

public static class ManufacturerService
{
    public static async Task<ManufacturerCollection.Manufacturer?> GetManufacturerAsync(byte id)
    {
#if WINDOWS
        // Åbn fil fra Resources/Raw
        using var stream = await FileSystem.OpenAppPackageFileAsync("NmraManufacturers.json");
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync();

        // Deserialize
        var collection = JsonSerializer.Deserialize<ManufacturerCollection>(json);
        var lookup = collection?.IDs.ToDictionary(x => x.Id, x => new ManufacturerCollection.Manufacturer { Name = x.Name, Type = x.Type });

        if (lookup is not null && lookup.TryGetValue(id, out var manufacturer))
        {
            return manufacturer;
        }
#endif
        return null;
    }
}
