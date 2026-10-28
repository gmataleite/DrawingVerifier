namespace DrawingVerifier.Application.DTOs;

/// <summary>
/// Consolidated report of a technical drawing verification against the Skill rules.
/// </summary>
public sealed record VerificationReport
{
    /// <summary>
    /// Name of the analyzed PDF file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Skill used for the verification.
    /// </summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the drawing passed all rules without errors.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Inconsistencies and failures identified by the validation rules.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Non-blocking warnings (e.g., a balloon without a matching BOM item).
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
