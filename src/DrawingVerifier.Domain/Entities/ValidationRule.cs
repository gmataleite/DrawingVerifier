namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Representa uma regra matemática/lógica de validação avaliada dinamicamente via NCalc.
/// </summary>
public class ValidationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome descritivo da regra (ex: "Validação de Sobremetal Mínimo").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Expressão matemática ou booleana avaliada pelo NCalc (ex: "CotaBase <= Dimensoes").
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem de erro amigável caso a validação retorne falso ou falhe.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    public ValidationRule()
    {
    }

    public ValidationRule(Guid id, string name, string expression, string errorMessage)
    {
        Id = id;
        Name = name;
        Expression = expression;
        ErrorMessage = errorMessage;
    }
}

