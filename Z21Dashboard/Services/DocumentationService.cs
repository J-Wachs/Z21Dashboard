using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.Services;

/// <summary>
/// Implements the service for handling application documentation.
/// This version opens PDF files located in a 'Docs' folder next to the application executable.
/// </summary>
public class DocumentationService(ILogger<DocumentationService> logger) : IDocumentationService
{

    /// <inheritdoc/>
    public Task OpenManualAsync()
    {
        try
        {
            // 1. Determine the language and select the correct file name.
            string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string manualFileName = cultureName.Equals("da", StringComparison.OrdinalIgnoreCase)
                ? "UserManual_da.pdf"
                : "UserManual.pdf"; // English is the fallback.

            //_logger.LogInformation("Attempting to open manual. Selected file: {FileName}", manualFileName);

            // 2. Get the directory of the currently executing application.
            string exePath = Assembly.GetExecutingAssembly().Location;
            string? exeDir = Path.GetDirectoryName(exePath);

            if (string.IsNullOrEmpty(exeDir))
            {
                // "Attempting to open manual. Selected file: {FileName}"
                logger.LogError(SharedResources.Text0003, manualFileName);
                return Task.CompletedTask;
            }

            // 3. Construct the full path to the PDF file inside the 'Docs' subfolder.
            string filePath = Path.Combine(exeDir, "Doc", manualFileName);

            if (!File.Exists(filePath))
            {
                // "Manual file not found at path: {FilePath}"
                logger.LogError(SharedResources.Text0004, filePath);
                // Optionally, you could try to open the fallback English manual here if the Danish one is missing.
                return Task.CompletedTask;
            }

            // 4. Use Process.Start to open the file with the default system application.
            // We need to configure it to use the shell for it to work with non-executable files like PDFs.
            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // "Failed to open the user manual: {ex}"
            logger.LogError(SharedResources.Text0005, ex.Message);
        }

        return Task.CompletedTask;
    }
}

