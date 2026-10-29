namespace DrawingVerifier.Domain.Entities;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a mathematical/logical validation rule dynamically evaluated via NCalc.
/// </summary>
public class ValidationRule : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private string _name = string.Empty;
    private string _expression = string.Empty;
    private string _errorMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    /// <summary>
    /// Descriptive rule name (e.g., "Minimum Overmetal Check").
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// Mathematical or boolean expression evaluated by NCalc (e.g., "BaseDimension <= BomDimension").
    /// </summary>
    public string Expression
    {
        get => _expression;
        set => SetField(ref _expression, value);
    }

    /// <summary>
    /// User-friendly error message shown when the validation returns false.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public ValidationRule()
    {
    }

    public ValidationRule(Guid id, string name, string expression, string errorMessage)
    {
        _id = id;
        _name = name;
        _expression = expression;
        _errorMessage = errorMessage;
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
