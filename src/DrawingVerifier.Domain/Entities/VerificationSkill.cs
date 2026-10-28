namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Representa o perfil de verificação (ex: perfil de cliente como Relco),
/// agregando o prompt do sistema para IA, schema estruturado e regras de validação.
/// </summary>
public class VerificationSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome da Skill / Perfil do Cliente (ex: "Relco - Padrão Mecânico").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Instruções especializadas enviadas à IA do Gemini para orientar a extração do PDF.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Esquema JSON (JSON Schema) que define o contrato estrito esperado como resposta da IA.
    /// </summary>
    public string OutputSchemaJson { get; set; } = string.Empty;

    /// <summary>
    /// Lista de regras de validação associadas a esta Skill.
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

