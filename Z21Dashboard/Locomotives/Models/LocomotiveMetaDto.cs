namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// The locomotive meta data.
/// </summary>
public class LocomotiveMetaDto
{
    public required string Name { get; set; }

    public string? ShortName { get; set; }

    public string? Manufacturer { get; set; }

    public string? CatalogueNumber { get; set; }

    public string? Class { get; set; }

    public string? UnitNumber { get; set; }

    public string? Description { get; set; }

    public DateOnly? Purchased { get; set; }

    public decimal? PurchasePrice { get; set; }

    public string? Modifications { get; set; }

    public string? Remarks { get; set; }
}
