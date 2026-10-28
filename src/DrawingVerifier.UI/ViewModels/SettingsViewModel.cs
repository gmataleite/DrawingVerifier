namespace DrawingVerifier.UI.ViewModels;

using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingVerifier.Application.Interfaces;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    public Action? CloseAction { get; set; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _apiKey = _settingsService.GetApiKey() ?? string.Empty;
    }

    [RelayCommand]
    private void Save(Window? window = null)
    {
        _settingsService.SaveApiKey(ApiKey);

        CloseAction?.Invoke();
        window?.Close();
    }
}
