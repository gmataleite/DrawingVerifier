namespace DrawingVerifier.Infrastructure.Gemini;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Cliente HTTP para a API do Google Gemini (1.5 Pro) para análise multimodal de PDFs de engenharia.
/// </summary>
public class GeminiApiHttpClient : IGeminiApiService
{
    private const string DefaultGeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiApiHttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<ExtractedDrawingData> AnalyzeDrawingAsync(
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfBase64);
        ArgumentNullException.ThrowIfNull(skill);

        var apiKey = GetApiKey();
        var endpoint = GetEndpoint();
        var requestUri = $"{endpoint}?key={apiKey}";

        // 1. Map generationConfig with response_mime_type and response_schema
        var generationConfig = new Dictionary<string, object?>
        {
            ["response_mime_type"] = "application/json"
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

    private string GetApiKey()
    {
        var apiKey = _configuration["Gemini:ApiKey"]
                     ?? _configuration["GEMINI_API_KEY"]
                     ?? _configuration["GeminiApiKey"]
                     ?? _configuration["Gemini:Key"]
                     ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured. Please define 'Gemini:ApiKey' or the environment variable 'GEMINI_API_KEY'.");
        }

        return apiKey;
    }

    private string GetEndpoint()
    {
        var configuredEndpoint = _configuration["Gemini:Endpoint"];
        return !string.IsNullOrWhiteSpace(configuredEndpoint)
            ? configuredEndpoint
            : DefaultGeminiEndpoint;
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
