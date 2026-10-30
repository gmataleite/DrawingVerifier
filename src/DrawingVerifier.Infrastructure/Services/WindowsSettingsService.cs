namespace DrawingVerifier.Infrastructure.Services;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DrawingVerifier.Application.Interfaces;

/// <summary>
/// Persists and retrieves user settings encrypted with Windows DPAPI under LocalApplicationData.
/// </summary>
public class WindowsSettingsService : ISettingsService
{
    private const string AppFolderName = "DrawingVerifier";
    private const string SettingsFileName = "settings.dat";
    private const string PreferencesFileName = "appsettings.json";

    public const string DefaultGeminiModel = "gemini-3.8-flash";

    private readonly string _settingsFilePath;
    private readonly string _preferencesFilePath;

    public WindowsSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName,
            SettingsFileName))
    {
    }

    public WindowsSettingsService(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        _settingsFilePath = settingsFilePath;
        var dir = Path.GetDirectoryName(settingsFilePath) ?? string.Empty;
        _preferencesFilePath = Path.Combine(dir, PreferencesFileName);
    }

    public string? GetApiKey()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return null;
            }

            var encryptedBytes = File.ReadAllBytes(_settingsFilePath);
            if (encryptedBytes.Length == 0)
            {
                return null;
            }

            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            var apiKey = Encoding.UTF8.GetString(decryptedBytes).Trim();
            return string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return null;
        }
    }

    public void SaveApiKey(string apiKey)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows DPAPI is only supported on Windows.");
        }

        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (File.Exists(_settingsFilePath))
                {
                    File.SetAttributes(_settingsFilePath, FileAttributes.Normal);
                    File.Delete(_settingsFilePath);
                }

                return;
            }

            var plainBytes = Encoding.UTF8.GetBytes(apiKey.Trim());
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            if (File.Exists(_settingsFilePath))
            {
                File.SetAttributes(_settingsFilePath, FileAttributes.Normal);
            }

            File.WriteAllBytes(_settingsFilePath, encryptedBytes);
            File.SetAttributes(_settingsFilePath, FileAttributes.Hidden);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            throw new InvalidOperationException($"Failed to save the encrypted API key: {ex.Message}", ex);
        }
    }

    public bool GetEnableHighDpiTiling()
    {
        var prefs = LoadPreferences();
        return prefs.EnableHighDpiTiling;
    }

    public void SetEnableHighDpiTiling(bool enabled)
    {
        var prefs = LoadPreferences();
        prefs.EnableHighDpiTiling = enabled;
        SavePreferences(prefs);
    }

    public string GetSelectedGeminiModel()
    {
        var prefs = LoadPreferences();
        return string.IsNullOrWhiteSpace(prefs.SelectedGeminiModel) ? DefaultGeminiModel : prefs.SelectedGeminiModel;
    }

    public void SetSelectedGeminiModel(string model)
    {
        var prefs = LoadPreferences();
        prefs.SelectedGeminiModel = string.IsNullOrWhiteSpace(model) ? DefaultGeminiModel : model.Trim();
        SavePreferences(prefs);
    }

    private AppPreferences LoadPreferences()
    {
        try
        {
            if (File.Exists(_preferencesFilePath))
            {
                var json = File.ReadAllText(_preferencesFilePath);
                var prefs = JsonSerializer.Deserialize<AppPreferences>(json);
                if (prefs != null)
                {
                    return prefs;
                }
            }
        }
        catch
        {
            // Fall back to defaults
        }

        return new AppPreferences();
    }

    private void SavePreferences(AppPreferences prefs)
    {
        try
        {
            var directory = Path.GetDirectoryName(_preferencesFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_preferencesFilePath, json);
        }
        catch
        {
            // Suppress IO failures on writing non-critical preferences
        }
    }

    private class AppPreferences
    {
        public bool EnableHighDpiTiling { get; set; } = true;
        public string SelectedGeminiModel { get; set; } = DefaultGeminiModel;
    }
}
