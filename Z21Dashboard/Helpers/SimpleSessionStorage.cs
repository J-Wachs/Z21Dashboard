using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Helpers;

/// <summary>
/// Session storage of various properties
/// </summary>
internal class SimpleSessionStorage : ISessionStorage
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
    public ushort? GetUshort(string key)
    {
        if (_sessiontItems.TryGetValue(key, out var value))
        {
            return (ushort?)value;
        }

        return null;
    }

    /// <inheritdoc />
    public void SetBool(string key, bool? value)
    {
        _sessiontItems[key] = value;
    }

    /// <inheritdoc />
    public void SetUshort(string key, ushort? value)
    {
        _sessiontItems[key] = value;
    }

    /// <inheritdoc />
    public void SetProgTarget(string key, ProgrammingTarget progTarget)
    {
        _sessiontItems[key] = progTarget;
    }
}
