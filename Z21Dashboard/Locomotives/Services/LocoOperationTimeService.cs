using Z21Client.Helpers;
using Z21Client;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Locomotives.Interfaces;

namespace Z21Dashboard.Services;

/// <summary>
/// A singleton service that continuously tracks the operating time of locomotives in the background.
/// It persists its own state using the IAppDataService.
/// </summary>
public partial class LocoOperatingTimeService : ILocoOperatingTimeService, IDisposable
{
    // This internal tracker holds both live state AND persistent time counters.
    private sealed record LocoTimeTracker
    {
        public ushort LocoAddress { get; init; }
        public byte CurrentSpeedStep { get; set; }
        public NativeSpeedSteps NativeSpeedSteps { get; set; }
        public SpeedSteps SpeedSteps { get; set; }
        public byte SpeedStepsNumeric { get; set; }
        public string? DisplayProtocol { get; set; }
        public LocoMode Protocol { get; set; }
        public DrivingDirection Direction { get; set; }
        public bool[] Functions { get; set; } = [];
        public DateTime Timestamp { get; set; }

        // Persistent counters now live here
        public long TotalOperatingSeconds { get; set; }
        public long OperatingSecondsSinceService { get; set; }
    }

    // The data structure that gets saved to the JSON file, now including protocol info.
    private sealed record PersistentLocoTime(
        ushort LocoAddress,
        long TotalOperatingSeconds,
        long OperatingSecondsSinceService,
        LocoMode Protocol,
        NativeSpeedSteps NativeSpeedSteps
    );

    private readonly IZ21Client _z21Client;
    private readonly IAppDataService _appDataService;
    private readonly System.Timers.Timer _secondTimer;
    private readonly Lock _locosLock = new();
    private readonly Dictionary<ushort, LocoTimeTracker> _trackedLocos = [];
    private TrackPowerState? _trackPowerState;
    private const string AppDataKey = "OperatingTimes";

    public event Action? OnDataUpdated;

    public LocoOperatingTimeService(
        IZ21Client z21Client,
        IAppDataService appDataService
        )
    {
        _z21Client = z21Client;
        _appDataService = appDataService;

        LoadDataAndRefresh();

        _z21Client.OnLocoInfoReceived += OnLocoInfoReceived;
        _z21Client.OnTrackPowerInfoReceived += OnTrackPowerInfoReceived;
        _z21Client.OnStatusChanged += OnStatusChanged;

        _ = _z21Client.GetStatusAsync();

        _secondTimer = new System.Timers.Timer(1000);
        _secondTimer.Elapsed += OnTimerTick;
        _secondTimer.AutoReset = true;
        _secondTimer.Start();
    }

    public IEnumerable<TrackedLocoTime> GetTrackedLocos()
    {
        lock (_locosLock)
        {
            return [.. _trackedLocos.Values.Select(t => new TrackedLocoTime(
                t.LocoAddress, t.CurrentSpeedStep, t.NativeSpeedSteps, t.SpeedSteps,
                t.SpeedStepsNumeric, t.TotalOperatingSeconds, t.OperatingSecondsSinceService,
                t.DisplayProtocol, t.Protocol, t.Direction, t.Functions, t.Timestamp
            )).OrderBy(l => l.LocoAddress).ToList()];
        }
    }

    /// <inheritdoc/>
    public void ResetServiceCounter(ushort locoAddress)
    {
        lock (_locosLock)
        {
            if (_trackedLocos.TryGetValue(locoAddress, out var tracker))
            {
                tracker.OperatingSecondsSinceService = 0;
            }
        }
        OnDataUpdated?.Invoke();
    }

    /// <inheritdoc/>
    public void RemoveLoco(ushort locoAddress)
    {
        lock (_locosLock)
        {
            if (_trackedLocos.Remove(locoAddress))
            {
                SaveData();
            }
        }
        OnDataUpdated?.Invoke();
    }

    /// <summary>
    /// Event handler triggered when the status changes, allowing the service to update its track power state.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnStatusChanged(object? sender, StatusChanged e)
    {
        _z21Client.OnStatusChanged -= OnStatusChanged;

        _trackPowerState = e.TrackVoltageOff ? TrackPowerState.Off : TrackPowerState.On;
    }

