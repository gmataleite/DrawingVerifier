namespace DrawingVerifier.Domain.Entities;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a row extracted from the Bill of Materials (BOM).
/// </summary>
public sealed record ExtractedBomItem
{
    public int Item { get; init; }
    public int Quantidade { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public string DimensoesTextoBruto { get; init; } = string.Empty;
    public double? DiametroBomMm { get; init; }
    public double? EspessuraBomMm { get; init; }
    public double? ComprimentoBomMm { get; init; }
    public double? MassaUnitariaKg { get; init; }
    public bool PossuiBalaoNasVistas { get; init; } = true;

    // Backward-compatibility aliases
    [JsonIgnore]
    public double Quantity => Quantidade;

    [JsonIgnore]
    public string Dimensions => DimensoesTextoBruto;

    public ExtractedBomItem()
    {
    }

    public ExtractedBomItem(
        int item,
        int quantidade,
        string descricao = "",
        string dimensoesTextoBruto = "",
        double? diametroBomMm = null,
        double? espessuraBomMm = null,
        double? comprimentoBomMm = null,
        double? massaUnitariaKg = null,
        bool possuiBalaoNasVistas = true)
    {
        Item = item;
        Quantidade = quantidade;
        Descricao = descricao;
        DimensoesTextoBruto = dimensoesTextoBruto;
        DiametroBomMm = diametroBomMm;
        EspessuraBomMm = espessuraBomMm;
        ComprimentoBomMm = comprimentoBomMm;
        MassaUnitariaKg = massaUnitariaKg;
        PossuiBalaoNasVistas = possuiBalaoNasVistas;
    }
}
