namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Data Transfer Object representing a summary of a decoder definition.
/// Used for populating selection lists in the UI.
/// </summary>
/// <param name="FileName">The filename (e.g., "esu_v5.json").</param>
/// <param name="Name">The display name or model name of the decoder.</param>
/// <param name="Manufacturer">The manufacturer name.</param>
/// <param name="Location">Where the file is stored (App or Local).</param>
internal record DecoderListItemDto(
    string FileName,
    string Name,
    ushort ManufacturerId,
    string Manufacturer,
    DecoderFileLocation Location)
{
    public virtual bool Equals(DecoderListItemDto? other)
    {
        return other is not null
            && FileName == other.FileName
            && Location == other.Location;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, Location);
    }
};
