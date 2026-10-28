namespace DrawingVerifier.Application.Services;

using DrawingVerifier.Domain.Entities;

/// <summary>
/// Contract for the dynamic rule evaluation engine.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates a logical/mathematical rule with the supplied parameters.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="parameters">Dictionary with the parameters available to the expression.</param>
    /// <param name="errorMessage">Error message produced when the rule fails or a syntax error occurs.</param>
    /// <returns>True if the expression was evaluated successfully and resulted in true; otherwise, false.</returns>
    bool Evaluate(ValidationRule rule, Dictionary<string, object> parameters, out string errorMessage);
}
