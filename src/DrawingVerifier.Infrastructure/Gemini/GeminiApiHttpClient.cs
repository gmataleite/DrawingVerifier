namespace DrawingVerifier.Infrastructure.Gemini;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrawingVerifier.Application.Interfaces;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.Infrastructure.Services;

/// <summary>
/// HTTP client for the Google Gemini API, supporting multimodal analysis with high-DPI quadrant tiling.
/// </summary>
public class GeminiApiHttpClient : IGeminiApiService
{
    public const string DefaultGeminiModel = "gemini-3.8-flash";

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly IPdfRasterizerService _pdfRasterizerService;

    public GeminiApiHttpClient(
        HttpClient httpClient,
        ISettingsService settingsService,
        IPdfRasterizerService? pdfRasterizerService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pdfRasterizerService = pdfRasterizerService ?? new PdfRasterizerService();
    }

    public async Task<ExtractedDrawingData> AnalyzeDrawingAsync(
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfBase64);
        ArgumentNullException.ThrowIfNull(skill);

        var apiKey = _settingsService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API Key is not configured. Please configure it in Settings.");
        }

        var selectedModel = _settingsService.GetSelectedGeminiModel();
        if (string.IsNullOrWhiteSpace(selectedModel))
        {
            selectedModel = DefaultGeminiModel;
        }

        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{selectedModel}:generateContent?key={apiKey}";

        // 1. Map generationConfig with response_mime_type and response_schema
        var generationConfig = new Dictionary<string, object?>
        {
            ["response_mime_type"] = "application/json",
            ["temperature"] = 0.0, // 0.0 ensures maximum determinism
        };

        if (!string.IsNullOrWhiteSpace(skill.OutputSchemaJson))
        {
            using var schemaDoc = JsonDocument.Parse(skill.OutputSchemaJson);
            generationConfig["response_schema"] = schemaDoc.RootElement.Clone();
        }

        // 2. Prepare multimodal parts: high-DPI quadrant tiling or raw PDF
        var parts = new List<object>();

        var enableTiling = _settingsService.GetEnableHighDpiTiling();
        bool tilingApplied = false;

        if (enableTiling)
        {
            try
            {
                var pdfBytes = Convert.FromBase64String(pdfBase64);
                var tiles = _pdfRasterizerService.RasterizeAndTile(pdfBytes, dpi: 300, overlapPercentage: 0.15);

                if (tiles != null && tiles.Count > 0)
                {
                    foreach (var tile in tiles)
                    {
                        parts.Add(new Dictionary<string, object>
                        {
                            ["inline_data"] = new Dictionary<string, string>
                            {
                                ["mime_type"] = "image/jpeg",
                                ["data"] = Convert.ToBase64String(tile)
                            }
                        });
                    }
                    tilingApplied = true;
                }
            }
            catch
            {
                // Fallback to sending raw PDF if rasterization fails
                tilingApplied = false;
            }
        }

        if (!tilingApplied)
        {
            parts.Add(new Dictionary<string, object>
            {
                ["inline_data"] = new Dictionary<string, string>
                {
                    ["mime_type"] = "application/pdf",
                    ["data"] = pdfBase64
                }
            });
        }

        var contents = new object[]
        {
            new Dictionary<string, object>
            {
                ["parts"] = parts.ToArray()
            }
        };

        // 3. Assemble root payload
        var payload = new Dictionary<string, object?>
        {
            ["contents"] = contents,
            ["generationConfig"] = generationConfig
        };

        // 4. Map system_instruction if present
        if (!string.IsNullOrWhiteSpace(skill.SystemPrompt))
        {
            payload["system_instruction"] = new Dictionary<string, object>
            {
                ["parts"] = new object[]
                {
                    new Dictionary<string, string>
                    {
                        ["text"] = skill.SystemPrompt
                    }
                }
            };
        }

        var requestJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = $"Gemini API request failed with status code {response.StatusCode}.";
            try
            {
                using var errorDoc = JsonDocument.Parse(responseContent);
                if (errorDoc.RootElement.TryGetProperty("error", out var errorObj) &&
                    errorObj.TryGetProperty("message", out var msgElem))
                {
                    errorMessage += $" Detail: {msgElem.GetString()}";
                }
            }
            catch
            {
                errorMessage += $" Raw response: {responseContent}";
            }

            throw new InvalidOperationException(errorMessage);
        }

        // 5. Parse Gemini generateContent response
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini API returned no candidates in the response.");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var contentElem) ||
            !contentElem.TryGetProperty("parts", out var respParts) ||
            respParts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini candidate did not contain any content parts.");
        }

        var textPart = respParts[0];
        if (!textPart.TryGetProperty("text", out var textElem))
        {
            throw new InvalidOperationException("Gemini content part does not contain a 'text' property.");
        }

        var rawJson = textElem.GetString();
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new InvalidOperationException("Gemini returned an empty structured text response.");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var extractedData = JsonSerializer.Deserialize<ExtractedDrawingData>(rawJson, options);
        if (extractedData == null)
        {
            throw new InvalidOperationException("Failed to deserialize Gemini JSON into ExtractedDrawingData.");
        }

        return extractedData;
    }
}
