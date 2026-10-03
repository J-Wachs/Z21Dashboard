using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Locomotives.Interfaces;
using Z21Dashboard.Locomotives.Models;

namespace Z21Dashboard.Locomotives.Services;

/// <summary>
/// A singleton service that manages user-defined metadata for locomotives,
/// persisting them using the AppDataService.
/// </summary>
public class LocoMetadataService : ILocoMetadataService
{
    private readonly IAppDataService _appDataService;
    private readonly ILocoDefinitionService _locoDefinitionService;
    private Dictionary<ushort, LocoMetadata> _locoMetadata = [];
    private const string LocoMetadataStorageKey = "LocomotiveMetadata";

    /// <summary>
    /// Event that is triggered when the collection of locomotive metadata is updated.
    /// </summary>
    public event Action? OnMetadataUpdated;

    public LocoMetadataService(IAppDataService appDataService, ILocoDefinitionService locoDefinitionService)
    {
        _appDataService = appDataService;
        _locoDefinitionService = locoDefinitionService;
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
    public async Task<string> GetDisplayNameAsync(ushort locoAddress, bool prefixWithAddress)
    {
        // First check if there is a locomotive defined with this address.
        var result = await _locoDefinitionService.GetLocomotiveByAddressAsync(locoAddress);
        if (result.IsFailure)
        {
            // Else get internal defined name if it exists, otherwise return the address as string.
            var metadata = GetMetadata(locoAddress);
            if (string.IsNullOrWhiteSpace(metadata?.Name))
            {
                return locoAddress.ToString();
            }

            return prefixWithAddress ? $"{locoAddress}: {metadata.Name}" : metadata.Name;
        }

        if (string.IsNullOrEmpty(result.Data?.ShortName))
        {
            return locoAddress.ToString();
        }

        return prefixWithAddress ? $"{locoAddress}: {result.Data.ShortName}" : result.Data.ShortName;
    }

    /// <inheritdoc/>
    public async Task SaveMetadataAsync(
        ushort locoAddress,
        LocoMetadata metadata
        )
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
