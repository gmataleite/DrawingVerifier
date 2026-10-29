namespace DrawingVerifier.Infrastructure.Data;

using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

/// <summary>
/// Seeds the local database with default verification skills so the application is ready to use on first run.
/// </summary>
public static class DatabaseSeeder
{
    private const string DefaultOutputSchemaJson = """
        {
          "type": "object",
          "properties": {
            "metadata": {
              "type": "object",
              "properties": {
                "client": { "type": "string" },
                "drawingNumber": { "type": "string" },
                "drawnBy": { "type": "string" },
                "totalMass": { "type": "number" }
              }
            },
            "bomItems": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "item": { "type": "string" },
                  "quantity": { "type": "number" },
                  "dimensions": { "type": "string" }
                },
                "required": ["item", "quantity", "dimensions"]
              }
            },
            "dimensions": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "balloonRef": { "type": "string" },
                  "baseDimension": { "type": "number" },
                  "additionalText": { "type": "string" }
                },
                "required": ["balloonRef", "baseDimension"]
              }
            }
          },
          "required": ["metadata", "bomItems", "dimensions"]
        }
        """;

    /// <summary>
    /// Adds the default skills when the repository is empty. Existing data is never modified.
    /// </summary>
    /// <param name="repository">Skill repository to seed.</param>
    public static void SeedDefaultSkills(ISkillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var existingSkills = repository.GetAllAsync().GetAwaiter().GetResult();
        if (existingSkills.Count > 0)
        {
            return;
        }

        var defaultSkill = new VerificationSkill
        {
            Name = "Stainless Steel Ducts",
            SystemPrompt =
                "Extract dimensional data from this mechanical drawing of stainless steel ducts. " +
                "Return the title block metadata, every Bill of Materials (BOM) row, and every dimension " +
                "linked to a reference balloon. Use millimeters and numeric values only for dimensions.",
            OutputSchemaJson = DefaultOutputSchemaJson,
            Rules =
            [
                new ValidationRule(
                    Guid.NewGuid(),
                    "Overmetal Check",
                    "BomDimension == BaseDimension + 100",
                    "Overmetal measurement does not match BOM (expected BOM = Base + 100mm)")
            ]
        };

        repository.AddAsync(defaultSkill).GetAwaiter().GetResult();
    }
}
