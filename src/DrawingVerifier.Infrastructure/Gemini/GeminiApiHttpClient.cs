namespace DrawingVerifier.Infrastructure.Gemini;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrawingVerifier.Application.Interfaces;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

/// <summary>
/// HTTP client for the Google Gemini (3.1 Flash Lite) API, used for multimodal analysis of engineering PDFs.
/// </summary>
public class GeminiApiHttpClient : IGeminiApiService
{
    private const string DefaultGeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent";

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    public GeminiApiHttpClient(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
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

        var requestUri = $"{DefaultGeminiEndpoint}?key={apiKey}";

        // 1. Map generationConfig with response_mime_type and response_schema
        var generationConfig = new Dictionary<string, object?>
        {
            ["response_mime_type"] = "application/json",
            ["temperature"] = 0.0, // 0.0 ensures maximum determinism (no hallucinations)
        };

        if (!string.IsNullOrWhiteSpace(skill.OutputSchemaJson))
        {
            using var schemaDoc = JsonDocument.Parse(skill.OutputSchemaJson);
            generationConfig["response_schema"] = schemaDoc.RootElement.Clone();
        }

        // 2. Map contents with inline_data for the PDF
        var contents = new object[]
        {
            new Dictionary<string, object>
            {
                ["parts"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["inline_data"] = new Dictionary<string, string>
                        {
                            ["mime_type"] = "application/pdf",
                            ["data"] = pdfBase64
                        }
                    }
                }
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

        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(requestUri, requestContent, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API call failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}");
        }

        var extractedJson = ExtractJsonTextFromResponse(responseBody);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var extractedData = JsonSerializer.Deserialize<ExtractedDrawingData>(extractedJson, serializerOptions);

        return extractedData ?? new ExtractedDrawingData();
    }

    private static string ExtractJsonTextFromResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"Gemini API response contained no candidates: {responseBody}");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0 ||
            !parts[0].TryGetProperty("text", out var textElement))
        {
            throw new InvalidOperationException(
                $"Gemini API response did not contain candidates[0].content.parts[0].text: {responseBody}");
        }

        var rawText = textElement.GetString() ?? string.Empty;
        return CleanMarkdownJsonFences(rawText);
    }

    private static string CleanMarkdownJsonFences(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[7..];
        }
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[3..];
        }

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^3];
        }

        return trimmed.Trim();
    }
}
