namespace Z21Dashboard.Application.Models;

/// <summary>
/// Specifies the scale ratios commonly used in model railroading.
/// </summary>
/// <remarks>Each member represents a standard model scale, where the associated value indicates the ratio of the
/// model's size to the real-world prototype (e.g., 1:87 for H0 scale). These scales are used to ensure consistency and
/// compatibility among model trains, tracks, and accessories.</remarks>
public enum ModelScale
{
    T = 450,
    ZZ = 300,
    Z = 220,
    N = 160,
    TT = 120,
    H0 = 87,
    OO = 76,
    S = 64,
    O_US = 48,
    O = 45,
    O_UK = 44,
    I = 32,
    G = 23
}
