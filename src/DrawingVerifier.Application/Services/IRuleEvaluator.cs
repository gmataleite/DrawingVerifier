namespace DrawingVerifier.Application.Services;

using DrawingVerifier.Domain.Entities;

/// <summary>
/// Contrato para o motor de avaliação de regras dinâmicas.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Avalia uma regra lógica/matemática com os parâmetros fornecidos.
    /// </summary>
    /// <param name="rule">A regra a ser avaliada.</param>
    /// <param name="parameters">Dicionário com os parâmetros disponíveis para a expressão.</param>
    /// <param name="errorMessage">Mensagem de erro gerada caso a regra falhe ou ocorra erro de sintaxe.</param>
    /// <returns>True se a expressão foi avaliada com sucesso e resultou em verdadeiro; caso contrário, False.</returns>
    bool Evaluate(ValidationRule rule, Dictionary<string, object> parameters, out string errorMessage);
}
