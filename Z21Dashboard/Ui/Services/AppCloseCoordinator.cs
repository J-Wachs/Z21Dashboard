using Z21Dashboard.Ui.Interfaces;
using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.Ui.Services;

/// <summary>
/// Coordinates application shutdown. Blazor components that can veto closing
/// subscribe to <see cref="CloseRequested"/> and vote via <see cref="CloseVetoContext.ReportVote"/>.
/// A central UI (MainLayout) subscribes to <see cref="CloseVetoed"/> to display the
/// collected reasons. The user can override all vetoes via <see cref="ForceClose"/>.
/// </summary>
public class AppCloseCoordinator : IAppCloseCoordinator
{
    private int _activeRequestId;
    private bool _forceClose;
    private bool _forceDisconnect;

    /// <summary>
    /// Raised when the user tries to close the application.
    /// Each subscriber MUST call <see cref="CloseVetoContext.ReportVote"/> exactly once,
    /// either synchronously or after asynchronous work.
    /// </summary>
    public event EventHandler<CloseVetoContext>? CloseRequested;

    /// <summary>
    /// Raised when closing was vetoed, carrying all collected reasons.
    /// </summary>
    public event EventHandler<IReadOnlyList<string>>? CloseVetoed;

    /// <summary>
    /// Raised when the user chose "close anyway". The native side listens and closes the window.
    /// </summary>
    public event EventHandler? ForceCloseRequested;

    /// <summary>
    /// Raised when an in-app disconnect from the central station is requested
    /// (the app keeps running). Each subscriber MUST call
    /// <see cref="CloseVetoContext.ReportVote"/> exactly once,
    /// either synchronously or after asynchronous work.
    /// </summary>
    public event EventHandler<CloseVetoContext>? DisconnectRequested;

    /// <summary>
    /// Raised when disconnecting was vetoed, carrying all collected reasons.
    /// </summary>
    public event EventHandler<IReadOnlyList<string>>? DisconnectVetoed;

    /// <summary>
    /// Raised when every component approved the disconnect. Subscribers must now
    /// run their cleanup (reset dirty state, end sessions, etc.).
    /// </summary>
    public event EventHandler? DisconnectApproved;

    /// <summary>
    /// Raised when the user chose "disconnect anyway" after a veto. The original requester
    /// listens and retries its action; the retried <see cref="RequestDisconnectAsync"/>
    /// is approved without voting.
    /// </summary>
    public event EventHandler? ForceDisconnectRequested;

    /// <summary>
    /// Called from the native Closing handler.
    /// Returns (canClose, reasons). canClose is true only if no subscriber vetoed,
    /// or the user has forced the close via <see cref="ForceClose"/>.
    /// </summary>
    public async Task<(bool CanClose, IReadOnlyList<string> Reasons)> RequestCloseAsync()
    {
        if (_forceClose)
        {
            return (true, Array.Empty<string>());
        }

        var handlers = CloseRequested;
        if (handlers is null)
        {
            // No component is listening — nothing can be dirty, allow close.
            return (true, Array.Empty<string>());
        }

        var context = new CloseVetoContext(Interlocked.Increment(ref _activeRequestId));
        var invocationList = handlers.GetInvocationList();

        foreach (EventHandler<CloseVetoContext> handler in invocationList)
        {
            handler(this, context);
        }

        var (canClose, reasons) = await context.WaitForVotesAsync(invocationList.Length);

        if (!canClose)
        {
            CloseVetoed?.Invoke(this, reasons);
        }

        return (canClose, reasons);
    }

    /// <summary>
    /// Called from Blazor when an in-app disconnect from the central station is wanted.
    /// Returns (canDisconnect, reasons). If approved, <see cref="DisconnectApproved"/> is raised so
    /// components perform their cleanup; the application keeps running either way.
    /// </summary>
    public async Task<(bool CanDisconnect, IReadOnlyList<string> Reasons)> RequestDisconnectAsync()
    {
        if (_forceDisconnect)
        {
            return (true, Array.Empty<string>());
        }

        var handlers = DisconnectRequested;
        if (handlers is null)
        {
            // Nothing listening — nothing to clean up.
            return (true, Array.Empty<string>());
        }

        var context = new CloseVetoContext(Interlocked.Increment(ref _activeRequestId));
        var invocationList = handlers.GetInvocationList();

        foreach (EventHandler<CloseVetoContext> handler in invocationList)
        {
            handler(this, context);
        }

        var (canDisconnect, reasons) = await context.WaitForVotesAsync(invocationList.Length);

        if (canDisconnect)
        {
            // Phase 2: everyone approved — now components do their actual cleanup.
            DisconnectApproved?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            DisconnectVetoed?.Invoke(this, reasons);
        }

        return (canDisconnect, reasons);
    }

    /// <summary>
    /// Called from the central dialog when the user chooses to discard unsaved data.
    /// All subsequent close requests are approved, and the native window is asked to close.
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
        ForceCloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called from the central dialog when the user chooses to discard unsaved data during
    /// an in-app disconnect. All components are told to clean up (discarding their state), all
    /// subsequent disconnect requests are approved, and the original requester is asked to retry.
    /// </summary>
    public void ForceDisconnect()
    {
        _forceDisconnect = true;
        // Components discard their dirty state so the retried request cannot veto again.
        DisconnectApproved?.Invoke(this, EventArgs.Empty);
        ForceDisconnectRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Re-arms disconnect voting after a <see cref="ForceDisconnect"/>. Call this once the
    /// forced action has completed so later disconnects ask subscribers again.
    /// </summary>
    public void ResetDisconnectForce() => _forceDisconnect = false;
}
