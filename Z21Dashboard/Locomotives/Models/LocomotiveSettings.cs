using Z21Dashboard.LocomotiveDecoders.Models;

namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// The locomotive settings structure (class) used internally.
/// </summary>
public class LocomotiveSettings
{
    public string? LocomotiveFile { get; set; }
    public LocomotiveState State { get; set; } = LocomotiveState.Idle;
    public required LocomotiveMeta LocomotiveMeta { get; set; }
    public required DecoderSettings DecoderSettings { get; set; }

    public void Clear()
    {
        LocomotiveFile = null;
        LocomotiveMeta.Clear();
        DecoderSettings.Clear();
        State = LocomotiveState.Idle;
    }

    public bool IsChanged()
    {
        return DecoderSettings.IsChanged() || LocomotiveMeta.IsChanged;
    }

    public void SetLocomotive(string locomotiveFile)
    {
        LocomotiveFile = locomotiveFile;
    }

    public void ClearChanged()
    {
        LocomotiveMeta.ClearChanged();
        DecoderSettings.ClearChanged();
    }
}
