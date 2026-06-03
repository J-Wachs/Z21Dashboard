namespace Z21Dashboard.Application.Models;

public class ManufacturerCollection
{
    public List<Manufacturer> IDs { get; set; } = [];
    public record Manufacturer(
        int Id,
        string Name,
        string Type
    );
}
