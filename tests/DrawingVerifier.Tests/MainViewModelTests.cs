namespace DrawingVerifier.Tests;

using DrawingVerifier.Application.Interfaces;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.UI.ViewModels;
using Xunit;

public class MainViewModelTests
{
    private class DummySkillRepository : ISkillRepository
    {
        public List<VerificationSkill> Skills { get; set; } = [];

        public Task<VerificationSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Skills.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<VerificationSkill>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VerificationSkill>>(Skills);

        public Task AddAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
        {
            Skills.Add(skill);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Skills.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }
    }

    private class DummySettingsService : ISettingsService
    {
        public string? GetApiKey() => "test-key";
        public void SaveApiKey(string apiKey) { }
        public bool GetEnableHighDpiTiling() => true;
        public void SetEnableHighDpiTiling(bool enabled) { }
        public string GetSelectedGeminiModel() => "gemini-2.5-flash";
        public void SetSelectedGeminiModel(string model) { }
    }

    private class DummyGeminiApiService : IGeminiApiService
    {
        public Task<ExtractedDrawingData> AnalyzeDrawingAsync(string pdfBase64, VerificationSkill skill, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExtractedDrawingData());
    }

    private class DummyReportGenerator : IReportGenerator
    {
        public void GeneratePdfReport(IEnumerable<DrawingVerifier.Application.DTOs.VerificationReport> reports, string outputPath) { }
    }

    [Fact]
    public async Task LoadSkills_LoadsSkillsAndSetsSelectedSkill()
    {
        var repo = new DummySkillRepository();
        repo.Skills.Add(new VerificationSkill { Name = "Default Profile" });

        var useCase = new AnalyzeDrawingUseCase(new DummyGeminiApiService(), new NCalcRuleEvaluator());
        var vm = new MainViewModel(repo, useCase, new DummyReportGenerator(), new DummySettingsService());

        await vm.LoadSkillsCommand.ExecuteAsync(null);

        Assert.Single(vm.Skills);
        Assert.Equal("Default Profile", vm.SelectedSkill?.Name);
    }
}
