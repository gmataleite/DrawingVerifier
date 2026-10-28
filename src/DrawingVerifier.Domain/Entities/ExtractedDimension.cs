namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Representa uma cota/dimensão geométrica extraída da vista do desenho técnico.
/// </summary>
public sealed record ExtractedDimension
{
    /// <summary>
    /// Identificador do balão de referência ou posição na vista (ex: "1", "12", "A").
    /// </summary>
    public string BalaoRef { get; init; } = string.Empty;

    /// <summary>
    /// Valor numérico da cota base/nominal para uso em cálculos e regras matemáticas.
    /// </summary>
    public double CotaBase { get; init; }

    /// <summary>
    /// Informações complementares da cota (ex: tolerâncias "±0.05", campo "2x", acabamento superficial).
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

