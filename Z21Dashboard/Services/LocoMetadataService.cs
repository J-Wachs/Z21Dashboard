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
    private Dictionary<ushort, LocoMetadata> _locoMetadata = [];
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
        var storedMetadata = _appDataService.GetData<Dictionary<ushort, LocoMetadata>>(LocoMetadataStorageKey);
        _locoMetadata = storedMetadata ?? [];
    }

    /// <inheritdoc/>
    public LocoMetadata? GetMetadata(ushort locoAddress)
    {
        _locoMetadata.TryGetValue(locoAddress, out var metadata);
        return metadata;
    }

    /// <inheritdoc/>
    public string GetDisplayName(ushort locoAddress)
    {
        var metadata = GetMetadata(locoAddress);
        return string.IsNullOrWhiteSpace(metadata?.Name)
            ? locoAddress.ToString()
            : $"{locoAddress}: {metadata.Name}";
    }

    /// <inheritdoc/>
    public async Task SaveMetadata(ushort locoAddress, LocoMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Name))
        {
            metadata.Name = metadata.Name.Trim();
        }

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

    /// <inheritdoc/>
    public void RemoveMetadata(ushort locoAddress)
    {
        if (_locoMetadata.Remove(locoAddress))
        {
            _appDataService.SaveData(LocoMetadataStorageKey, _locoMetadata);
            OnMetadataUpdated?.Invoke();
        }
    }
}