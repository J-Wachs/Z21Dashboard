using System;
using Z21Client.Models;

namespace Z21Dashboard.Application.Models;

public record TrackedTurnoutCount
{
    public ushort Address { get; init; }
    public long TotalSwitches { get; init; }
    public long SwitchesSinceService { get; init; }
    public TurnoutState State { get; init; } = TurnoutState.NotSwitched;
    public DateTime Timestamp { get; init; } = DateTime.MinValue;
    public TurnoutMode? Mode { get; init; }
}
