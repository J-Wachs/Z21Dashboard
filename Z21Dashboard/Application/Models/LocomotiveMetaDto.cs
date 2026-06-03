using System.ComponentModel.DataAnnotations;

namespace Z21Dashboard.Application.Models;

public class LocomotiveMetaDto
{
    public required string Name { get; set; }

    [MaxLength(20)]
    public string? ShortName { get; set; }

    public string? Manufacturer { get; set; }

    public string? CatalogueNumber { get; set; }

    public string? Class { get; set; }

    public string? UnitNumber { get; set; }

    public string? Description { get; set; }

    public DateTime? Purchased { get; set; }

    public decimal? PurchasePrice { get; set; }

    public string? Remarks { get; set; }

    public string? Modifications { get; set; }
}
