namespace DrawingVerifier.Application.DTOs;

/// <summary>
/// Relatório consolidado da verificação do desenho técnico contra as regras da Skill.
/// </summary>
public sealed record VerificationReport
{
    /// <summary>
    /// Nome do arquivo PDF analisado.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Nome da Skill utilizada na verificação.
    /// </summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Indica se o desenho passou em todas as regras sem erros.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Lista de inconsistências e falhas identificadas pelas regras de validação.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Avisos ou inconformidades não bloqueantes (ex: balão sem item correspondente na BOM).
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    public VerificationReport()
    {
    }

    public VerificationReport(
        string fileName,
        string skillName,
        bool isValid = true,
        List<string>? errors = null,
        List<string>? warnings = null)
    {
        FileName = fileName;
        SkillName = skillName;
        IsValid = isValid;
        Errors = errors ?? [];
        Warnings = warnings ?? [];
    }
}
