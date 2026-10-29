namespace DrawingVerifier.Tests;

using System.Collections.ObjectModel;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.UI.ViewModels;
using Xunit;

public class SkillManagerViewModelTests
{
    private class FakeSkillRepository : ISkillRepository
    {
        public List<VerificationSkill> Items { get; } = [];

        public Task<VerificationSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = Items.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<VerificationSkill>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VerificationSkill>>(Items.ToList());
        }

        public Task AddAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
        {
            Items.Add(skill);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(VerificationSkill skill, CancellationToken cancellationToken = default)
        {
            var index = Items.FindIndex(s => s.Id == skill.Id);
            if (index >= 0)
            {
                Items[index] = skill;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadSkills_PopulatesSkillsAndSelectsFirst()
    {
        var repo = new FakeSkillRepository();
        repo.Items.Add(new VerificationSkill { Name = "Skill 1" });
        repo.Items.Add(new VerificationSkill { Name = "Skill 2" });

        var vm = new SkillManagerViewModel(repo);
        await vm.LoadSkillsCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Skills.Count);
        Assert.NotNull(vm.SelectedSkill);
        Assert.Equal("Skill 1", vm.SelectedSkill.Name);
        Assert.True(vm.HasSelectedSkill);
    }

    [Fact]
    public void AddNewSkill_CreatesBlankSkillAndSetsAsSelected()
    {
        var repo = new FakeSkillRepository();
        var vm = new SkillManagerViewModel(repo);

        vm.AddNewSkillCommand.Execute(null);

        Assert.Contains(vm.SelectedSkill, vm.Skills);
        Assert.NotNull(vm.SelectedSkill);
        Assert.Equal("New Skill", vm.SelectedSkill.Name);
        Assert.True(vm.HasSelectedSkill);
    }

    [Fact]
    public async Task SaveSkill_AddsNewSkill_WhenNotExistingInRepo()
    {
        var repo = new FakeSkillRepository();
        var vm = new SkillManagerViewModel(repo);

        vm.AddNewSkillCommand.Execute(null);
        vm.SelectedSkill!.Name = "Custom Profile";
        vm.SelectedSkill.SystemPrompt = "Custom instructions";

        await vm.SaveSkillCommand.ExecuteAsync(null);

        Assert.Single(repo.Items);
        Assert.Equal("Custom Profile", repo.Items[0].Name);
        Assert.Equal(repo.Items[0].Id, vm.SelectedSkill.Id);
    }

    [Fact]
    public async Task SaveSkill_UpdatesExistingSkill_WhenAlreadyInRepo()
    {
        var repo = new FakeSkillRepository();
        var existing = new VerificationSkill { Name = "Original Name" };
        repo.Items.Add(existing);

        var vm = new SkillManagerViewModel(repo);
        await vm.LoadSkillsCommand.ExecuteAsync(null);

        vm.SelectedSkill!.Name = "Updated Name";
        await vm.SaveSkillCommand.ExecuteAsync(null);

        Assert.Single(repo.Items);
        Assert.Equal("Updated Name", repo.Items[0].Name);
    }

    [Fact]
    public async Task DeleteSkill_RemovesSelectedSkillFromRepository()
    {
        var repo = new FakeSkillRepository();
        var s1 = new VerificationSkill { Name = "Skill 1" };
        var s2 = new VerificationSkill { Name = "Skill 2" };
        repo.Items.Add(s1);
        repo.Items.Add(s2);

        var vm = new SkillManagerViewModel(repo);
        await vm.LoadSkillsCommand.ExecuteAsync(null);
        vm.SelectedSkill = vm.Skills.First(s => s.Id == s1.Id);

        await vm.DeleteSkillCommand.ExecuteAsync(null);

        Assert.Single(repo.Items);
        Assert.Equal("Skill 2", repo.Items[0].Name);
        Assert.Equal("Skill 2", vm.SelectedSkill?.Name);
    }

    [Fact]
    public void AddRule_AddsValidationRuleToSelectedSkill()
    {
        var repo = new FakeSkillRepository();
        var vm = new SkillManagerViewModel(repo);
        vm.AddNewSkillCommand.Execute(null);

        Assert.Empty(vm.SelectedSkill!.Rules);

        vm.AddRuleCommand.Execute(null);

        Assert.Single(vm.SelectedSkill.Rules);
        Assert.Equal("New Rule", vm.SelectedSkill.Rules[0].Name);
    }

    [Fact]
    public void RemoveRule_RemovesSpecifiedRuleFromSelectedSkill()
    {
        var repo = new FakeSkillRepository();
        var vm = new SkillManagerViewModel(repo);
        vm.AddNewSkillCommand.Execute(null);

        vm.AddRuleCommand.Execute(null);
        var rule = vm.SelectedSkill!.Rules[0];

        vm.RemoveRuleCommand.Execute(rule);

        Assert.Empty(vm.SelectedSkill.Rules);
    }
}
