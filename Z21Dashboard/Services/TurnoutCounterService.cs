using System.Collections.Concurrent;
using Z21Client;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Services;

public class TurnoutCounterService : ITurnoutCounterService, IDisposable
{
    private sealed class TurnoutTracker
    {
        public ushort Address { get; init; }
        public long TotalSwitches { get; set; }
        public long SwitchesSinceService { get; set; }
    }

    private record PersistentTurnoutData(ushort Address, long TotalSwitches, long SwitchesSinceService);

    private readonly IZ21Client _z21Client;
    private readonly IAppDataService _appDataService;
    private readonly ConcurrentDictionary<ushort, TurnoutTracker> _trackers = new();
    private readonly ConcurrentDictionary<ushort, TurnoutState> _lastKnownStates = new();
    private readonly ConcurrentDictionary<ushort, DateTime> _timestamps = new();
    private readonly ConcurrentDictionary<ushort, TurnoutMode> _modes = new();
    private Dictionary<ushort, TurnoutMetadata> _metadata = new();
    private readonly object _lock = new();

    private const string StorageKey = "TurnoutCounts";
    private const string MetadataKey = "TurnoutMetadata";

    public event Action? OnDataUpdated;

    public TurnoutCounterService(IZ21Client z21Client, IAppDataService appDataService)
    {
        _z21Client = z21Client;
        _appDataService = appDataService;

        LoadData();

        _z21Client.TurnoutInfoReceived += OnTurnoutInfoReceived;
        _z21Client.TurnoutModeReceived += OnTurnoutModeReceived;
    }

    private void LoadData()
    {
        var savedData = _appDataService.GetData<List<PersistentTurnoutData>>(StorageKey);
        if (savedData != null)
        {
            foreach (var item in savedData)
            {
                _trackers[item.Address] = new TurnoutTracker
                {
                    Address = item.Address,
                    TotalSwitches = item.TotalSwitches,
                    SwitchesSinceService = item.SwitchesSinceService
                };
            }
        }

        var savedMetadata = _appDataService.GetData<Dictionary<ushort, TurnoutMetadata>>(MetadataKey);
        _metadata = savedMetadata ?? new();
    }

    public IEnumerable<TrackedTurnoutCount> GetTrackedTurnouts()
    {
        lock (_lock)
        {
            return _trackers.Values.Select(t => new TrackedTurnoutCount
            {
                Address = t.Address,
                TotalSwitches = t.TotalSwitches,
                SwitchesSinceService = t.SwitchesSinceService,
                State = _lastKnownStates.TryGetValue(t.Address, out var state) ? state : TurnoutState.NotSwitched,
                Timestamp = _timestamps.TryGetValue(t.Address, out var ts) ? ts : DateTime.MinValue,
                Mode = _modes.TryGetValue(t.Address, out var mode) ? mode : null
            }).OrderBy(rec => rec.Address).ToList();
        }
    }

    public long GetCount(ushort address)
    {
        return _trackers.TryGetValue(address, out var tracker) ? tracker.TotalSwitches : 0;
    }

    public void IncrementCount(ushort address)
    {
        lock (_lock)
        {
            if (!_trackers.TryGetValue(address, out var tracker))
            {
                tracker = new TurnoutTracker { Address = address };
                _trackers[address] = tracker;
            }
            tracker.TotalSwitches++;
            tracker.SwitchesSinceService++;
            SaveToAppData();
            OnDataUpdated?.Invoke();
        }
    }

    public void ResetCount(ushort address)
    {
        lock (_lock)
        {
            if (_trackers.TryGetValue(address, out var tracker))
            {
                tracker.TotalSwitches = 0;
                tracker.SwitchesSinceService = 0;
                SaveToAppData();
                OnDataUpdated?.Invoke();
            }
        }
    }

    public void ResetServiceCounter(ushort address)
    {
        lock (_lock)
        {
            if (_trackers.TryGetValue(address, out var tracker))
            {
                tracker.SwitchesSinceService = 0;
                SaveToAppData();
                OnDataUpdated?.Invoke();
            }
        }
    }

    public TurnoutMetadata? GetMetadata(ushort address)
    {
        lock (_lock)
        {
            _metadata.TryGetValue(address, out var meta);
            return meta;
        }
    }

    public async Task SaveMetadata(ushort address, TurnoutMetadata metadata)
    {
        lock (_lock)
        {
            if (metadata.ServiceIntervalSwitches is null or <= 0)
            {
                _metadata.Remove(address);
            }
            else
            {
                _metadata[address] = metadata;
            }
            _appDataService.SaveData(MetadataKey, _metadata);
        }
        OnDataUpdated?.Invoke();
        await Task.CompletedTask;
    }

    public void RemoveTurnout(ushort address)
    {
        lock (_lock)
        {
            bool removed = _trackers.TryRemove(address, out _);
            _lastKnownStates.TryRemove(address, out _);
            _timestamps.TryRemove(address, out _);
            _modes.TryRemove(address, out _);
            _metadata.Remove(address);

            if (removed)
            {
                SaveToAppData();
                _appDataService.SaveData(MetadataKey, _metadata);
                OnDataUpdated?.Invoke();
            }
        }
    }

    private void OnTurnoutInfoReceived(object? sender, TurnoutInfo e)
    {
        if (e.State == TurnoutState.NotSwitched) return;

        lock (_lock)
        {
            bool stateChanged = false;
            if (_lastKnownStates.TryGetValue(e.Address, out var previousState))
            {
                if (previousState != e.State)
                {
                    _lastKnownStates[e.Address] = e.State;
                    _timestamps[e.Address] = DateTime.Now;

                    if (!_trackers.TryGetValue(e.Address, out var tracker))
                    {
                        tracker = new TurnoutTracker { Address = e.Address };
                        _trackers[e.Address] = tracker;
                    }
                    tracker.TotalSwitches++;
                    tracker.SwitchesSinceService++;

                    SaveToAppData();
                    stateChanged = true;
                }
            }
            else
            {
                _lastKnownStates.TryAdd(e.Address, e.State);
                _timestamps.TryAdd(e.Address, DateTime.Now);

                if (!_trackers.ContainsKey(e.Address))
                {
                    _trackers[e.Address] = new TurnoutTracker { Address = e.Address };
                    SaveToAppData();
                }
                stateChanged = true;
            }

            if (stateChanged)
            {
                if (!_modes.ContainsKey(e.Address))
                {
                    _ = _z21Client.GetTurnoutModeAsync(e.Address);
                }
                OnDataUpdated?.Invoke();
            }
        }
    }

    private void OnTurnoutModeReceived(object? sender, TurnoutModeStatus e)
    {
        lock (_lock)
        {
            _modes[e.Address] = e.Mode;
            if (!_trackers.ContainsKey(e.Address))
            {
                _trackers[e.Address] = new TurnoutTracker { Address = e.Address };
                SaveToAppData();
            }
            OnDataUpdated?.Invoke();
        }
    }

    private void SaveToAppData()
    {
        var dataToSave = _trackers.Values.Select(t => new PersistentTurnoutData(t.Address, t.TotalSwitches, t.SwitchesSinceService)).ToList();
        _appDataService.SaveData(StorageKey, dataToSave);
    }

    public void Dispose()
    {
        _z21Client.TurnoutInfoReceived -= OnTurnoutInfoReceived;
        _z21Client.TurnoutModeReceived -= OnTurnoutModeReceived;
    }
}
