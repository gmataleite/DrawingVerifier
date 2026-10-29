namespace DrawingVerifier.Domain.Entities;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a geometric dimension extracted from a technical drawing view.
/// </summary>
public sealed record ExtractedDimension
{
    /// <summary>
    /// Reference balloon identifier (e.g., 1, 2, 5).
    /// </summary>
    public int? BalaoRef { get; init; }

    /// <summary>
    /// Nominal/base finished dimension in millimeters.
    /// </summary>
    public double? CotaBaseMm { get; init; }

    /// <summary>
    /// Excess material / overmetal value in mm (e.g. 100 if "+ 100 (Sobremetal)", 0 or null if none).
    /// </summary>
    public double? ValorSobremetalMm { get; init; }

    /// <summary>
    /// Indicates whether overmetal was specified for this dimension.
    /// </summary>
    public bool PossuiSobremetal { get; init; }

    /// <summary>
    /// Internal/nominal diameter indicated on the view in millimeters.
    /// </summary>
    public double? DiametroVistaMm { get; init; }

    /// <summary>
    /// Wall/plate thickness indicated on the view in millimeters.
    /// </summary>
    public double? EspessuraVistaMm { get; init; }

    // Backward-compatibility aliases
    [JsonIgnore]
    public string BalloonRef => BalaoRef?.ToString() ?? string.Empty;

    [JsonIgnore]
    public double BaseDimension => CotaBaseMm ?? 0.0;

    public string? AdditionalText { get; init; }

    public ExtractedDimension()
    {
    }

    public ExtractedDimension(
        int? balaoRef,
        double? cotaBaseMm,
        double? valorSobremetalMm = null,
        bool possuiSobremetal = false,
        double? diametroVistaMm = null,
        double? espessuraVistaMm = null,
        string? additionalText = null)
    {
        BalaoRef = balaoRef;
        CotaBaseMm = cotaBaseMm;
        ValorSobremetalMm = valorSobremetalMm;
        PossuiSobremetal = possuiSobremetal;
        DiametroVistaMm = diametroVistaMm;
        EspessuraVistaMm = espessuraVistaMm;
        AdditionalText = additionalText;
    }
}
