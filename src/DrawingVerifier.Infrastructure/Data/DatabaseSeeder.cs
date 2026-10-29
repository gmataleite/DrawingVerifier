namespace DrawingVerifier.Infrastructure.Data;

using System.Collections.ObjectModel;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

/// <summary>
/// Seeds the local database with default verification skills and standard industrial duct validation rules.
/// </summary>
public static class DatabaseSeeder
{
    public const string DefaultSkillName = "Stainless Steel Ducts";

    public const string DefaultSystemPrompt =
        "Extract dimensional and compliance data from this mechanical fabrication drawing of industrial stainless steel ducts.\n" +
        "Return the title block metadata, standard technical blocks, general notes, every Bill of Materials (BOM) row, and view dimensions linked to reference balloons.\n\n" +
        "BOM Dimensions Parsing Instructions:\n" +
        "Parse raw BOM dimensions formatted like 'Ø400x#2x651' (Diameter x Thickness x Length) into distinct numeric fields:\n" +
        "- diametroBomMm: diameter in mm (e.g., 400)\n" +
        "- espessuraBomMm: sheet thickness in mm (e.g., 2)\n" +
        "- comprimentoBomMm: cut length in mm (e.g., 651)\n" +
        "Also extract 'massaUnitariaKg' and check whether each item balloon appears visually on the views ('possuiBalaoNasVistas').\n\n" +
        "View Dimensions & Overmetal Parsing Instructions:\n" +
        "- For dimensions showing excess material like '551 + 100 (Sobremetal)': extract cotaBaseMm: 551, possuiSobremetal: true, and valorSobremetalMm: 100.\n" +
        "- Extract view internal diameter ('diametroVistaMm') and view wall thickness ('espessuraVistaMm') when indicated.\n\n" +
        "Title Block & Notes Inspection:\n" +
        "- Extract cliente, numeroDesenho, revisao, desenhadoPor, verificadoPor, aprovadoPor, and approximate total mass note ('massaTotalNotaKg').\n" +
        "- Inspect whether standard technical requirement blocks exist: possuiBlocoSoldaTopo, possuiBlocoRugosidade, possuiBlocoReferenciaChapas.\n" +
        "- Count any open duct ends lacking connection leader notes ('extremidadesSemConexao').";

    public const string DefaultOutputSchemaJson = """
        {
          "type": "object",
          "properties": {
            "metadata": {
              "type": "object",
              "properties": {
                "cliente": { "type": "string" },
                "numeroDesenho": { "type": "string" },
                "revisao": { "type": "string" },
                "desenhadoPor": { "type": "string" },
                "verificadoPor": { "type": "string" },
                "aprovadoPor": { "type": "string" },
                "massaTotalNotaKg": { "type": "number" },
                "possuiBlocoSoldaTopo": { "type": "boolean" },
                "possuiBlocoRugosidade": { "type": "boolean" },
                "possuiBlocoReferenciaChapas": { "type": "boolean" },
                "extremidadesSemConexao": { "type": "integer" }
              },
              "required": ["cliente", "numeroDesenho", "desenhadoPor"]
            },
            "bomItems": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "item": { "type": "integer" },
                  "quantidade": { "type": "integer" },
                  "descricao": { "type": "string" },
                  "dimensoesTextoBruto": { "type": "string" },
                  "diametroBomMm": { "type": "number" },
                  "espessuraBomMm": { "type": "number" },
                  "comprimentoBomMm": { "type": "number" },
                  "massaUnitariaKg": { "type": "number" },
                  "possuiBalaoNasVistas": { "type": "boolean" }
                },
                "required": ["item", "quantidade"]
              }
            },
            "dimensions": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "balaoRef": { "type": "integer" },
                  "cotaBaseMm": { "type": "number" },
                  "valorSobremetalMm": { "type": "number" },
                  "possuiSobremetal": { "type": "boolean" },
                  "diametroVistaMm": { "type": "number" },
                  "espessuraVistaMm": { "type": "number" }
                },
                "required": ["balaoRef", "cotaBaseMm"]
              }
            }
          },
          "required": ["metadata", "bomItems", "dimensions"]
        }
        """;

