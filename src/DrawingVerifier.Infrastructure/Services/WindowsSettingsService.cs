namespace DrawingVerifier.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using DrawingVerifier.Application.Interfaces;

/// <summary>
/// Persists and retrieves user settings encrypted with Windows DPAPI under LocalApplicationData.
/// </summary>
public class WindowsSettingsService : ISettingsService
{
    private const string AppFolderName = "DrawingVerifier";
    private const string SettingsFileName = "settings.dat";

    private readonly string _settingsFilePath;

    public WindowsSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName,
            SettingsFileName))
    {
    }

    internal WindowsSettingsService(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        _settingsFilePath = settingsFilePath;
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
}
