namespace Z21Dashboard.NmraDccStandard.Models;

public class ManufacturerCollection
{
    public List<Manufacturer> IDs { get; set; } = [];
    public record Manufacturer(
        int Id,
        string Name,
        string Type
    );
}
