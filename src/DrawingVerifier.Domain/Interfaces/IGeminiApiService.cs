namespace DrawingVerifier.Domain.Interfaces;

using DrawingVerifier.Domain.Entities;

/// <summary>
/// Contract for communicating with the AI service (Google Gemini 1.5 Pro)
/// for multimodal analysis of engineering PDFs and structured data extraction.
/// </summary>
public interface IGeminiApiService
{
    /// <summary>
    /// Sends the Base64-encoded PDF to the AI for analysis using the prompt and schema defined in the Skill.
    /// </summary>
    /// <param name="pdfBase64">PDF file content as a Base64 string.</param>
    /// <param name="skill">Profile containing the extraction instructions and the corresponding JSON schema.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous request.</param>
    /// <returns>Structured data extracted from the mechanical drawing.</returns>
    Task<ExtractedDrawingData> AnalyzeDrawingAsync(
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default);
}
