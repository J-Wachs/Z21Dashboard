using System.Collections.Concurrent;

namespace Z21Dashboard.Ui.Models;

/// <summary>
/// Passed to each <see cref="AppCloseCoordinator.CloseRequested"/> subscriber.
/// Call <see cref="ReportVote"/> exactly once per request.
/// </summary>
public class CloseVetoContext : EventArgs
{
    private readonly ConcurrentBag<string> _reasons = new();
    private int _votesReceived;
    private int _expectedVotes;
    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal CloseVetoContext(int requestId) => RequestId = requestId;

    public int RequestId { get; }

    /// <summary>
    /// Report this component's vote.
    /// </summary>
    /// <param name="canClose">true = this component allows closing.</param>
    /// <param name="reason">Required when canClose is false — why closing is blocked
    /// (e.g. "Locomotive 'BR 01' has unsaved CV changes").</param>
    public void ReportVote(
        bool canClose,
        string? reason = null
        )
    {
        if (!canClose)
        {
            _reasons.Add(string.IsNullOrWhiteSpace(reason) ? "Unsaved changes" : reason);
        }

        Interlocked.Increment(ref _votesReceived);
        TryComplete();
    }

    internal async Task<(bool CanClose, IReadOnlyList<string> Reasons)> WaitForVotesAsync(int expectedVotes)
    {
        // Set the expected count first (handles votes that already arrived synchronously).
        Interlocked.Exchange(ref _expectedVotes, expectedVotes);
        TryComplete();

        await _completion.Task;
        return (_reasons.IsEmpty, _reasons.ToArray());
    }

    private void TryComplete()
    {
        // Only complete once the expected count is known and every voter has reported.
        var expected = Volatile.Read(ref _expectedVotes);
        if (expected > 0 && Volatile.Read(ref _votesReceived) >= expected)
        {
            _completion.TrySetResult();
        }
    }
}
