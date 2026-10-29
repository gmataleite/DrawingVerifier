namespace DrawingVerifier.Infrastructure.Services;

/// <summary>
/// Service for rasterizing PDF documents into high-resolution images and spatial tiles.
/// </summary>
public interface IPdfRasterizerService
{
    /// <summary>
    /// Rasterizes the first page of a PDF into high-DPI overlapping quadrants and an overview image.
    /// </summary>
    /// <param name="pdfBytes">Raw PDF byte array.</param>
    /// <param name="dpi">Rendering resolution in DPI (default 300).</param>
    /// <param name="overlapPercentage">Overlap ratio between adjacent quadrants (default 0.15 = 15%).</param>
    /// <returns>List of image byte arrays (Overview, TopLeft, TopRight, BottomLeft, BottomRight).</returns>
    IReadOnlyList<byte[]> RasterizeAndTile(byte[] pdfBytes, int dpi = 300, double overlapPercentage = 0.15);
}
