namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// Class defining the data for showing a locomotive in the DataGrid.
/// </summary>
public record LocomotiveListItemDto(
    string FilePath,
    short Address,
    string FileName,
    string Name,
    string? ShortName,
    string? Manufacturer,
    string? Class,
    string? UnitNumber
    )
{
    public virtual bool Equals(LocomotiveListItemDto? other)
    {
        return other is not null && FilePath == other.FilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath);
    }
}
