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
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public QRCodeController(IMemoryCache cache, ILogger<QRCodeController> logger, ConfigHandler configHandler)
        : base(configHandler)
    {
        _cache = cache;
        _logger = logger;

        // 🔒 Configurable cache policy
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.High
        };
    }

    [HttpGet("QRGenerate")]
    public IActionResult Generate([FromQuery] string url, [FromQuery] string? logoPath = null)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var validatedUri))
            return BadRequest("Invalid URL format.");

        string safeUrl = validatedUri.ToString().Trim();
        string cacheKey = $"qr-png-{safeUrl}-{logoPath}";

        if (!_cache.TryGetValue(cacheKey, out byte[] qrBytes))
        {
            qrBytes = GenerateQrCode(safeUrl, logoPath, 300);
            _cache.Set(cacheKey, qrBytes, _cacheOptions);
            _logger.LogInformation("Generated new QR code for {Url}", safeUrl);
        }
        else
        {
            _logger.LogInformation("Served cached QR code for {Url}", safeUrl);
        }

        return File(qrBytes, "image/png");
    }

    [HttpGet("generate-pdf")]
    public IActionResult GeneratePdf([FromQuery] string url, [FromQuery] string? logoPath = null)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var validatedUri))
            return BadRequest("Invalid URL format.");

        string safeUrl = validatedUri.ToString().Trim();
        string cacheKey = $"qr-pdf-{safeUrl}-{logoPath}";

        if (!_cache.TryGetValue(cacheKey, out byte[] pdfBytes))
        {
            var qrBytes = GenerateQrCode(safeUrl, logoPath, 300);

            using var ms = new MemoryStream();
            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                using var qrStream = new MemoryStream(qrBytes);
                using var qrImage = XImage.FromStream(() => qrStream);
                gfx.DrawImage(qrImage, 100, 100, 200, 200);

                var font = new XFont("Arial", 12, XFontStyle.Bold);
                gfx.DrawString(safeUrl, font, XBrushes.Black,
                    new XRect(0, 320, page.Width, 50),
                    XStringFormats.Center);

                doc.Save(ms);
            }
            pdfBytes = ms.ToArray();

            _cache.Set(cacheKey, pdfBytes, _cacheOptions);
            _logger.LogInformation("Generated new PDF QR for {Url}", safeUrl);
        }
        else
        {
            _logger.LogInformation("Served cached PDF QR for {Url}", safeUrl);
        }

        return File(pdfBytes, "application/pdf", "QRCode.pdf");
    }

    /// <summary>
    /// Core QR generator with optional logo embedding.
    /// </summary>
    private static byte[] GenerateQrCode(string url, string? logoPath, int size = 300)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H); // ✅ stronger ECC
        var qrCode = new PngByteQRCode(qrCodeData);

        // Generate with custom RED foreground
        var qrBytes = qrCode.GetGraphic(
            pixelsPerModule: 10,
            darkColorRgba: new byte[] { 200, 0, 0, 255 },   // deep red
            lightColorRgba: new byte[] { 255, 255, 255, 255 }
        );

        using var qrBitmap = SKBitmap.Decode(qrBytes);
        using var resized = qrBitmap.Resize(new SKImageInfo(size, size), SKFilterQuality.High);
        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var canvas = surface.Canvas;

        // 🔥 Gradient background for style
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(size, size),
                new[] { SKColors.White, SKColors.LightGray },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(new SKRect(0, 0, size, size), paint);

        // Draw QR on top
        canvas.DrawBitmap(resized, 0, 0);

        // Optional: embed logo in center
        if (!string.IsNullOrWhiteSpace(logoPath) && System.IO.File.Exists(logoPath))
        {
            using var logoBitmap = SKBitmap.Decode(logoPath);
            int logoSize = size / 4;
            int x = (size - logoSize) / 2;
            int y = (size - logoSize) / 2;
            var destRect = new SKRect(x, y, x + logoSize, y + logoSize);
            canvas.DrawBitmap(logoBitmap, destRect);
        }

        canvas.Flush();
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}