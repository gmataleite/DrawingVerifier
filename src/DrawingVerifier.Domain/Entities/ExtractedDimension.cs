namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Represents a geometric dimension extracted from a technical drawing view.
/// </summary>
public sealed record ExtractedDimension
{
    /// <summary>
    /// Reference balloon identifier or position in the view (e.g., "1", "12", "A").
    /// </summary>
    public string BalaoRef { get; init; } = string.Empty;

    /// <summary>
    /// Nominal/base numeric value of the dimension, used in calculations and mathematical rules.
    /// </summary>
    public double CotaBase { get; init; }

    /// <summary>
    /// Additional dimension information (e.g., tolerances "±0.05", "2x" multiplier, surface finish).
    /// </summary>
    public string? TextoAdicional { get; init; }

    public ExtractedDimension()
    {
    }

    public ExtractedDimension(string balaoRef, double cotaBase, string? textoAdicional = null)
    {
        BalaoRef = balaoRef;
        CotaBase = cotaBase;
        TextoAdicional = textoAdicional;
    }
}
