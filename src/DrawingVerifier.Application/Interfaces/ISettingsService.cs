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

    /// <summary>
    /// Retrieves whether High-DPI Quadrant Tiling (300 DPI) is enabled. Defaults to true.
    /// </summary>
    bool GetEnableHighDpiTiling();

    /// <summary>
    /// Sets whether High-DPI Quadrant Tiling is enabled.
    /// </summary>
    void SetEnableHighDpiTiling(bool enabled);

    /// <summary>
    /// Retrieves the selected Gemini model identifier (e.g. gemini-2.5-flash). Defaults to "gemini-2.5-flash".
    /// </summary>
    string GetSelectedGeminiModel();

    /// <summary>
    /// Sets the selected Gemini model identifier.
    /// </summary>
    void SetSelectedGeminiModel(string model);
}
