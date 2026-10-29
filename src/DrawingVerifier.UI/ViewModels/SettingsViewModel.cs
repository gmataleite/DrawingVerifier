namespace DrawingVerifier.UI.ViewModels;

using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingVerifier.Application.Interfaces;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private bool _enableHighDpiTiling = true;

    [ObservableProperty]
    private string _selectedGeminiModel = "gemini-2.5-flash";

    public IReadOnlyList<string> AvailableGeminiModels { get; } =
    [
        "gemini-2.5-flash",
        "gemini-2.5-pro",
        "gemini-3.1-flash-lite"
    ];

    public Action? CloseAction { get; set; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _apiKey = _settingsService.GetApiKey() ?? string.Empty;
        _enableHighDpiTiling = _settingsService.GetEnableHighDpiTiling();
        _selectedGeminiModel = _settingsService.GetSelectedGeminiModel();
    }

    [RelayCommand]
    private void Save(Window? window = null)
    {
        _settingsService.SaveApiKey(ApiKey);
        _settingsService.SetEnableHighDpiTiling(EnableHighDpiTiling);
        _settingsService.SetSelectedGeminiModel(SelectedGeminiModel);

        CloseAction?.Invoke();
        window?.Close();
    }
}
