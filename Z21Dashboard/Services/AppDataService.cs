using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.Services;

/// <summary>
/// A singleton service that manages saving and loading structured data sets
/// to a single JSON file in the user's application data folder.
/// </summary>
public class AppDataService : IAppDataService
{
    private readonly string _dataFilePath;
    private readonly object _fileLock = new();
    private readonly ILogger<AppDataService> _logger;
    private readonly IStringLocalizer<SharedResources> _localizer;


    public AppDataService(ILogger<AppDataService> logger, IStringLocalizer<SharedResources> localizer)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "Z21DashboardApp");
        Directory.CreateDirectory(appFolderPath);
        _dataFilePath = Path.Combine(appFolderPath, "app_data.json");
        _logger = logger;
        _localizer = localizer;
    }

    public T? GetData<T>(string key)
    {
        lock (_fileLock)
        {
            if (!File.Exists(_dataFilePath))
            {
                return default;
            }

            try
            {
                string json = File.ReadAllText(_dataFilePath);
                var allData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (allData is not null && allData.TryGetValue(key, out var jsonElement))
                {
                    // Deserialize the specific JSON element into the requested type.
                    return jsonElement.Deserialize<T>();
                }
            }
            catch (Exception ex)
            {
                // "Error reading data for key '{key}': {ex.Message}"
                _logger.LogCritical(_localizer["Text0002"], key, ex.Message);
            }

            return default;
        }
    }

    public void SaveData<T>(string key, T data)
    {
        lock (_fileLock)
        {
            try
            {
                // Read the existing file or create a new dictionary.
                var allData = new Dictionary<string, JsonElement>();
                if (File.Exists(_dataFilePath))
                {
                    string existingJson = File.ReadAllText(_dataFilePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        allData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson) ?? [];
                    }
                }

                // Serialize the new data to a JSON string, then parse it into a JsonElement.
                string dataJson = JsonSerializer.Serialize(data);
                JsonElement jsonElement = JsonDocument.Parse(dataJson).RootElement;

                // Update or add the data set in the dictionary.
                allData[key] = jsonElement;

                // Serialize the entire dictionary back to the file.
                string newJson = JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFilePath, newJson);
            }
            catch (Exception ex)
            {
                // $"Error saving data for key '{key}': {ex.Message}"
                _logger.LogCritical(_localizer["Text0002"], key, ex.Message);
            }
        }
    }
}
