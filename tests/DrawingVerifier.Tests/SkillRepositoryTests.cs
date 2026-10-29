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

    [Fact]
    public async Task DatabaseSeeder_SeedsAllDefaultRulesAndUpgradesLegacySkills()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
        try
        {
            var repo = new SkillLiteDbRepository($"Filename={tempDbPath};Connection=shared");

            // 1. Initial seed into empty repository
            DatabaseSeeder.SeedDefaultSkills(repo);
            var skills = await repo.GetAllAsync();
            Assert.Single(skills);
            var seededSkill = skills[0];
            Assert.Equal(DatabaseSeeder.DefaultSkillName, seededSkill.Name);
            Assert.Equal(8, seededSkill.Rules.Count);
            Assert.Contains(seededSkill.Rules, r => r.Name == "MassNoteCheck");
            Assert.Contains(seededSkill.Rules, r => r.Name == "TitleBlockCheck");
            Assert.Contains(seededSkill.Rules, r => r.Name == "OvermetalLengthMatch");

            // 2. Simulate legacy skill with 1 rule and test migration
            seededSkill.Rules = [new ValidationRule(Guid.NewGuid(), "OldRule", "1==1", "Old")];
            seededSkill.OutputSchemaJson = "{}";
            await repo.UpdateAsync(seededSkill);

            DatabaseSeeder.SeedDefaultSkills(repo);
            var reloaded = await repo.GetByIdAsync(seededSkill.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(8, reloaded.Rules.Count);
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
