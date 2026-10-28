namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Metadata extracted from the technical drawing title block.
/// </summary>
public sealed record DrawingMetadata
{
    public string Client { get; init; } = string.Empty;
    public string DrawingNumber { get; init; } = string.Empty;
    public string DrawnBy { get; init; } = string.Empty;
    public double? TotalMass { get; init; }

    public DrawingMetadata()
    {
    }

    public DrawingMetadata(string client, string drawingNumber, string drawnBy, double? totalMass = null)
    {
        Client = client;
        DrawingNumber = drawingNumber;
        DrawnBy = drawnBy;
        TotalMass = totalMass;
    }
}
