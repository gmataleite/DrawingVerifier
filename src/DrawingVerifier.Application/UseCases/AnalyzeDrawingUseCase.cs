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

        foreach (var dimension in dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimension.BalloonRef))
            {
                continue;
            }

            var matchingBomItem = bomItems.FirstOrDefault(b => MatchBalloonWithBomItem(dimension.BalloonRef, b.Item));

            if (matchingBomItem == null)
            {
                report.Warnings.Add($"Reference balloon '{dimension.BalloonRef}' (dimension {dimension.BaseDimension}) was not found in the Bill of Materials (BOM).");
                continue;
            }

            double bomDimension = 0.0;
            if (!string.IsNullOrWhiteSpace(matchingBomItem.Dimensions))
            {
                var sanitized = matchingBomItem.Dimensions.Trim().Replace(',', '.');
                if (double.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    bomDimension = parsed;
                }
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["BaseDimension"] = dimension.BaseDimension,
                ["BomDimension"] = bomDimension,
                ["Quantity"] = matchingBomItem.Quantity,
                ["Dimensions"] = bomDimension != 0.0 || matchingBomItem.Dimensions?.Trim() == "0" ? bomDimension : (matchingBomItem.Dimensions ?? string.Empty),
                ["BalloonRef"] = dimension.BalloonRef,
                ["Item"] = matchingBomItem.Item,
                ["AdditionalText"] = dimension.AdditionalText ?? string.Empty,

                // Backward-compatibility aliases for existing rules
                ["CotaBase"] = dimension.BaseDimension,
                ["CotaBom"] = bomDimension,
                ["Quantidade"] = matchingBomItem.Quantity,
                ["Dimensoes"] = bomDimension != 0.0 || matchingBomItem.Dimensions?.Trim() == "0" ? bomDimension : (matchingBomItem.Dimensions ?? string.Empty),
                ["BalaoRef"] = dimension.BalloonRef,
                ["TextoAdicional"] = dimension.AdditionalText ?? string.Empty
            };

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

        report.IsValid = report.Errors.Count == 0;
        return report;
    }

    private static bool MatchBalloonWithBomItem(string balloonRef, string? bomItem)
    {
        if (string.IsNullOrWhiteSpace(balloonRef) || string.IsNullOrWhiteSpace(bomItem))
        {
            return false;
        }

        var cleanBalloon = balloonRef.Trim();
        var cleanBom = bomItem.Trim();

        if (string.Equals(cleanBalloon, cleanBom, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (int.TryParse(cleanBalloon, out var balloonInt) && int.TryParse(cleanBom, out var bomInt))
        {
            return balloonInt == bomInt;
        }

        return false;
    }
}
