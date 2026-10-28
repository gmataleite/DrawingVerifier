namespace DrawingVerifier.Infrastructure.Services;

using DrawingVerifier.Application.DTOs;
using DrawingVerifier.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

/// <summary>
/// Generates the drawing verification PDF report using the QuestPDF fluent API.
/// </summary>
public class QuestPdfReportGenerator : IReportGenerator
{
    static QuestPdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GeneratePdfReport(IEnumerable<VerificationReport> reports, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // Materialize once and drop null entries so the layout never iterates a null/lazy sequence.
        var reportList = (reports ?? []).Where(r => r is not null).ToList();
        var generatedAt = DateTime.Now;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Text("Drawing Verification Report")
                        .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    header.Item().Text($"Generated on {generatedAt:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    header.Item().PaddingTop(4).Text(BuildSummary(reportList))
                        .FontSize(10);
                    header.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(10);

                    if (reportList.Count == 0)
                    {
                        content.Item().Text("No verification results to display.")
                            .Italic().FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    foreach (var report in reportList)
                    {
                        content.Item().Element(item => ComposeReport(item, report));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        })
        .GeneratePdf(outputPath);
    }

    private static string BuildSummary(IReadOnlyCollection<VerificationReport> reports)
    {
        var passed = reports.Count(r => r.IsValid);
        return $"Files verified: {reports.Count}  |  Passed: {passed}  |  Failed: {reports.Count - passed}";
    }

    private static void ComposeReport(IContainer container, VerificationReport report)
    {
        var errors = report.Errors ?? [];
        var warnings = report.Warnings ?? [];

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(4);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(string.IsNullOrWhiteSpace(report.FileName) ? "(unnamed file)" : report.FileName)
                        .FontSize(12).SemiBold();

                    row.AutoItem().Text(report.IsValid ? "VALID" : "INVALID")
                        .SemiBold()
                        .FontColor(report.IsValid ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });

                if (!string.IsNullOrWhiteSpace(report.SkillName))
                {
                    column.Item().Text($"Skill: {report.SkillName}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                }

                if (errors.Count == 0 && warnings.Count == 0)
                {
                    column.Item().Text("No issues found.").FontColor(Colors.Green.Darken2);
                    return;
                }

                if (errors.Count > 0)
                {
                    column.Item().PaddingTop(4).Text($"Errors ({errors.Count})")
                        .SemiBold().FontColor(Colors.Red.Darken2);

                    foreach (var error in errors)
                    {
                        column.Item().PaddingLeft(10).Text($"• {error}").FontColor(Colors.Red.Medium);
                    }
                }

                if (warnings.Count > 0)
                {
                    column.Item().PaddingTop(4).Text($"Warnings ({warnings.Count})")
                        .SemiBold().FontColor(Colors.Orange.Darken2);

                    foreach (var warning in warnings)
                    {
                        column.Item().PaddingLeft(10).Text($"• {warning}").FontColor(Colors.Orange.Darken1);
                    }
                }
            });
    }
}
