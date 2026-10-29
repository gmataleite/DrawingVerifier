namespace DrawingVerifier.Domain.Entities;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a verification profile (e.g., a client profile such as BHP, Vale, etc.) containing specialized
/// instructions for the Gemini AI, aggregating the system prompt, the structured output schema and the validation rules.
/// </summary>
public class VerificationSkill : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private string _name = string.Empty;
    private string _systemPrompt = string.Empty;
    private string _outputSchemaJson = string.Empty;
    private ObservableCollection<ValidationRule> _rules = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    /// <summary>
    /// Skill / client profile name (e.g., "BHP - Mechanical Standard").
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// Specialized instructions sent to the Gemini AI to guide the PDF data extraction.
    /// </summary>
    public string SystemPrompt
    {
        get => _systemPrompt;
        set => SetField(ref _systemPrompt, value);
    }

    /// <summary>
    /// JSON Schema defining the strict contract expected as the AI response.
    /// </summary>
    public string OutputSchemaJson
    {
        get => _outputSchemaJson;
        set => SetField(ref _outputSchemaJson, value);
    }

    /// <summary>
    /// Validation rules associated with this Skill.
    /// </summary>
    public ObservableCollection<ValidationRule> Rules
    {
        get => _rules;
        set => SetField(ref _rules, value);
    }

    public VerificationSkill()
    {
    }

    public VerificationSkill(
        Guid id,
        string name,
        string systemPrompt,
        string outputSchemaJson,
        ObservableCollection<ValidationRule>? rules = null)
    {
        _id = id;
        _name = name;
        _systemPrompt = systemPrompt;
        _outputSchemaJson = outputSchemaJson;
        _rules = rules ?? [];
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
