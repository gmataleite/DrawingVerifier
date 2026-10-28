namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Represents a row extracted from the Bill of Materials (BOM).
/// </summary>
public sealed record ExtractedBomItem
{
    public string Item { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public string Dimensions { get; init; } = string.Empty;

    public ExtractedBomItem()
    {
    }

    public ExtractedBomItem(string item, double quantity, string dimensions)
    {
        Item = item;
        Quantity = quantity;
        Dimensions = dimensions;
    }
}
