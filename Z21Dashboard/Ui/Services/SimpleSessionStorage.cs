using Z21Dashboard.Ui.Interfaces;
using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.Ui.Services;

/// <summary>
/// Session storage of various properties
/// </summary>
public class SimpleSessionStorage : ISessionStorage
{
    private Dictionary<string, object?> _sessiontItems { get; set; } = [];

    /// <inheritdoc />
    public bool? GetBool(string key, bool? defaultValue = null)
    {
        if (_sessiontItems.TryGetValue(key, out var boolValue))
        {
            return (bool?)boolValue;
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public ProgrammingTarget? GetProgTarget(string key)
    {
        if (_sessiontItems.TryGetValue(key, out var progTarget))
        {
            return (ProgrammingTarget?)progTarget;
        }
        return null;
    }

    /// <inheritdoc />
    public short? GetShort(string key)
    {
        if (_sessiontItems.TryGetValue(key, out var value))
        {
            return (short?)value;
        }

        return null;
    }

    /// <inheritdoc />
    public void SetBool(string key, bool? value)
    {
        _sessiontItems[key] = value;
    }

    /// <inheritdoc />
    public void SetShort(string key, short? value)
    {
        _sessiontItems[key] = value;
    }

    /// <inheritdoc />
    public void SetProgTarget(string key, ProgrammingTarget progTarget)
    {
        _sessiontItems[key] = progTarget;
    }
}