    /// <summary>
    /// Event handler triggered when track power information is received from the central station.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnTrackPowerInfoReceived(object? sender, TrackPowerInfo e)
    {
        _trackPowerState = e.State;
    }

    /// <summary>
    /// Event handler trigged when information about a locomotive is send from the 
    /// central station.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocoInfoReceived(
        object? sender,
        LocoInfo e
        )
    {
        lock (_locosLock)
        {
            if (!_trackedLocos.TryGetValue(e.Address, out var tracker))
            {
                tracker = new LocoTimeTracker { LocoAddress = e.Address };
                _trackedLocos[e.Address] = tracker;
            }

            tracker.CurrentSpeedStep = e.CurrentSpeed;
            tracker.NativeSpeedSteps = e.NativeSpeedSteps;
            tracker.SpeedSteps = e.SpeedSteps;
            tracker.SpeedStepsNumeric = e.SpeedStepsNumeric;
            tracker.DisplayProtocol = e.DisplayProtocol;
            tracker.Protocol = e.LocomotiveMode ?? LocoMode.DCC;
            tracker.Direction = e.Direction;
            tracker.Functions = e.Functions;
            tracker.Timestamp = DateTime.Now;
        }
        OnDataUpdated?.Invoke();
    }

    /// <summary>
    /// Event handler for the timer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTimerTick(
        object? sender,
        System.Timers.ElapsedEventArgs e
        )
    {
        bool hasChanges = false;
        lock (_locosLock)
        {
            foreach (var tracker in _trackedLocos.Values)
            {
                // We must only increment the operating time if the locomotive is actually
                // moving and the track power is on.
                if (tracker.CurrentSpeedStep > 0 && _trackPowerState == TrackPowerState.On)
                {
                    tracker.TotalOperatingSeconds++;
                    tracker.OperatingSecondsSinceService++;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            OnDataUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Loads and request data about a locomotive form the central station.
    /// </summary>
    private void LoadDataAndRefresh()
    {
        var persistentData = _appDataService.GetData<List<PersistentLocoTime>>(AppDataKey);
        if (persistentData == null) return;

        List<ushort> addressesToRefresh = [];
        lock (_locosLock)
        {
            foreach (var item in persistentData)
            {
                if (!_trackedLocos.ContainsKey(item.LocoAddress))
                {
                    _trackedLocos[item.LocoAddress] = new LocoTimeTracker
                    {
                        LocoAddress = item.LocoAddress,
                        TotalOperatingSeconds = item.TotalOperatingSeconds,
                        OperatingSecondsSinceService = item.OperatingSecondsSinceService,
                        Protocol = item.Protocol,
                        NativeSpeedSteps = item.NativeSpeedSteps,
                        DisplayProtocol = Z21ProtocolName.GetName(item.Protocol, item.NativeSpeedSteps)
                    };
                    addressesToRefresh.Add(item.LocoAddress);
                }
            }
        }

        Task.Run(async () =>
        {
            foreach (var address in addressesToRefresh)
            {
                await _z21Client.GetLocoInfoAsync(address);
                await Task.Delay(200);
            }
        });
    }

    /// <summary>
    /// Saves the locomotive data to the application data store
    /// JSON file.
    /// </summary>
    private void SaveData()
    {
        List<PersistentLocoTime> dataToSave;
        lock (_locosLock)
        {
            dataToSave = [.. _trackedLocos.Values.Select(t =>
                new PersistentLocoTime(
                    t.LocoAddress,
                    t.TotalOperatingSeconds,
                    t.OperatingSecondsSinceService,
                    t.Protocol,
                    t.NativeSpeedSteps
                ))];
        }
        _appDataService.SaveData(AppDataKey, dataToSave);
    }

    public void Dispose()
    {
        SaveData();
        _z21Client.OnLocoInfoReceived -= OnLocoInfoReceived;
        _secondTimer?.Stop();
        _secondTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
