namespace Z21Dashboard.Application.Models;

public class ManufacturerCollection
{
    public List<Manufacturer> IDs { get; set; } = [];
    public class Manufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
