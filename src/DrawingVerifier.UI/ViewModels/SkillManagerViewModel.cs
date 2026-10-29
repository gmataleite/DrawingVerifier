namespace DrawingVerifier.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;

public partial class SkillManagerViewModel : ObservableObject
{
    private readonly ISkillRepository _skillRepository;

    [ObservableProperty]
    private ObservableCollection<VerificationSkill> _skills = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSkill))]
    private VerificationSkill? _selectedSkill;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedSkill => SelectedSkill is not null;

    public SkillManagerViewModel(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _ = LoadSkillsAsync();
    }

    [RelayCommand]
    public async Task LoadSkillsAsync()
    {
        try
        {
            var loadedSkills = await _skillRepository.GetAllAsync();

            Skills.Clear();
            foreach (var skill in loadedSkills)
            {
                Skills.Add(skill);
            }

            if (SelectedSkill is null || !Skills.Any(s => s.Id == SelectedSkill.Id))
            {
                SelectedSkill = Skills.FirstOrDefault();
            }

            StatusMessage = $"Loaded {Skills.Count} skill(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading skills: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddNewSkill()
    {
        var newSkill = new VerificationSkill
        {
            Name = "New Skill",
            SystemPrompt = string.Empty,
            OutputSchemaJson = string.Empty,
            Rules = []
        };

        Skills.Add(newSkill);
        SelectedSkill = newSkill;
        StatusMessage = "New skill added. Configure details and click 'Save Changes'.";
    }

    [RelayCommand]
    private async Task SaveSkillAsync()
    {
        if (SelectedSkill is null)
        {
            return;
        }

        try
        {
            var existing = await _skillRepository.GetByIdAsync(SelectedSkill.Id);
            if (existing is null)
            {
                await _skillRepository.AddAsync(SelectedSkill);
            }
            else
            {
                await _skillRepository.UpdateAsync(SelectedSkill);
            }

            var savedId = SelectedSkill.Id;
            await LoadSkillsAsync();
            SelectedSkill = Skills.FirstOrDefault(s => s.Id == savedId);
            StatusMessage = "Skill saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving skill: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteSkillAsync()
    {
        if (SelectedSkill is null)
        {
            return;
        }

        try
        {
            var skillToDelete = SelectedSkill;
            await _skillRepository.DeleteAsync(skillToDelete.Id);
            await LoadSkillsAsync();
            SelectedSkill = Skills.FirstOrDefault();
            StatusMessage = $"Deleted skill '{skillToDelete.Name}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting skill: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        if (SelectedSkill is null)
        {
            return;
        }

        SelectedSkill.Rules ??= [];
        var newRule = new ValidationRule
        {
            Name = "New Rule",
            Expression = string.Empty,
            ErrorMessage = string.Empty
        };

        SelectedSkill.Rules.Add(newRule);
        StatusMessage = "Rule added.";
    }

    [RelayCommand]
    private void RemoveRule(ValidationRule? rule)
    {
        if (SelectedSkill is null || rule is null || SelectedSkill.Rules is null)
        {
            return;
        }

        SelectedSkill.Rules.Remove(rule);
        StatusMessage = $"Rule '{rule.Name}' removed.";
    }
}
