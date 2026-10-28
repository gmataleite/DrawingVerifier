namespace DrawingVerifier.Application.UseCases;

using System.Globalization;
using DrawingVerifier.Application.DTOs;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

/// <summary>
/// Caso de uso responsável por orquestrar a extração de dados do desenho via IA
/// e a validação das regras matemáticas e de conformidade.
/// </summary>
public class AnalyzeDrawingUseCase
{
    private readonly IGeminiApiService _geminiApiService;
    private readonly IRuleEvaluator _ruleEvaluator;

    public AnalyzeDrawingUseCase(IGeminiApiService geminiApiService, IRuleEvaluator ruleEvaluator)
    {
        _geminiApiService = geminiApiService ?? throw new ArgumentNullException(nameof(geminiApiService));
        _ruleEvaluator = ruleEvaluator ?? throw new ArgumentNullException(nameof(ruleEvaluator));
    }

    /// <summary>
    /// Executa o fluxo de análise e validação do desenho técnico em PDF.
    /// </summary>
    /// <param name="fileName">Nome do arquivo PDF a ser validado.</param>
    /// <param name="pdfBase64">Conteúdo do arquivo codificado em Base64.</param>
    /// <param name="skill">Perfil de verificação (Skill) com prompt e regras.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Relatório consolidado com status da validação, erros e avisos.</returns>
    public async Task<VerificationReport> ExecuteAsync(
        string fileName,
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);

        // B) Inicialize um VerificationReport
        var report = new VerificationReport
        {
            FileName = fileName,
            SkillName = skill.Name,
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        if (string.IsNullOrWhiteSpace(pdfBase64))
        {
            report.Errors.Add("O conteúdo Base64 do PDF fornecido está vazio ou inválido.");
            report.IsValid = false;
            return report;
        }

        // A) Chame o IGeminiApiService.AnalyzeDrawingAsync passando o base64 e a skill
        ExtractedDrawingData? extractedData;
        try
        {
            extractedData = await _geminiApiService.AnalyzeDrawingAsync(pdfBase64, skill, cancellationToken);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Falha ao comunicar com o serviço de extração de IA: {ex.Message}");
            report.IsValid = false;
            return report;
        }

        if (extractedData == null)
        {
            report.Errors.Add("O serviço de IA não retornou dados estruturados para o desenho.");
            report.IsValid = false;
            return report;
        }

        var dimensions = extractedData.Dimensions ?? [];
        var bomItems = extractedData.BomItems ?? [];

        if (dimensions.Count == 0)
        {
            report.Warnings.Add("Nenhuma cota geométrica foi extraída do desenho.");
        }

        if (bomItems.Count == 0)
        {
            report.Warnings.Add("Nenhum item da lista de materiais (BOM) foi extraído do desenho.");
        }

        // C) Crie a lógica de cruzamento base: Para cada ExtractedDimension extraída que possua um BalaoRef,
        // busque o ExtractedBomItem correspondente na lista de peças.
        foreach (var dimension in dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimension.BalaoRef))
            {
                continue;
            }

            var matchingBomItem = bomItems.FirstOrDefault(b => MatchBalaoWithBomItem(dimension.BalaoRef, b.Item));

            if (matchingBomItem == null)
            {
                report.Warnings.Add($"O balão de referência '{dimension.BalaoRef}' da cota {dimension.CotaBase} não foi encontrado na lista de materiais (BOM).");
                continue;
            }

            // D) Monte um Dictionary<string, object> contendo chaves amigáveis (ex: "CotaBase", "CotaBom", "Quantidade") extraídas desses dois objetos.
            double cotaBom = 0.0;
            if (!string.IsNullOrWhiteSpace(matchingBomItem.Dimensoes))
            {
                var sanitized = matchingBomItem.Dimensoes.Trim().Replace(',', '.');
                if (double.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    cotaBom = parsed;
                }
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CotaBase"] = dimension.CotaBase,
                ["CotaBom"] = cotaBom,
                ["Quantidade"] = matchingBomItem.Quantidade,
                ["Dimensoes"] = cotaBom != 0.0 || matchingBomItem.Dimensoes?.Trim() == "0" ? cotaBom : (matchingBomItem.Dimensoes ?? string.Empty),
                ["BalaoRef"] = dimension.BalaoRef,
                ["Item"] = matchingBomItem.Item,
                ["TextoAdicional"] = dimension.TextoAdicional ?? string.Empty
            };

            // E) Itere sobre as skill.Rules passando esse dicionário para o IRuleEvaluator.
            // Se falhar, adicione o errorMessage na lista de erros do Report.
            if (skill.Rules != null)
            {
                foreach (var rule in skill.Rules)
                {
                    if (!_ruleEvaluator.Evaluate(rule, parameters, out var errorMessage))
                    {
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            report.Errors.Add(errorMessage);
                        }
                    }
                }
            }
        }

        // F) Retorne o Report preenchido.
        report.IsValid = report.Errors.Count == 0;
        return report;
    }

    private static bool MatchBalaoWithBomItem(string balaoRef, string? bomItem)
    {
        if (string.IsNullOrWhiteSpace(balaoRef) || string.IsNullOrWhiteSpace(bomItem))
        {
            return false;
        }

        var cleanBalao = balaoRef.Trim();
        var cleanBom = bomItem.Trim();

        if (string.Equals(cleanBalao, cleanBom, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (int.TryParse(cleanBalao, out var balaoInt) && int.TryParse(cleanBom, out var bomInt))
        {
            return balaoInt == bomInt;
        }

        return false;
    }
}
