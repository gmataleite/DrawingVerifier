namespace DrawingVerifier.Application.UseCases;

using System.Globalization;
using DrawingVerifier.Application.DTOs;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

/// <summary>
/// Use case that orchestrates the AI-based drawing data extraction
/// and the evaluation of the mathematical and compliance rules.
/// </summary>
public class AnalyzeDrawingUseCase
{
    private static readonly string[] ItemLevelKeywords =
    [
        "CotaBase",
        "ComprimentoBom",
        "PossuiSobremetal",
        "ValorSobremetal",
        "DiametroVista",
        "DiametroBom",
        "EspessuraVista",
        "EspessuraBom",
        "Quantidade",
        "BalaoRef",
        "BaseDimension",
        "BomDimension",
        "Quantity",
        "BalloonRef"
    ];

    private readonly IGeminiApiService _geminiApiService;
    private readonly IRuleEvaluator _ruleEvaluator;

    public AnalyzeDrawingUseCase(IGeminiApiService geminiApiService, IRuleEvaluator ruleEvaluator)
    {
        _geminiApiService = geminiApiService ?? throw new ArgumentNullException(nameof(geminiApiService));
        _ruleEvaluator = ruleEvaluator ?? throw new ArgumentNullException(nameof(ruleEvaluator));
    }

