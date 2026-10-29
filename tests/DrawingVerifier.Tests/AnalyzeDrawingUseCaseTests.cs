namespace DrawingVerifier.Tests;

using DrawingVerifier.Application.Services;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.Infrastructure.Data;
using Xunit;

public class AnalyzeDrawingUseCaseTests
{
    private class MockGeminiService : IGeminiApiService
    {
        private readonly ExtractedDrawingData _data;

        public MockGeminiService(ExtractedDrawingData data)
        {
            _data = data;
        }

        public Task<ExtractedDrawingData> AnalyzeDrawingAsync(string pdfBase64, VerificationSkill skill, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_data);
        }
    }

    [Fact]
    public async Task DocumentLevelRules_AreEvaluatedOnce_WithoutBalloonPrefix()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "", // Missing
                NumeroDesenho = "123",
                DesenhadoPor = "Engineer",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 0, // Missing mass
                PossuiBlocoSoldaTopo = false,
                PossuiBlocoReferenciaChapas = true,
                ExtremidadesSemConexao = 2
            },
            BomItems =
            [
                new ExtractedBomItem { Item = 1, Quantidade = 1, ComprimentoBomMm = 500 },
                new ExtractedBomItem { Item = 2, Quantidade = 1, ComprimentoBomMm = 600 }
            ],
            Dimensions =
            [
                new ExtractedDimension { BalaoRef = 1, CotaBaseMm = 500 },
                new ExtractedDimension { BalaoRef = 2, CotaBaseMm = 600 }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = "Doc Test Skill",
            Rules =
            [
                new ValidationRule(Guid.NewGuid(), "MassCheck", "MassaTotalNotaKg > 0", "Mass note is missing."),
                new ValidationRule(Guid.NewGuid(), "EndsCheck", "ExtremidadesSemConexao == 0", "Duct ends without connection."),
                new ValidationRule(Guid.NewGuid(), "TitleBlockCheck", "CamposLegendaVaziosCount == 0", "Title block has empty fields.")
            ]
        };

        var evaluator = new NCalcRuleEvaluator();
        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), evaluator);

        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.False(report.IsValid);
        Assert.Equal(3, report.Errors.Count);
        Assert.Contains(report.Errors, e => e == "Mass note is missing.");
        Assert.Contains(report.Errors, e => e == "Duct ends without connection.");
        Assert.Contains(report.Errors, e => e == "Title block has empty fields.");
        Assert.DoesNotContain(report.Errors, e => e.StartsWith("[Balloon"));
    }

    [Fact]
    public async Task ItemLevelRules_AreEvaluatedPerDimension_WithBalloonPrefix()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Client",
                NumeroDesenho = "DWG-001",
                DesenhadoPor = "Eng",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 50
            },
            BomItems =
            [
                new ExtractedBomItem
                {
                    Item = 1,
                    Quantidade = 2,
                    ComprimentoBomMm = 651,
                    DiametroBomMm = 400,
                    EspessuraBomMm = 2
                },
                new ExtractedBomItem
                {
                    Item = 2,
                    Quantidade = 1,
                    ComprimentoBomMm = 300,
                    DiametroBomMm = 500,
                    EspessuraBomMm = 3
                }
            ],
            Dimensions =
            [
                new ExtractedDimension
                {
                    BalaoRef = 1,
                    CotaBaseMm = 551,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 100,
                    DiametroVistaMm = 400,
                    EspessuraVistaMm = 2
                },
                new ExtractedDimension
                {
                    BalaoRef = 2,
                    CotaBaseMm = 300,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 50, // Invalid: expected 100
                    DiametroVistaMm = 450, // Invalid: does not match BOM 500
                    EspessuraVistaMm = 3
                }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = "Item Test Skill",
            Rules =
            [
                new ValidationRule(
                    Guid.NewGuid(),
                    "OvermetalStandard100mm",
                    "if(PossuiSobremetal, ValorSobremetal == 100, true)",
                    "Overmetal is not 100mm."),

                new ValidationRule(
                    Guid.NewGuid(),
                    "DiameterMatch",
                    "if(DiametroVista > 0 && DiametroBom > 0, DiametroVista == DiametroBom, true)",
                    "Diameter mismatch.")
            ]
        };

        var evaluator = new NCalcRuleEvaluator();
        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), evaluator);

        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.False(report.IsValid);
        Assert.Equal(2, report.Errors.Count);

        // Balloon 1 passed all rules
        Assert.DoesNotContain(report.Errors, e => e.StartsWith("[Balloon 1]"));

        // Balloon 2 failed OvermetalStandard100mm and DiameterMatch
        Assert.Contains(report.Errors, e => e == "[Balloon 2] Overmetal is not 100mm.");
        Assert.Contains(report.Errors, e => e == "[Balloon 2] Diameter mismatch.");
    }

    [Fact]
    public async Task DefaultSeededRules_PassForCompliantDrawing()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Vale S.A.",
                NumeroDesenho = "30-260096-05-07-00_A",
                Revisao = "A",
                DesenhadoPor = "Leonardo",
                VerificadoPor = "Carlos",
                AprovadoPor = "Marcos",
                MassaTotalNotaKg = 85.5,
                PossuiBlocoSoldaTopo = true,
                PossuiBlocoRugosidade = true,
                PossuiBlocoReferenciaChapas = true,
                ExtremidadesSemConexao = 0
            },
            BomItems =
            [
                new ExtractedBomItem
                {
                    Item = 1,
                    Quantidade = 1,
                    Descricao = "Duto Reto",
                    DimensoesTextoBruto = "Ø400x#2x651",
                    DiametroBomMm = 400,
                    EspessuraBomMm = 2,
                    ComprimentoBomMm = 651,
                    MassaUnitariaKg = 85.5,
                    PossuiBalaoNasVistas = true
                }
            ],
            Dimensions =
            [
                new ExtractedDimension
                {
                    BalaoRef = 1,
                    CotaBaseMm = 551,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 100,
                    DiametroVistaMm = 400,
                    EspessuraVistaMm = 2
                }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = DatabaseSeeder.DefaultSkillName,
            Rules = DatabaseSeeder.CreateDefaultRules()
        };

        var evaluator = new NCalcRuleEvaluator();
        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), evaluator);

        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
    }
}
