namespace Z21Dashboard.Helpers;

/// <summary>
/// A utility class to debounce an action, meaning it will only be executed
/// after a certain amount of time has passed without the action being triggered again.
/// </summary>
public class Debouncer
{
    private readonly TimeSpan _delay;
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    /// <param name="delay">The time to wait before executing the action.</param>
    public Debouncer(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <summary>
    /// Debounces the specified action. Each time this is called, the internal timer is reset.
    /// The action will only be executed once the timer completes without being reset.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Debounce(Action action)
    {
        // Cancel any existing timer
        _timer?.Dispose();

        // Create a new timer that will execute the action after the delay
        _timer = new Timer(
            _ => action(), // The callback to execute
            null,         // No state object needed
            _delay,       // The delay before execution
            Timeout.InfiniteTimeSpan // Do not repeat
        );
    }

    /// <summary>
    /// Disposes the internal timer.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
