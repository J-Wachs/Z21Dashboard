using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;

namespace Z21Dashboard.Services;

/// <summary>
/// A singleton service that continuously tracks the operating time of locomotives in the background.
/// It persists its own state using the IAppDataService.
/// </summary>
public class LocoOperatingTimeService : ILocoOperatingTimeService, IDisposable
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
    private readonly object _locosLock = new();
    private readonly Dictionary<ushort, LocoTimeTracker> _trackedLocos = [];
    private const string AppDataKey = "OperatingTimes";

    public event Action? OnDataUpdated;

    public LocoOperatingTimeService(IZ21Client z21Client, IAppDataService appDataService)
    {
        _z21Client = z21Client;
        _appDataService = appDataService;

        LoadDataAndRefresh(); // Renamed to reflect new dual responsibility

        _z21Client.LocoInfoReceived += OnLocoInfoReceived;

        _secondTimer = new System.Timers.Timer(1000);
        _secondTimer.Elapsed += OnTimerTick;
        _secondTimer.AutoReset = true;
        _secondTimer.Start();
    }

    public IEnumerable<TrackedLocoTime> GetTrackedLocos()
    {
        lock (_locosLock)
        {
            return _trackedLocos.Values.Select(t => new TrackedLocoTime(
                t.LocoAddress, t.CurrentSpeedStep, t.NativeSpeedSteps, t.SpeedSteps,
                t.SpeedStepsNumeric, t.TotalOperatingSeconds, t.OperatingSecondsSinceService,
                t.DisplayProtocol, t.Protocol, t.Direction, t.Functions, t.Timestamp
            )).OrderBy(l => l.LocoAddress).ToList();
        }
    }

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

    private void OnLocoInfoReceived(object? sender, LocoInfo e)
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

    private void OnTimerTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        bool hasChanges = false;
        lock (_locosLock)
        {
            foreach (var tracker in _trackedLocos.Values)
            {
                if (tracker.CurrentSpeedStep > 0)
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

    private void LoadDataAndRefresh()
    {
        var persistentData = _appDataService.GetData<List<PersistentLocoTime>>(AppDataKey);
        if (persistentData == null) return;

        List<ushort> addressesToRefresh = new();
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

        // Proactively refresh the data from the Z21 in the background
        Task.Run(async () =>
        {
            foreach (var address in addressesToRefresh)
            {
                await _z21Client.GetLocoInfoAsync(address);
                await Task.Delay(200); // Wait 200ms between each request
            }
        });
    }

    private void SaveData()
    {
        List<PersistentLocoTime> dataToSave;
        lock (_locosLock)
        {
            dataToSave = _trackedLocos.Values.Select(t =>
                new PersistentLocoTime(
                    t.LocoAddress,
                    t.TotalOperatingSeconds,
                    t.OperatingSecondsSinceService,
                    t.Protocol,
                    t.NativeSpeedSteps
                )).ToList();
        }
        _appDataService.SaveData(AppDataKey, dataToSave);
    }

    public void Dispose()
    {
        SaveData();
        _z21Client.LocoInfoReceived -= OnLocoInfoReceived;
        _secondTimer?.Stop();
        _secondTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}