using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines a contract for adding UI pages to a decoder interface asynchronously.
/// </summary>
internal interface IDecoderUIPageBuilder
{
    /// <summary>
    /// Adds one or more pages to the UI based on the specified parameters.
    /// </summary>
    /// <param name="parms">An object containing the parameters that define the pages to add. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task<Result> AddPages(UIPagesAddPagesParams parms);
}
