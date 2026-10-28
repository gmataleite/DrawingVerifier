namespace DrawingVerifier.Application.Interfaces;

using DrawingVerifier.Application.DTOs;

/// <summary>
/// Contract for generating the final verification report document.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a PDF report summarizing the verification results.
    /// </summary>
    /// <param name="reports">Verification reports to include in the document.</param>
    /// <param name="outputPath">Absolute path where the PDF file will be written.</param>
    void GeneratePdfReport(IEnumerable<VerificationReport> reports, string outputPath);
}
