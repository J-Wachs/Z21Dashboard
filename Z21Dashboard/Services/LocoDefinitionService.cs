using Microsoft.Extensions.Logging;
using System.Text.Json;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;

namespace Z21Dashboard.Services;

internal class LocoDefinitionService(
    IDashboardStateService dashboardStateService,
    ILogger<LocoDefinitionService> logger
    ) : ILocoDefinitionService
{
    public async Task<Result> DeleteLocomotiveFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Result.Success();
        }
        else
        {
            return Result.Failure("Locomotive file not found");
        }
    }

    public async Task<Result<LocomotiveDefinitionDto?>> GetLocomotiveAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogError($"Locomotive file not found: {filePath}");
                return Result<LocomotiveDefinitionDto?>.Failure($"Locomotive file not found: {filePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var locomotive = JsonSerializer.Deserialize<LocomotiveDefinitionDto>(jsonContent, options);

            return Result<LocomotiveDefinitionDto?>.Success(locomotive);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error deserializing locomotive from file: {filePath}");
            return Result<LocomotiveDefinitionDto?>.Failure($"Error loading locomotive from file '{filePath}': {ex.Message}");
        }
    }


    public async Task<Result<IReadOnlyList<LocomotiveListItemDto>?>> GetLocomotivesAsync()
    {
        try
        {
            List<LocomotiveListItemDto> locomotivesList = [];
            var settings = await dashboardStateService.GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.LocalLocomotiveFolder))
            {
                return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure("Locomotives folder not configured");
            }

            if (!Directory.Exists(settings.LocalLocomotiveFolder))
            {
                return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure($"Locomotives folder '{settings.LocalLocomotiveFolder}' not found");
            }

            var jsonFiles = Directory.GetFiles(settings.LocalLocomotiveFolder, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var locomotive = JsonSerializer.Deserialize<LocomotiveDefinitionDto>(jsonContent, options);

                    if (locomotive?.Meta is not null)
                    {
                        locomotivesList.Add(new LocomotiveListItemDto
                        {
                            Address = locomotive.Decoder.Address,
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            Name = locomotive.Meta.Name ?? string.Empty,
                            ShortName = locomotive.Meta.ShortName,
                            Manufacturer = locomotive.Meta.Manufacturer,
                            Class = locomotive.Meta.Class,
                            UnitNumber = locomotive.Meta.UnitNumber
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error gettings locomotives files: {filePath}");
                    return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure($"Error loading locomotive from file '{filePath}': {ex.Message}");
                }
            }
            return Result<IReadOnlyList<LocomotiveListItemDto>?>.Success([.. locomotivesList]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading locomotives from directory");
            return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure($"Error loading locomotives: {ex.Message}");
        }
    }

    public async Task<Result<string>> SaveLocomotiveAsync(LocomotiveDefinitionDto locomotive)
    {
        try
        {
            if (locomotive?.Meta is null)
            {
                return Result<string>.Failure("Locomotive data is invalid.");
            }

            // Validate required field
            if (string.IsNullOrWhiteSpace(locomotive.Meta.Name))
            {
                return Result<string>.Failure("Locomotive name is required.");
            }

            var settings = await dashboardStateService.GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.LocalLocomotiveFolder))
            {
                return Result<string>.Failure("Locomotives folder not configured");
            }
            if (!Directory.Exists(settings.LocalLocomotiveFolder))
            {
                Directory.CreateDirectory(settings.LocalLocomotiveFolder);
            }
            string fileName = GenerateUniqueLocoFileName(locomotive.Meta?.Name ?? "locomotive", settings.LocalLocomotiveFolder);
            string filePath = Path.Combine(settings.LocalLocomotiveFolder, fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonContent = JsonSerializer.Serialize(locomotive, options);
            await File.WriteAllTextAsync(filePath, jsonContent);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving locomotive");
            return Result<string>.Failure($"Error saving locomotive: {ex.Message}");
        }
    }


    /// <summary>
    /// Generate a unique filename for a new locomotive based on its name.
    /// </summary>
    private string GenerateUniqueLocoFileName(string locomotiveName, string locomotivesDirectory)
    {
        // Create a filename from the locomotive name, replacing invalid characters
        string baseName = Path.GetInvalidFileNameChars()
            .Aggregate(locomotiveName, (current, c) => current.Replace(c.ToString(), "_"));

        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\s+", "_");
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, "_+", "_").Trim('_');

        string fileName = $"{baseName}.json";
        string filePath = Path.Combine(locomotivesDirectory, fileName);

        // If the file already exists, add a number to make it unique
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{baseName}_{counter}.json";
            filePath = Path.Combine(locomotivesDirectory, fileName);
            counter++;
        }

        return fileName;
    }

}
