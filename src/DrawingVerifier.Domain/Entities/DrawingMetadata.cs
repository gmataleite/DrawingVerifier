namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Metadados extraídos da legenda/carimbo do desenho técnico.
/// </summary>
public sealed record DrawingMetadata
{
    public string Cliente { get; init; } = string.Empty;
    public string NumeroDesenho { get; init; } = string.Empty;
    public string DesenhadoPor { get; init; } = string.Empty;
    public double? MassaTotal { get; init; }

    public DrawingMetadata()
    {
    }

    public DrawingMetadata(string cliente, string numeroDesenho, string desenhadoPor, double? massaTotal = null)
    {
        Cliente = cliente;
        NumeroDesenho = numeroDesenho;
        DesenhadoPor = desenhadoPor;
        MassaTotal = massaTotal;
    }
}