    /// <summary>
    /// Runs the analysis and validation flow for a technical drawing PDF.
    /// </summary>
    /// <param name="fileName">Name of the PDF file being validated.</param>
    /// <param name="pdfBase64">File content encoded in Base64.</param>
    /// <param name="skill">Verification profile (Skill) containing the prompt and rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Consolidated report with validation status, errors and warnings.</returns>
    public async Task<VerificationReport> ExecuteAsync(
        string fileName,
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);

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
            report.Errors.Add("The supplied PDF Base64 content is empty or invalid.");
            report.IsValid = false;
            return report;
        }

        ExtractedDrawingData? extractedData;
        try
        {
            extractedData = await _geminiApiService.AnalyzeDrawingAsync(pdfBase64, skill, cancellationToken);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Failed to communicate with the AI extraction service: {ex.Message}");
            report.IsValid = false;
            return report;
        }

        if (extractedData == null)
        {
            report.Errors.Add("The AI service returned no structured data for the drawing.");
            report.IsValid = false;
            return report;
        }

        var metadata = extractedData.Metadata ?? new DrawingMetadata();
        var dimensions = extractedData.Dimensions ?? [];
        var bomItems = extractedData.BomItems ?? [];

        if (dimensions.Count == 0)
        {
            report.Warnings.Add("No geometric dimensions were extracted from the drawing.");
        }

        if (bomItems.Count == 0)
        {
            report.Warnings.Add("No Bill of Materials (BOM) items were extracted from the drawing.");
        }

        // Calculate document-level metrics
        var camposLegendaVaziosCount = 0;
        if (string.IsNullOrWhiteSpace(metadata.Cliente)) camposLegendaVaziosCount++;
        if (string.IsNullOrWhiteSpace(metadata.NumeroDesenho)) camposLegendaVaziosCount++;
        if (string.IsNullOrWhiteSpace(metadata.DesenhadoPor)) camposLegendaVaziosCount++;
        if (string.IsNullOrWhiteSpace(metadata.AprovadoPor)) camposLegendaVaziosCount++;

        var somaMassaBomKg = bomItems.Sum(b => (double)b.Quantidade * (b.MassaUnitariaKg ?? 0.0));
        var itensBomSemBalaoCount = bomItems.Count(b => !b.PossuiBalaoNasVistas);

        var documentParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["MassaTotalNotaKg"] = metadata.MassaTotalNotaKg ?? 0.0,
            ["SomaMassaBomKg"] = somaMassaBomKg,
            ["PossuiBlocoSoldaTopo"] = metadata.PossuiBlocoSoldaTopo,
            ["PossuiBlocoRugosidade"] = metadata.PossuiBlocoRugosidade,
            ["PossuiBlocoReferenciaChapas"] = metadata.PossuiBlocoReferenciaChapas,
            ["ExtremidadesSemConexao"] = metadata.ExtremidadesSemConexao,
            ["ItensBomSemBalaoCount"] = itensBomSemBalaoCount,
            ["CamposLegendaVaziosCount"] = camposLegendaVaziosCount
        };

        var rules = skill.Rules ?? [];
        var documentRules = rules.Where(r => !IsItemLevelRule(r)).ToList();
        var itemRules = rules.Where(IsItemLevelRule).ToList();

        // 1. Evaluate Document-Level Rules ONLY ONCE per drawing (Anti-duplication)
        foreach (var rule in documentRules)
        {
            if (!_ruleEvaluator.Evaluate(rule, documentParameters, out var errorMessage))
            {
                var rawTemplate = errorMessage?.StartsWith("Rule syntax error", StringComparison.OrdinalIgnoreCase) == true
                    ? errorMessage
                    : (!string.IsNullOrWhiteSpace(rule.ErrorMessage) ? rule.ErrorMessage : errorMessage ?? string.Empty);

                var interpolatedMessage = InterpolateErrorMessage(rawTemplate, documentParameters);

                if (!string.IsNullOrWhiteSpace(interpolatedMessage))
                {
                    report.Errors.Add(interpolatedMessage);
                }
            }
        }

        // 2. Evaluate Item-Level Rules & Spatial Validation for each dimension matching a BOM item
        if (dimensions.Count > 0)
        {
            foreach (var dimension in dimensions)
            {
                var balloonRefInt = dimension.BalaoRef;
                if (!balloonRefInt.HasValue && int.TryParse(dimension.BalloonRef, out var parsedRef))
                {
                    balloonRefInt = parsedRef;
                }

                if (!balloonRefInt.HasValue)
                {
                    continue;
                }

                var matchingBomItem = bomItems.FirstOrDefault(b => b.Item == balloonRefInt.Value);
                if (matchingBomItem == null)
                {
                    report.Warnings.Add($"Reference balloon '{balloonRefInt}' was not found in the Bill of Materials (BOM).");
                    continue;
                }

                // Validate spatial proximity between balloon and dimension bounding boxes
                if (dimension.BalaoBoundingBox is { Count: 4 } bBox &&
                    dimension.CotaBoundingBox is { Count: 4 } cBox)
                {
                    double bCenterY = (bBox[0] + bBox[2]) / 2.0;
                    double bCenterX = (bBox[1] + bBox[3]) / 2.0;
                    double cCenterY = (cBox[0] + cBox[2]) / 2.0;
                    double cCenterX = (cBox[1] + cBox[3]) / 2.0;

                    double deltaX = bCenterX - cCenterX;
                    double deltaY = bCenterY - cCenterY;
                    double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                    if (distance > 600)
                    {
                        report.Warnings.Add($"[Balloon {balloonRefInt.Value}] Spatial distance between balloon and dimension is unusually large ({Math.Round(distance)} units). Verify leader line alignment.");
                    }
                }

                var comprimentoBom = matchingBomItem.ComprimentoBomMm ?? 0.0;
                if (comprimentoBom == 0.0 && !string.IsNullOrWhiteSpace(matchingBomItem.DimensoesTextoBruto))
                {
                    var sanitized = matchingBomItem.DimensoesTextoBruto.Trim().Replace(',', '.');
                    if (double.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal))
                    {
                        comprimentoBom = parsedVal;
                    }
                }

                var itemParameters = new Dictionary<string, object>(documentParameters, StringComparer.OrdinalIgnoreCase)
                {
                    ["BalaoRef"] = balloonRefInt.Value,
                    ["PossuiSobremetal"] = dimension.PossuiSobremetal,
                    ["CotaBase"] = dimension.CotaBaseMm ?? 0.0,
                    ["ValorSobremetal"] = dimension.ValorSobremetalMm ?? 0.0,
                    ["ComprimentoBom"] = comprimentoBom,
                    ["DiametroVista"] = dimension.DiametroVistaMm ?? 0.0,
                    ["DiametroBom"] = matchingBomItem.DiametroBomMm ?? 0.0,
                    ["EspessuraVista"] = dimension.EspessuraVistaMm ?? 0.0,
                    ["EspessuraBom"] = matchingBomItem.EspessuraBomMm ?? 0.0,
                    ["Quantidade"] = matchingBomItem.Quantidade,

                    // Backward-compatibility aliases
                    ["BaseDimension"] = dimension.CotaBaseMm ?? 0.0,
                    ["BomDimension"] = comprimentoBom,
                    ["Quantity"] = matchingBomItem.Quantidade,
                    ["BalloonRef"] = balloonRefInt.Value.ToString(),
                    ["Item"] = matchingBomItem.Item
                };

                if (itemRules.Count > 0)
                {
                    foreach (var rule in itemRules)
                    {
                        if (!_ruleEvaluator.Evaluate(rule, itemParameters, out var errorMessage))
                        {
                            var rawTemplate = errorMessage?.StartsWith("Rule syntax error", StringComparison.OrdinalIgnoreCase) == true
                                ? errorMessage
                                : (!string.IsNullOrWhiteSpace(rule.ErrorMessage) ? rule.ErrorMessage : errorMessage ?? string.Empty);

                            var interpolatedMessage = InterpolateErrorMessage(rawTemplate, itemParameters);

                            if (!string.IsNullOrWhiteSpace(interpolatedMessage))
                            {
                                if (interpolatedMessage.StartsWith($"[Balloon {balloonRefInt.Value}]", StringComparison.OrdinalIgnoreCase) ||
                                    interpolatedMessage.StartsWith($"[Balão {balloonRefInt.Value}]", StringComparison.OrdinalIgnoreCase) ||
                                    interpolatedMessage.StartsWith("[Balloon", StringComparison.OrdinalIgnoreCase) ||
                                    interpolatedMessage.StartsWith("[Balão", StringComparison.OrdinalIgnoreCase))
                                {
                                    report.Errors.Add(interpolatedMessage);
                                }
                                else
                                {
                                    report.Errors.Add($"[Balloon {balloonRefInt.Value}] {interpolatedMessage}");
                                }
                            }
                        }
                    }
                }
            }
        }

        report.IsValid = report.Errors.Count == 0;
        return report;
    }

    private string InterpolateErrorMessage(string template, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrWhiteSpace(template) || parameters == null)
        {
            return template ?? string.Empty;
        }

        foreach (var key in parameters.Keys)
        {
            template = template.Replace($"{{{key}}}", parameters[key]?.ToString() ?? "null", StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    private static bool IsItemLevelRule(ValidationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Expression))
        {
            return false;
        }

        return ItemLevelKeywords.Any(keyword => rule.Expression.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
