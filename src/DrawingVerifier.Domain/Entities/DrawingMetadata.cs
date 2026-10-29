namespace DrawingVerifier.Domain.Entities;

using System.Text.Json.Serialization;

/// <summary>
/// Metadata extracted from the technical drawing title block and general notes.
/// </summary>
public sealed record DrawingMetadata
{
    public string Cliente { get; init; } = string.Empty;
    public string NumeroDesenho { get; init; } = string.Empty;
    public string Revisao { get; init; } = string.Empty;
    public string DesenhadoPor { get; init; } = string.Empty;
    public string VerificadoPor { get; init; } = string.Empty;
    public string AprovadoPor { get; init; } = string.Empty;
    public double? MassaTotalNotaKg { get; init; }
    public bool PossuiBlocoSoldaTopo { get; init; }
    public bool PossuiBlocoRugosidade { get; init; }
    public bool PossuiBlocoReferenciaChapas { get; init; }
    public int ExtremidadesSemConexao { get; init; }

    // Backward-compatibility aliases
    [JsonIgnore]
    public string Client => Cliente;

    [JsonIgnore]
    public string DrawingNumber => NumeroDesenho;

    [JsonIgnore]
    public string DrawnBy => DesenhadoPor;

    [JsonIgnore]
    public double? TotalMass => MassaTotalNotaKg;

    public DrawingMetadata()
    {
    }

    public DrawingMetadata(
        string cliente,
        string numeroDesenho,
        string desenhadoPor,
        double? massaTotalNotaKg = null,
        string revisao = "",
        string verificadoPor = "",
        string aprovadoPor = "",
        bool possuiBlocoSoldaTopo = false,
        bool possuiBlocoRugosidade = false,
        bool possuiBlocoReferenciaChapas = false,
        int extremidadesSemConexao = 0)
    {
        Cliente = cliente;
        NumeroDesenho = numeroDesenho;
        DesenhadoPor = desenhadoPor;
        MassaTotalNotaKg = massaTotalNotaKg;
        Revisao = revisao;
        VerificadoPor = verificadoPor;
        AprovadoPor = aprovadoPor;
        PossuiBlocoSoldaTopo = possuiBlocoSoldaTopo;
        PossuiBlocoRugosidade = possuiBlocoRugosidade;
        PossuiBlocoReferenciaChapas = possuiBlocoReferenciaChapas;
        ExtremidadesSemConexao = extremidadesSemConexao;
    }
}
