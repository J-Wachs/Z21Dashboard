using System.ComponentModel.DataAnnotations;

namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// This locomotive metadata class is used for the edit of the information.
/// The setter will set the "IsChanged" flag in the class.
/// </summary>
public class LocomotiveMeta
{
    public bool IsChanged { get; private set; } = false;

    [MaxLength(50)]
    public required string Name
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(20)]
    public string? ShortName
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(30)]
    public string? Manufacturer
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(10)]
    public string? CatalogueNumber
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(10)]
    public string? Class
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(10)]
    public string? UnitNumber
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Description
    {
        get;
        set => SetField(ref field, value);
    }

    public DateOnly? Purchased
    {
        get;
        set => SetField(ref field, value);
    }

    public decimal? PurchasePrice
    {
        get;
        set => SetField(ref field, value);
    }


    public string? Modifications
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Remarks
    {
        get;
        set => SetField(ref field, value);
    }

    public void ClearChanged()
    {
        IsChanged = false;
    }

    internal void Clear()
    {
        Name = string.Empty;
        ShortName = null;
        Manufacturer = null;
        CatalogueNumber = null;
        Class = null;
        UnitNumber = null;
        Description = null;
        Purchased = null;
        PurchasePrice = null;
        Modifications = null;
        Remarks = null;
        IsChanged = false;
    }

    /// <summary>
    /// Helper method to set a field value and mark the object as changed if the value is different from the current value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="value"></param>
    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value) is false)
        {
            field = value;
            IsChanged = true;
        }
    }
}
