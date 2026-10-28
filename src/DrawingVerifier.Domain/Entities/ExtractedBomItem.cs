namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Representa uma linha extraída da Lista de Materiais (Bill of Materials - BOM).
/// </summary>
public sealed record ExtractedBomItem
{
    public string Item { get; init; } = string.Empty;
    public double Quantidade { get; init; }
    public string Dimensoes { get; init; } = string.Empty;

    public ExtractedBomItem()
    {
    }

    public ExtractedBomItem(string item, double quantidade, string dimensoes)
    {
        Item = item;
        Quantidade = quantidade;
        Dimensoes = dimensoes;
    }
}