    /// <summary>
    /// Creates the standard set of NCalc verification rules for industrial ducts.
    /// </summary>
    public static ObservableCollection<ValidationRule> CreateDefaultRules() =>
    [
        new ValidationRule(
            Guid.NewGuid(),
            "Check Title Block Fields",
            "CamposLegendaVaziosCount == 0",
            "Mandatory title block information is missing. Check: Client, Drawing No., Drawn By, or Approved By."),

        new ValidationRule(
            Guid.NewGuid(),
            "Check Total Mass Note",
            "MassaTotalNotaKg > 0",
            "CRITICAL: The APPROXIMATE TOTAL MASS note is missing, zero, or N/A. Update with 3D model value."),

        new ValidationRule(
            Guid.NewGuid(),
            "Compare Mass (BOM vs Note)",
            "if(MassaTotalNotaKg > 0 && SomaMassaBomKg > 0, Abs(MassaTotalNotaKg - SomaMassaBomKg) < (MassaTotalNotaKg * 0.05), true)",
            "Warning: Declared mass in note ({MassaTotalNotaKg}kg) deviates by more than 5% from BOM items total ({SomaMassaBomKg}kg)."),

        new ValidationRule(
            Guid.NewGuid(),
            "Overmetal Must Be 100mm",
            "if(PossuiSobremetal, ValorSobremetal == 100, true)",
            "[Balloon {BalaoRef}] Out of RELCO standard: Overmetal indicated on view is {ValorSobremetal}mm (Expected: 100mm)."),

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
            "[Balloon {BalaoRef}] Sheet thickness mismatch. (View: #{EspessuraVista}. BOM: #{EspessuraBom})."),

        new ValidationRule(
            Guid.NewGuid(),
            "Mandatory Standard Blocks",
            "PossuiBlocoSoldaTopo && PossuiBlocoReferenciaChapas && PossuiBlocoRugosidade",
            "Missing mandatory standard blocks on sheet (Butt Weld, Sheet Reference, or 0.8µm Roughness)."),

        new ValidationRule(
            Guid.NewGuid(),
            "Orphan Connection Ends",
            "ExtremidadesSemConexao == 0",
            "There are duct ends without leader notes indicating connection/interface points."),

        new ValidationRule(
            Guid.NewGuid(),
            "All BOM Items Ballooned",
            "ItensBomSemBalaoCount == 0",
            "There are {ItensBomSemBalaoCount} item(s) in the Bill of Materials without indicative balloons on the views.")
    ];

    /// <summary>
    /// Adds or updates default skills in the repository.
    /// </summary>
    /// <param name="repository">Skill repository to seed.</param>
    public static void SeedDefaultSkills(ISkillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var existingSkills = repository.GetAllAsync().GetAwaiter().GetResult();
        var existingSkill = existingSkills.FirstOrDefault(s => s.Name == DefaultSkillName);

        if (existingSkill == null)
        {
            var defaultSkill = new VerificationSkill
            {
                Name = DefaultSkillName,
                SystemPrompt = DefaultSystemPrompt,
                OutputSchemaJson = DefaultOutputSchemaJson,
                Rules = CreateDefaultRules()
            };

            repository.AddAsync(defaultSkill).GetAwaiter().GetResult();
        }
        else
        {
            // Automatic migration: If the existing skill has old legacy rules or schema, upgrade it
            if (existingSkill.Rules.Count < 10 || !existingSkill.OutputSchemaJson.Contains("diametroBomMm", StringComparison.OrdinalIgnoreCase))
            {
                existingSkill.SystemPrompt = DefaultSystemPrompt;
                existingSkill.OutputSchemaJson = DefaultOutputSchemaJson;
                existingSkill.Rules = CreateDefaultRules();
                repository.UpdateAsync(existingSkill).GetAwaiter().GetResult();
            }
        }
    }
}
