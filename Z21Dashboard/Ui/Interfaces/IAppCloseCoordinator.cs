using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.Ui.Interfaces;

public interface IAppCloseCoordinator
{
    event EventHandler<CloseVetoContext>? CloseRequested;
    event EventHandler<IReadOnlyList<string>>? CloseVetoed;
    event EventHandler? ForceCloseRequested;

    /// <summary>Raised to ask components whether an in-app disconnect is OK. Vote via the context.</summary>
    event EventHandler<CloseVetoContext>? DisconnectRequested;

    /// <summary>Raised when disconnecting was vetoed, carrying all collected reasons.</summary>
    event EventHandler<IReadOnlyList<string>>? DisconnectVetoed;

    /// <summary>Raised when all components approved — each component must now run its disconnect cleanup.</summary>
    event EventHandler? DisconnectApproved;

    /// <summary>
    /// Raised when the user chose "disconnect anyway" after a veto. The original requester
    /// (e.g. the component that called <see cref="RequestDisconnectAsync"/>) listens and retries.
    /// </summary>
    event EventHandler? ForceDisconnectRequested;

    void ForceClose();
    Task<(bool CanClose, IReadOnlyList<string> Reasons)> RequestCloseAsync();

    /// <summary>
    /// Asks all subscribed components for permission to disconnect from the central station
    /// (app keeps running). If nobody vetoes, <see cref="DisconnectApproved"/> is raised so
    /// components reset themselves.
    /// </summary>
    Task<(bool CanDisconnect, IReadOnlyList<string> Reasons)> RequestDisconnectAsync();

    /// <summary>
    /// Called from the central dialog when the user chooses to discard unsaved data during
    /// an in-app disconnect. Raises <see cref="DisconnectApproved"/> unconditionally (components
    /// discard their state) and then <see cref="ForceDisconnectRequested"/> so the original
    /// requester retries its action.
    /// </summary>
    void ForceDisconnect();

    /// <summary>
    /// Re-arms disconnect voting after a <see cref="ForceDisconnect"/>: subsequent
    /// <see cref="RequestDisconnectAsync"/> calls ask all subscribers again instead of
    /// being auto-approved.
    /// </summary>
    void ResetDisconnectForce();
}
