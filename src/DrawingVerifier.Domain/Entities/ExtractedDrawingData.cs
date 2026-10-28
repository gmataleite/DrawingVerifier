namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Root object representing the structured data extracted by the AI from the technical drawing PDF.
/// </summary>
public sealed record ExtractedDrawingData
{
    /// <summary>
    /// General metadata from the drawing title block.
    /// </summary>
    public DrawingMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Bill of Materials (BOM) items identified in the drawing.
    /// </summary>
    public List<ExtractedBomItem> BomItems { get; init; } = [];

    /// <summary>
    /// Geometric dimensions identified in the drawing views.
    /// </summary>
    public List<ExtractedDimension> Dimensions { get; init; } = [];

    public ExtractedDrawingData()
    {
    }

    public ExtractedDrawingData(
        DrawingMetadata metadata,
        List<ExtractedBomItem>? bomItems = null,
        List<ExtractedDimension>? dimensions = null)
    {
        Metadata = metadata;
        BomItems = bomItems ?? [];
        Dimensions = dimensions ?? [];
    }
}
