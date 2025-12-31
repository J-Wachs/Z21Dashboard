using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces
{
    public interface ITurnoutCounterService
    {
        event Action OnDataUpdated;
        IEnumerable<TrackedTurnoutCount> GetTrackedTurnouts();
        long GetCount(ushort address);
        void IncrementCount(ushort address);
        void ResetCount(ushort address);
        void RemoveTurnout(ushort address);

        // Service functionality
        void ResetServiceCounter(ushort address);
        TurnoutMetadata? GetMetadata(ushort address);
        Task SaveMetadata(ushort address, TurnoutMetadata metadata);
    }
}
