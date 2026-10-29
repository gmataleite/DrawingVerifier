namespace DrawingVerifier.Tests;

using System.Collections.ObjectModel;
using System.IO;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Infrastructure.Data;
using Xunit;

public class SkillRepositoryTests
{
    [Fact]
    public async Task CanPersistAndRetrieveSkillWithRules()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
        try
        {
            var repo = new SkillLiteDbRepository($"Filename={tempDbPath};Connection=shared");
            var skill = new VerificationSkill
            {
                Name = "Test Skill",
                SystemPrompt = "Prompt",
                OutputSchemaJson = "{}",
                Rules =
                [
                    new ValidationRule(Guid.NewGuid(), "Rule 1", "1 == 1", "Error 1")
                ]
            };

            await repo.AddAsync(skill);

            var retrieved = await repo.GetByIdAsync(skill.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Test Skill", retrieved.Name);
            Assert.Single(retrieved.Rules);
            Assert.Equal("Rule 1", retrieved.Rules[0].Name);
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }
}
