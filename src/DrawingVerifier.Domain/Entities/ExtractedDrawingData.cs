namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Objeto raiz que representa os dados estruturados extraídos do desenho técnico em PDF pela IA.
/// </summary>
public sealed record ExtractedDrawingData
{
    /// <summary>
    /// Metadados gerais do cabeçalho/legenda do desenho.
    /// </summary>
    public DrawingMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Itens da Lista de Materiais (BOM) identificados no desenho.
    /// </summary>
    public List<ExtractedBomItem> BomItems { get; init; } = [];

    /// <summary>
    /// Dimensões e cotas geométricas identificadas nas vistas do desenho.
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

