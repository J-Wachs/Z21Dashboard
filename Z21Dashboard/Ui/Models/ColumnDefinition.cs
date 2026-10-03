namespace Z21Dashboard.Ui.Models;

/// <summary>
/// Defines a column for use in a data grid component.
/// </summary>
/// <typeparam name="TItem">The type of items in the grid</typeparam>
public class ColumnDefinition<TItem>
{
    /// <summary>
    /// The header text for this column
    /// </summary>
    public required string Header { get; set; }

    /// <summary>
    /// Function to extract the value from an item for display in this column
    /// </summary>
    public required Func<TItem, object?> ValueSelector { get; set; }

    /// <summary>
    /// Optional CSS class to apply to cells in this column
    /// </summary>
    public string? CellCssClass { get; set; }

    /// <summary>
    /// Optional width for this column (e.g., "150px", "20%")
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Optional alignment for the column (e.g., "left", "center", "right")
    /// </summary>
    public string Alignment { get; set; } = "left";
}
