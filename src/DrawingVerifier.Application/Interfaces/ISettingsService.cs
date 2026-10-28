namespace DrawingVerifier.Application.Interfaces;

/// <summary>
/// Contract for persisting and retrieving application settings securely.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Retrieves the decrypted Gemini API key, or null if not configured.
    /// </summary>
    string? GetApiKey();

    /// <summary>
    /// Encrypts and persists the Gemini API key on the local machine.
    /// </summary>
    /// <param name="apiKey">Plain-text Gemini API key to store.</param>
    void SaveApiKey(string apiKey);
}
