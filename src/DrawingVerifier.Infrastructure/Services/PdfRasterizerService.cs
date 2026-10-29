namespace DrawingVerifier.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.IO;
using PDFtoImage;
using SkiaSharp;

/// <summary>
/// Rasterizes PDF documents into high-DPI full views and overlapping spatial quadrant tiles.
/// </summary>
public class PdfRasterizerService : IPdfRasterizerService
{
    public IReadOnlyList<byte[]> RasterizeAndTile(byte[] pdfBytes, int dpi = 300, double overlapPercentage = 0.15)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (pdfBytes.Length == 0)
        {
            return [];
        }

        using var pdfStream = new MemoryStream(pdfBytes);
        using var fullBitmap = Conversion.ToImage(pdfStream, page: 0, options: new RenderOptions(Dpi: dpi));

        if (fullBitmap == null)
        {
            return [];
        }

        var results = new List<byte[]>();

        // 1. Overview image
        results.Add(EncodeBitmapToJpeg(fullBitmap, quality: 80));

        // 2. Quadrants with overlap
        int width = fullBitmap.Width;
        int height = fullBitmap.Height;

        int halfWidth = width / 2;
        int halfHeight = height / 2;
        int overlapX = (int)(halfWidth * overlapPercentage);
        int overlapY = (int)(halfHeight * overlapPercentage);

        // Top-Left
        results.Add(CropAndEncode(fullBitmap, 0, 0, Math.Min(width, halfWidth + overlapX), Math.Min(height, halfHeight + overlapY)));

        // Top-Right
        int trX = Math.Max(0, halfWidth - overlapX);
        results.Add(CropAndEncode(fullBitmap, trX, 0, width - trX, Math.Min(height, halfHeight + overlapY)));

        // Bottom-Left
        int blY = Math.Max(0, halfHeight - overlapY);
        results.Add(CropAndEncode(fullBitmap, 0, blY, Math.Min(width, halfWidth + overlapX), height - blY));

        // Bottom-Right
        int brX = Math.Max(0, halfWidth - overlapX);
        int brY = Math.Max(0, halfHeight - overlapY);
        results.Add(CropAndEncode(fullBitmap, brX, brY, width - brX, height - brY));

        return results;
    }

    private static byte[] CropAndEncode(SKBitmap source, int x, int y, int width, int height)
    {
        var srcRect = new SKRect(x, y, x + width, y + height);
        using var cropped = new SKBitmap(width, height);
        using var canvas = new SKCanvas(cropped);
        var destRect = new SKRect(0, 0, width, height);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, srcRect, destRect, new SKSamplingOptions(SKFilterMode.Linear));
        canvas.Flush();

        return EncodeBitmapToJpeg(cropped, quality: 85);
    }

    private static byte[] EncodeBitmapToJpeg(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}
