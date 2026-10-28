namespace DrawingVerifier.Domain.Entities;

/// <summary>
/// Represents a mathematical/logical validation rule dynamically evaluated via NCalc.
/// </summary>
public class ValidationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Descriptive rule name (e.g., "Minimum Overmetal Check").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mathematical or boolean expression evaluated by NCalc (e.g., "BaseDimension <= BomDimension").
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly error message shown when the validation returns false.
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
