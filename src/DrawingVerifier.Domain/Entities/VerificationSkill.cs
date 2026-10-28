namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Represents a verification profile (e.g., a client profile such as BHP, Vale, etc.) containing specialized
/// instructions for the Gemini AI, aggregating the system prompt, the structured output schema and the validation rules.
/// </summary>
public class VerificationSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Skill / client profile name (e.g., "BHP - Mechanical Standard").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Specialized instructions sent to the Gemini AI to guide the PDF data extraction.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// JSON Schema defining the strict contract expected as the AI response.
    /// </summary>
    public string OutputSchemaJson { get; set; } = string.Empty;

    /// <summary>
    /// Validation rules associated with this Skill.
    /// </summary>
    public List<ValidationRule> Rules { get; set; } = [];

    public VerificationSkill()
    {
    }

    public VerificationSkill(
        Guid id,
        string name,
        string systemPrompt,
        string outputSchemaJson,
        List<ValidationRule>? rules = null)
    {
        Id = id;
        Name = name;
        SystemPrompt = systemPrompt;
        OutputSchemaJson = outputSchemaJson;
        Rules = rules ?? [];
    }
}
