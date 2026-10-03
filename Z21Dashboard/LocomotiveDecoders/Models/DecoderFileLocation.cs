namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Specifies the storage location of a decoder definition file.
/// </summary>
public enum DecoderFileLocation
{
    /// <summary>
    /// The file is embedded within the application package (Read-only, standard files).
    /// </summary>
    Application,

    /// <summary>
    /// The file is located in the local file system (Read/write, user custom files).
    /// </summary>
    LocalStorage
}
