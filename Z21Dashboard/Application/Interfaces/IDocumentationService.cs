namespace Z21Status.Application.Interfaces;

/// <summary>
/// Defines a service for handling application documentation.
/// </summary>
public interface IDocumentationService
{
    /// <summary>
    /// Opens the correct user manual (e.g., a PDF file) based on the application's
    /// current UI culture, using the default system application.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OpenManualAsync();
}
