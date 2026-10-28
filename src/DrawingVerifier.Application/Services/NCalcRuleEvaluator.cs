namespace DrawingVerifier.Application.Services;

using DrawingVerifier.Domain.Entities;
using NCalc;

/// <summary>
/// Dynamic rule evaluator powered by the NCalc engine.
/// </summary>
public class NCalcRuleEvaluator : IRuleEvaluator
{
    public bool Evaluate(ValidationRule rule, Dictionary<string, object> parameters, out string errorMessage)
    {
        if (rule == null || string.IsNullOrWhiteSpace(rule.Expression))
        {
            errorMessage = "Rule syntax error: expression is null or not defined.";
            return false;
        }

        try
        {
            var expression = new Expression(rule.Expression);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    expression.Parameters[param.Key] = param.Value;
                    if (!expression.Parameters.ContainsKey(param.Key.ToLowerInvariant()))
                    {
                        expression.Parameters[param.Key.ToLowerInvariant()] = param.Value;
                    }
                }
            }

            if (expression.HasErrors())
            {
                errorMessage = "Rule syntax error: " + expression.Error?.Message;
                return false;
            }

            var result = expression.Evaluate();

            if (result is bool boolResult)
            {
                if (boolResult)
                {
                    errorMessage = string.Empty;
                    return true;
                }

                errorMessage = rule.ErrorMessage;
                return false;
            }

            if (result != null && bool.TryParse(result.ToString(), out var parsedBool))
            {
                if (parsedBool)
                {
                    errorMessage = string.Empty;
                    return true;
                }

                errorMessage = rule.ErrorMessage;
                return false;
            }

            errorMessage = rule.ErrorMessage;
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = "Rule syntax error: " + ex.Message;
            return false;
        }
    }
}
