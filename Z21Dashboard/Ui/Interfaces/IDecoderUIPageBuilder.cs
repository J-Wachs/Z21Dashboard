using Z21Dashboard.Helpers;
using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.Ui.Interfaces;

/// <summary>
/// Defines a contract for adding UI pages to a decoder UI interface.
/// </summary>
internal interface IDecoderUIPageBuilder
{
    /// <summary>
    /// Adds one or more pages to the UI based on the specified parameters.
    /// </summary>
    /// <param name="parms">An object containing the parameters that define the pages to add. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task<Result> AddPagesAsync(UIPagesAddPagesParams parms);
}
