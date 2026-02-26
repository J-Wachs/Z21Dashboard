using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

internal interface ISessionStorage
{
    /// <summary>
    /// Retrieves the Boolean value associated with the specified key, if it exists.
    /// </summary>
    /// <remarks>Use this method to access configuration settings or options stored as Boolean values. Ensure
    /// that the specified key corresponds to a valid entry in the underlying storage.</remarks>
    /// <param name="key">The key that identifies the Boolean value to retrieve. This value cannot be null or empty.</param>
    /// <returns>A nullable Boolean value: <see langword="true"/> if the key is found and its value is <see langword="true"/>;
    /// <see langword="false"/> if the key is found and its value is <see langword="false"/>; otherwise, <see
    /// langword="null"/> if the key does not exist.</returns>
    bool? GetBool(string key, bool? defaultValue = null);

    /// <summary>
    /// Retrieves the programming target associated with the specified key.
    /// </summary>
    /// <remarks>If the key does not correspond to any existing programming target, the method returns null.
    /// Ensure that the key is valid to avoid unexpected results.</remarks>
    /// <param name="key">The unique identifier for the programming target to retrieve. This key must not be null or empty.</param>
    /// <returns>A nullable ProgrammingTarget object representing the associated programming target if found; otherwise, null.</returns>
    ProgrammingTarget? GetProgTarget(string key);

    /// <summary>
    /// Retrieves the unsigned 16-bit integer value associated with the specified key.
    /// </summary>
    /// <remarks>Use this method to access configuration values or settings stored as unsigned 16-bit
    /// integers. Ensure that the provided key corresponds to a valid entry in the underlying storage.</remarks>
    /// <param name="key">The key whose associated unsigned 16-bit integer value is to be retrieved. This parameter cannot be null or
    /// empty.</param>
    /// <returns>An unsigned 16-bit integer value if the key exists; otherwise, null.</returns>
    ushort? GetUshort(string key);

    /// <summary>
    /// Sets the boolean value associated with the specified key in the session storage.
    /// </summary>
    /// <remarks>If the specified key already exists, its value is overwritten. Ensure that the key
    /// corresponds to a valid configuration or setting expected by the application.</remarks>
    /// <param name="key">The key that identifies the value to set. Cannot be null or empty.</param>
    /// <param name="value">The boolean value to assign to the specified key. Specify <see langword="true"/> to enable the key; otherwise,
    /// <see langword="false"/>.</param>
    void SetBool(string key, bool? value);

    /// <summary>
    /// Sets the programming target associated with the specified key.
    /// </summary>
    /// <remarks>This method updates the programming target for the given key. Ensure that the key exists
    /// before calling this method to avoid unexpected behavior.</remarks>
    /// <param name="key">The unique identifier for the programming target. This key must not be null or empty.</param>
    /// <param name="progTarget">The programming target to be set. This parameter defines the target configuration for the specified key.</param>
    void SetProgTarget(string key, ProgrammingTarget progTarget);

    /// <summary>
    /// Sets the value associated with the specified key to the provided unsigned short value.
    /// </summary>
    /// <remarks>This method updates the value for the specified key in the underlying storage. If the key
    /// does not exist, it will be created.</remarks>
    /// <param name="key">The key associated with the value to set. This cannot be null or empty.</param>
    /// <param name="value">The unsigned short value to set for the specified key. If null, the key will be removed.</param>
    void SetUshort(string key, ushort? value);
}
