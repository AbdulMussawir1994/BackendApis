using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;
using SkiaSharp;

namespace BackendApis.Controllers;
[ApiController]
[AllowAnonymous]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class QRCodeController : WebBaseController
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QRCodeController> _logger;

    public QRCodeController(IMemoryCache cache, ILogger<QRCodeController> logger, ConfigHandler configHandler) : base(configHandler)
    {
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("QRGenerate")]
    public async Task<IActionResult> Generate([FromQuery] string url, [FromQuery] string? logoPath = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("URL parameter is required.");

        // string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "my_gibhli.png");
        string imagePath = string.Empty;

        string cacheKey = $"qr-png-{url}-{imagePath}";
        if (!_cache.TryGetValue(cacheKey, out byte[] qrBytes))
        {
            qrBytes = await Task.Run(() => GenerateQrCode(url, imagePath));
            _cache.Set(cacheKey, qrBytes, TimeSpan.FromHours(1)); // cache for 1h
            _logger.LogInformation("Generated new QR code for {Url}", url);
        }
        else
        {
            _logger.LogInformation("Served cached QR code for {Url}", url);
        }

        return File(qrBytes, "image/png");
    }

    [HttpGet("generate-pdf")]
    public async Task<IActionResult> GeneratePdf([FromQuery] string url, [FromQuery] string? logoPath = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("URL parameter is required.");

        string cacheKey = $"qr-pdf-{url}-{logoPath}";
        if (!_cache.TryGetValue(cacheKey, out byte[] pdfBytes))
        {
            // Generate QR code
            var qrBytes = await Task.Run(() => GenerateQrCode(url, logoPath));

            // Generate PDF
            pdfBytes = await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                using (var doc = new PdfDocument())
                {
                    var page = doc.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);

                    using var qrStream = new MemoryStream(qrBytes);
                    using var qrImage = XImage.FromStream(() => qrStream);
                    gfx.DrawImage(qrImage, 100, 100, 200, 200);

                    var font = new XFont("Arial", 12, XFontStyle.Bold);
                    gfx.DrawString(url, font, XBrushes.Black,
                        new XRect(0, 320, page.Width, 50),
                        XStringFormats.Center);

                    doc.Save(ms);
                }
                return ms.ToArray();
            });

            _cache.Set(cacheKey, pdfBytes, TimeSpan.FromHours(1));
            _logger.LogInformation("Generated new PDF QR for {Url}", url);
        }
        else
        {
            _logger.LogInformation("Served cached PDF QR for {Url}", url);
        }

        return File(pdfBytes, "application/pdf", "QRCode.pdf");
    }

    /// <summary>
    /// Core QR generator with optional logo embedding.
    /// </summary>
    private static byte[] GenerateQrCode(string url, string? logoPath)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        // 🔴 Change 1: Generate with RED foreground instead of black
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrBytes = qrCode.GetGraphic(
            pixelsPerModule: 10, // smaller modules → smaller raw image
            darkColorRgba: new byte[] { 222, 31, 82, 255 }, // dark red
            lightColorRgba: new byte[] { 255, 255, 255, 255 } // white
        );

        using var qrBitmap = SKBitmap.Decode(qrBytes);

        // 🔴 Change 2: Force default size (e.g., 300x300 px)
        int targetSize = 300;
        using var resized = qrBitmap.Resize(new SKImageInfo(targetSize, targetSize), SKFilterQuality.High);

        using var surface = SKSurface.Create(new SKImageInfo(targetSize, targetSize));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(resized, 0, 0);

        // Optional: embed logo
        if (!string.IsNullOrWhiteSpace(logoPath) && System.IO.File.Exists(logoPath))
        {
            using var logoBitmap = SKBitmap.Decode(logoPath);
            int logoSize = targetSize / 4; // 25% of QR
            int x = (targetSize - logoSize) / 2;
            int y = (targetSize - logoSize) / 2;
            var destRect = new SKRect(x, y, x + logoSize, y + logoSize);
            canvas.DrawBitmap(logoBitmap, destRect);
        }

        canvas.Flush();
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }
}