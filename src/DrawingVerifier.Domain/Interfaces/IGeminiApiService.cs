namespace DrawingVerifier.Domain.Interfaces;

using DrawingVerifier.Domain.Entities;

/// <summary>
/// Contrato para comunicação com o serviço de IA (Google Gemini 1.5 Pro)
/// para análise multimodal de PDFs de engenharia e extração de dados estruturados.
/// </summary>
public interface IGeminiApiService
{
    /// <summary>
    /// Envia o PDF codificado em Base64 para análise pela IA utilizando o prompt e schema definidos na Skill.
    /// </summary>
    /// <param name="pdfBase64">Conteúdo do arquivo PDF em string Base64.</param>
    /// <param name="skill">Perfil com as instruções de extração e schema JSON correspondente.</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição assíncrona.</param>
    /// <returns>Dados estruturados extraídos do desenho mecânico.</returns>
    Task<ExtractedDrawingData> AnalyzeDrawingAsync(
        string pdfBase64,
        VerificationSkill skill,
        CancellationToken cancellationToken = default);
}

