namespace DrawingVerifier.Tests;

using System;
using System.IO;
using System.Threading.Tasks;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.Infrastructure.Data;
using DrawingVerifier.Infrastructure.Services;
using QuestPDF.Fluent;
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
                Cliente = "Industrial Client Inc.",
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

    [Fact]
    public async Task DynamicErrorMessageInterpolation_InterpolatesDocumentLevelRuleParameters()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Test Client",
                NumeroDesenho = "DWG-100",
                DesenhadoPor = "Eng",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 100.0,
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
                    MassaUnitariaKg = 80.0, // 80kg != 100kg (> 5% deviation)
                    PossuiBalaoNasVistas = true
                }
            ],
            Dimensions = []
        };

        var skill = new VerificationSkill
        {
            Name = "Dynamic Error Message Test",
            Rules =
            [
                new ValidationRule(
                    Guid.NewGuid(),
                    "Compare Mass (BOM vs Note)",
                    "if(MassaTotalNotaKg > 0 && SomaMassaBomKg > 0, Abs(MassaTotalNotaKg - SomaMassaBomKg) < (MassaTotalNotaKg * 0.05), true)",
                    "Warning: Declared mass in note ({MassaTotalNotaKg}kg) deviates by more than 5% from BOM items total ({SomaMassaBomKg}kg).")
            ]
        };

        var evaluator = new NCalcRuleEvaluator();
        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), evaluator);

        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.False(report.IsValid);
        Assert.Single(report.Errors);
        Assert.Equal("Warning: Declared mass in note (100kg) deviates by more than 5% from BOM items total (80kg).", report.Errors[0]);
    }

    [Fact]
    public async Task DynamicErrorMessageInterpolation_InterpolatesItemLevelRuleParameters()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Test Client",
                NumeroDesenho = "DWG-100",
                DesenhadoPor = "Eng",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 50.0
            },
            BomItems =
            [
                new ExtractedBomItem
                {
                    Item = 3,
                    Quantidade = 1,
                    ComprimentoBomMm = 620.0,
                    DiametroBomMm = 450.0,
                    EspessuraBomMm = 3.0,
                    PossuiBalaoNasVistas = true
                }
            ],
            Dimensions =
            [
                new ExtractedDimension
                {
                    BalaoRef = 3,
                    CotaBaseMm = 500.0,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 120.0, // Invalid: not 100mm
                    DiametroVistaMm = 400.0,   // Invalid: 400 != 450
                    EspessuraVistaMm = 2.0     // Invalid: 2 != 3
                }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = "Dynamic Item Error Message Test",
            Rules =
            [
                new ValidationRule(
                    Guid.NewGuid(),
                    "Overmetal Must Be 100mm",
                    "if(PossuiSobremetal, ValorSobremetal == 100, true)",
                    "[Balloon {BalaoRef}] Out of standard: Overmetal indicated on view is {ValorSobremetal}mm (Expected: 100mm)."),

                new ValidationRule(
                    Guid.NewGuid(),
                    "Overmetal Length Math",
                    "if(PossuiSobremetal && CotaBase > 0 && ComprimentoBom > 0, ComprimentoBom == CotaBase + 100, true)",
                    "[Balloon {BalaoRef}] BOM cut length does not match view dimension. (View: {CotaBase} + 100. BOM: {ComprimentoBom}mm)."),

                new ValidationRule(
                    Guid.NewGuid(),
                    "Diameter Match (View vs BOM)",
                    "if(DiametroVista > 0 && DiametroBom > 0, DiametroVista == DiametroBom, true)",
                    "[Balloon {BalaoRef}] Diameter mismatch. (View: Ø{DiametroVista}. BOM: Ø{DiametroBom})."),

                new ValidationRule(
                    Guid.NewGuid(),
                    "Thickness Match (View vs BOM)",
                    "if(EspessuraVista > 0 && EspessuraBom > 0, EspessuraVista == EspessuraBom, true)",
                    "[Balloon {BalaoRef}] Sheet thickness mismatch. (View: #{EspessuraVista}. BOM: #{EspessuraBom}).")
            ]
        };

        var evaluator = new NCalcRuleEvaluator();
        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), evaluator);

        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.False(report.IsValid);
        Assert.Equal(4, report.Errors.Count);

        Assert.Contains(report.Errors, e => e == "[Balloon 3] Out of standard: Overmetal indicated on view is 120mm (Expected: 100mm).");
        Assert.Contains(report.Errors, e => e == "[Balloon 3] BOM cut length does not match view dimension. (View: 500 + 100. BOM: 620mm).");
        Assert.Contains(report.Errors, e => e == "[Balloon 3] Diameter mismatch. (View: Ø400. BOM: Ø450).");
        Assert.Contains(report.Errors, e => e == "[Balloon 3] Sheet thickness mismatch. (View: #2. BOM: #3).");
    }

    [Fact]
    public async Task SpatialBoundingBoxValidation_EmitsWarning_WhenBalloonAndDimensionAreDistant()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Test Client",
                NumeroDesenho = "DWG-200",
                DesenhadoPor = "Engineer",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 50.0
            },
            BomItems =
            [
                new ExtractedBomItem
                {
                    Item = 8,
                    Quantidade = 1,
                    ComprimentoBomMm = 1920.0,
                    PossuiBalaoNasVistas = true
                }
            ],
            Dimensions =
            [
                new ExtractedDimension
                {
                    BalaoRef = 8,
                    CotaBaseMm = 956.0,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 100.0,
                    RaciocinioVisual = "Traced leader line",
                    // Far top-left balloon, far bottom-right dimension (distance ~ 1180 units)
                    BalaoBoundingBox = [50, 50, 80, 80],
                    CotaBoundingBox = [900, 900, 930, 930]
                }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = "Spatial Test Skill",
            Rules = []
        };

        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), new NCalcRuleEvaluator());
        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.Contains(report.Warnings, w => w.Contains("[Balloon 8] Spatial distance between balloon and dimension is unusually large"));
    }

    [Fact]
    public async Task SpatialBoundingBoxValidation_DoesNotEmitWarning_WhenBalloonAndDimensionAreClose()
    {
        var data = new ExtractedDrawingData
        {
            Metadata = new DrawingMetadata
            {
                Cliente = "Test Client",
                NumeroDesenho = "DWG-201",
                DesenhadoPor = "Engineer",
                AprovadoPor = "Lead",
                MassaTotalNotaKg = 50.0
            },
            BomItems =
            [
                new ExtractedBomItem
                {
                    Item = 8,
                    Quantidade = 1,
                    ComprimentoBomMm = 1920.0,
                    PossuiBalaoNasVistas = true
                }
            ],
            Dimensions =
            [
                new ExtractedDimension
                {
                    BalaoRef = 8,
                    CotaBaseMm = 1821.0,
                    PossuiSobremetal = true,
                    ValorSobremetalMm = 100.0,
                    RaciocinioVisual = "Balloon 8 points directly to vertical duct",
                    // Nearby coordinates (~120 units apart)
                    BalaoBoundingBox = [300, 400, 340, 440],
                    CotaBoundingBox = [320, 280, 360, 330]
                }
            ]
        };

        var skill = new VerificationSkill
        {
            Name = "Spatial Close Skill",
            Rules = []
        };

        var useCase = new AnalyzeDrawingUseCase(new MockGeminiService(data), new NCalcRuleEvaluator());
        var report = await useCase.ExecuteAsync("drawing.pdf", "valid-base64", skill);

        Assert.DoesNotContain(report.Warnings, w => w.Contains("Spatial distance"));
    }

    [Fact]
    public void SettingsService_PersistsTilingAndModelPreferences()
    {
        var tempSettingsFile = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid()}.dat");
        try
        {
            var settings = new WindowsSettingsService(tempSettingsFile);

            // Default checks
            Assert.True(settings.GetEnableHighDpiTiling());
            Assert.Equal("gemini-2.5-flash", settings.GetSelectedGeminiModel());

            // Update checks
            settings.SetEnableHighDpiTiling(false);
            settings.SetSelectedGeminiModel("gemini-2.5-pro");

            Assert.False(settings.GetEnableHighDpiTiling());
            Assert.Equal("gemini-2.5-pro", settings.GetSelectedGeminiModel());
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
            {
                File.Delete(tempSettingsFile);
            }
            var dir = Path.GetDirectoryName(tempSettingsFile);
            var prefFile = Path.Combine(dir ?? "", "appsettings.json");
            if (File.Exists(prefFile))
            {
                File.Delete(prefFile);
            }
        }
    }

    [Fact]
    public void PdfRasterizerService_GeneratesOverviewAndFourQuadrants()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Content().Text("Test Technical Drawing Page");
            });
        }).GeneratePdf();

        var rasterizer = new PdfRasterizerService();
        var tiles = rasterizer.RasterizeAndTile(pdfBytes, dpi: 150);

        Assert.NotNull(tiles);
        Assert.Equal(5, tiles.Count); // 1 overview + 4 quadrants
        Assert.All(tiles, tile => Assert.True(tile.Length > 0));
    }
}
