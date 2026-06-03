namespace Z21Dashboard.Application.Models;

public class LocomotiveGridItemDto
{
    public required string FilePath { get; set; }
    public required short Address { get; set; }
    public required string FileName { get; set; }
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public string? Manufacturer { get; set; }
    public string? Class { get; set; }
    public string? UnitNumber { get; set; }
}
