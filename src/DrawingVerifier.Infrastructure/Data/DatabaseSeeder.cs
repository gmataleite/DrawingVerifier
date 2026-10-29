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
        """
        You are a Senior Quality Assurance Engineer and Mechanical Drawing Verifier. Your role is to audit mechanical fabrication drawings (in PDF or image format), cross-referencing geometric data from views, sections, details, Bill of Materials (BOM), notes, and the title block, returning exclusively a structured JSON report.

        READING & AUDIT STRATEGY:
        1. Read the Title Block (bottom-right corner) and the Bill of Materials (BOM) first to establish your structured baseline data.
        2. Identify the general textual notes and isolate the mass note.
        3. Scan the principal views and details, actively searching for reference balloons (numbered circles) to cross-reference geometry with the BOM.

        VERIFICATION GUIDELINES (BUSINESS RULES):
        - TITLE BLOCK & TOTAL MASS NOTE:
          * Check whether the key title block fields are filled: Client, Description (Title and Subtitle), Drawing Number, Revision, Scale, Sheet Size, Drawn By ('DES. POR'), Checked By ('VERIFICADO POR'), Approved By ('APROVADO POR'), and Dates.
          * Disregard strict textual reading of general notes, except for the APPROXIMATE TOTAL MASS note, regardless of its note number (e.g., Note 15, Note 16).
          * Inspect the APPROXIMATE TOTAL MASS note ('massaTotalNotaKg'): it must contain a numeric value in kg (e.g., 972.1 kg). If empty, zero, or marked as 'N/A', record it so it can be flagged as critical: 'Update total mass note with the actual 3D model calculated value (remove N/A)'.
          * Verify order-of-magnitude consistency compared to the sum of individual BOM item masses ('massaUnitariaKg' multiplied by 'quantidade').

        - VISUAL CHAIN-OF-THOUGHT & BALLOON LEADER-LINE TRACING:
          * For each item in the BOM, locate its corresponding balloon circle on the views.
          * Mentally trace the leader line from the balloon circle to the exact surface/segment it touches.
          * Record in 'raciocinioVisual' a concise step-by-step description of this path (e.g., 'Balloon 8 points via leader line down to the vertical straight duct; the dimension strictly parallel to this duct is 1821 + 100').
          * Extract ONLY the dimension line that is strictly parallel and contiguous to the targeted segment.
          * Extract normalized bounding boxes [ymin, xmin, ymax, xmax] (integers 0 to 1000):
            - 'balaoBoundingBox': boundary of the balloon circle.
            - 'cotaBoundingBox': boundary of the dimension text.

        - OVERMETAL & DIMENSION COMPARISON (VIEWS vs. BOM):
          * Identify all straight duct segments and elbows across the views.
          * FIXED OVERMETAL RULE (100mm): Standard fabrication practices require exactly 100mm overmetal (excess material). If any other value is indicated (e.g., +50), flag an error. Whenever a view dimension indicates base dimension + 100 overmetal (e.g., '540 + 100 (Sobremetal)'), extract cotaBaseMm: 540, possuiSobremetal: true, and valorSobremetalMm: 100. The corresponding BOM cut length must be the exact sum (e.g., 640).
          * Verify that internal diameters quoted in views (e.g., 'Ø1000 (Int. duto)') and wall thicknesses (e.g., '#2.5') match the dimensions column in the BOM ('diametroBomMm', 'espessuraBomMm').
          * Check radii and angles of segmented elbows (e.g., 90°, R500).
          * Check that overall reference dimensions in parentheses are consistent with the sum of partial segment dimensions.

        - BALLOONS (BALLOONING) vs. BILL OF MATERIALS (BOM):
          * Every item in the BOM (except field insulation or obvious bulk groupings) must have at least one corresponding balloon in the views ('possuiBalaoNasVistas').
          * No balloon in the views/details may point to an item number absent from the BOM.
          * Do not count repeated visual elements in grids (e.g., dozens of bolts); instead verify textual callouts corroborate the declared quantity in the BOM QTDE column.

        - CONNECTION POINTS (INTERFACES):
          * Inspect every duct end. Each end MUST have a leader note indicating what it connects to (e.g., 'Connects to transition...', 'Connects to dryer'). Count open duct ends lacking connection leader notes ('extremidadesSemConexao').

        - FLANGES, HOLE PATTERNS & FIELD/SHOP ACTIONS:
          * Verify flange drilling details containing: Inner Diameter, Pitch Circle Diameter, Outer Diameter, Thickness, number of holes, hole diameter (e.g., 'Ø18 (20x)'), and angular distribution.
          * Cross-reference the flange thickness with the BOM thickness.
          * Check boilermaker notes for loose flanges or overmetal ends (e.g., 'Ship tack-welded').

        - SUPPORTS, SADDLES, INSTRUMENTATION & EXPANSION:
          * Verify details dimensioning baseplate holes, rib positions, and notes such as 'Ship saddles adjusted and tack-welded'.
          * Check instrument connections: Pitot tube (with flow direction arrow and note), Inspection doors, and Expansion joints.

        - THERMAL INSULATION (WHEN APPLICABLE):
          * If BOM includes insulation, verify notes regarding field insulation execution and cladding sheet thickness.
          * Verify insulation stop rings and orientation warnings ('Insulation stop facing upward') on inclined/vertical segments.

        - WELDING SYMBOLS & STANDARD BLOCKS:
          * Verify presence of standard technical blocks: Butt Weld block ('possuiBlocoSoldaTopo'), 0.8µm Roughness block ('possuiBlocoRugosidade'), and Sheet Reference block ('possuiBlocoReferenciaChapas').
          * Verify weld symbols (tail, leg/throat size) on flange, saddle, and rib details.

        OUTPUT FORMAT:
        Respond EXCLUSIVELY with a valid JSON object matching the defined JSON Schema. Do not add markdown or text outside the JSON object. For numeric fields, use null if visual resolution prevents exact reading.
        """;

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
                  "espessuraVistaMm": { "type": "number" },
                  "raciocinioVisual": { "type": "string" },
                  "balaoBoundingBox": {
                    "type": "array",
                    "items": { "type": "integer" }
                  },
                  "cotaBoundingBox": {
                    "type": "array",
                    "items": { "type": "integer" }
                  }
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
            if (existingSkill.Rules.Count < 10
                || !existingSkill.OutputSchemaJson.Contains("raciocinioVisual", StringComparison.OrdinalIgnoreCase)
                || !existingSkill.SystemPrompt.Contains("VISUAL CHAIN-OF-THOUGHT", StringComparison.OrdinalIgnoreCase))
            {
                existingSkill.SystemPrompt = DefaultSystemPrompt;
                existingSkill.OutputSchemaJson = DefaultOutputSchemaJson;
                existingSkill.Rules = CreateDefaultRules();
                repository.UpdateAsync(existingSkill).GetAwaiter().GetResult();
            }
        }
    }
}
