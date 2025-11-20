//
// Services/LocoMetadataService.cs
//
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Services;

/// <summary>
/// A singleton service that manages user-defined metadata for locomotives,
/// persisting them using the AppDataService.
/// </summary>
public class LocoMetadataService : ILocoMetadataService
{
    private readonly IAppDataService _appDataService;
    private Dictionary<int, LocoMetadata> _locoMetadata = [];
    private const string LocoMetadataStorageKey = "LocomotiveMetadata";

    /// <summary>
    /// Event that is triggered when the collection of locomotive metadata is updated.
    /// </summary>
    public event Action? OnMetadataUpdated;

    public LocoMetadataService(IAppDataService appDataService)
    {
        _appDataService = appDataService;
        LoadMetadata();
    }

    /// <summary>
    /// Loads the dictionary of metadata from the storage.
    /// </summary>
    private void LoadMetadata()
    {
        var storedMetadata = _appDataService.GetData<Dictionary<int, LocoMetadata>>(LocoMetadataStorageKey);
        _locoMetadata = storedMetadata ?? [];
    }

    /// <inheritdoc/>
    public LocoMetadata? GetMetadata(int locoAddress)
    {
        _locoMetadata.TryGetValue(locoAddress, out var metadata);
        return metadata;
    }

    /// <inheritdoc/>
    public string GetDisplayName(int locoAddress)
    {
        var metadata = GetMetadata(locoAddress);
        return string.IsNullOrWhiteSpace(metadata?.Name)
            ? locoAddress.ToString()
            : $"{locoAddress}: {metadata.Name}";
    }

    /// <inheritdoc/>
    public async Task SaveMetadata(int locoAddress, LocoMetadata metadata)
    {
        // Trim the name before saving
        if (!string.IsNullOrWhiteSpace(metadata.Name))
        {
            metadata.Name = metadata.Name.Trim();
        }

        // Business rule: If a metadata object becomes "empty" (no name, no interval),
        // we remove it from the dictionary to keep storage clean.
        bool isEffectivelyEmpty = string.IsNullOrWhiteSpace(metadata.Name) &&
                                  (metadata.ServiceIntervalHours is null or <= 0);

        if (isEffectivelyEmpty)
        {
            _locoMetadata.Remove(locoAddress);
        }
        else
        {
            _locoMetadata[locoAddress] = metadata;
        }

        _appDataService.SaveData(LocoMetadataStorageKey, _locoMetadata);

        OnMetadataUpdated?.Invoke();

        await Task.CompletedTask;
    }
}