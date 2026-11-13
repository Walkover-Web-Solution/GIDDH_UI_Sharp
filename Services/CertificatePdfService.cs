using PuppeteerSharp;
using PuppeteerSharp.Media;
using GiddhTemplate.Models;
using System.Text;

namespace GiddhTemplate.Services
{
    public class CertificatePdfService
    {
        private readonly RazorTemplateService _razorTemplateService;
        private readonly PdfService _pdfService;
        private readonly string _templatePath;
        private static readonly SemaphoreSlim _pdfGenerationSemaphore = new(1, 1);

        public CertificatePdfService(PdfService pdfService, RazorTemplateService razorTemplateService)
        {
            _razorTemplateService = razorTemplateService;
            _pdfService = pdfService;
            _templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Certificate");
        }

        public async Task<string> GenerateCertificatePdfToFileAsync(CertificateRequest request)
        {
            await _pdfGenerationSemaphore.WaitAsync();
            Console.WriteLine($"[CertificatePdfService] PDF generation started. Active requests: {1 - _pdfGenerationSemaphore.CurrentCount}/1");

            IPage? page = null;
            try
            {
                var browser = await _pdfService.GetBrowserAsync();

                Console.WriteLine("[CertificatePdfService] Creating new page...");
                var pageTask = browser.NewPageAsync();
                page = await pageTask.WaitAsync(TimeSpan.FromSeconds(30));
                Console.WriteLine("[CertificatePdfService] Page created successfully.");

                // Puppeteer's PDF margin always wins over this template's own
                // "@page { margin }" CSS, so it is set to zero here instead — the
                // design's own border/padding already frames the page.
                var pdfOptions = new PdfOptions
                {
                    Format = PaperFormat.A4,
                    Landscape = false,
                    PrintBackground = true,
                    PreferCSSPageSize = true,
                    DisplayHeaderFooter = false,
                    MarginOptions = new MarginOptions { Top = "0px", Bottom = "0px", Left = "0px", Right = "0px" }
                };

                var tasks = new[]
                {
                    _razorTemplateService.RenderTemplateAsync(Path.Combine(_templatePath, "Header.cshtml"), request),
                    _razorTemplateService.RenderTemplateAsync(Path.Combine(_templatePath, "Body.cshtml"), request),
                    _razorTemplateService.RenderTemplateAsync(Path.Combine(_templatePath, "Footer.cshtml"), request)
                };
                await Task.WhenAll(tasks);
                string header = tasks[0].Result;
                string body = tasks[1].Result;
                string footer = tasks[2].Result;

                var stylesTasks = new[]
                {
                    LoadFileContentAsync(Path.Combine(_templatePath, "Styles", "Styles.css")),
                    LoadFileContentAsync(Path.Combine(_templatePath, "Styles", "Header.css")),
                    LoadFileContentAsync(Path.Combine(_templatePath, "Styles", "Body.css")),
                    LoadFileContentAsync(Path.Combine(_templatePath, "Styles", "Footer.css"))
                };
                await Task.WhenAll(stylesTasks);
                string commonStyles = stylesTasks[0].Result;
                string headerStyles = stylesTasks[1].Result;
                string bodyStyles = stylesTasks[2].Result;
                string footerStyles = stylesTasks[3].Result;

                string htmlContent = CreateCertificateDocument(header, body, footer, commonStyles, headerStyles, bodyStyles, footerStyles);

                Console.WriteLine($"[CertificatePdfService] Setting page content (HTML size: {htmlContent.Length} bytes)...");
                await page.SetContentAsync(htmlContent, new NavigationOptions
                {
                    Timeout = 15000,
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
                });

                Console.WriteLine("[CertificatePdfService] Emulating print media type...");
                await page.EmulateMediaTypeAsync(MediaType.Print);

                string tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                Directory.CreateDirectory(tempPath);

                string fileName = !string.IsNullOrWhiteSpace(request?.templateType)
                    ? SanitizeFileName(request.templateType)
                    : !string.IsNullOrWhiteSpace(request?.name)
                        ? SanitizeFileName($"Certificate_{request.name}")
                        : $"Certificate_{DateTime.Now:yyyyMMddHHmmss}";

                string tempFilePath = Path.Combine(tempPath, $"{fileName}_{Guid.NewGuid():N}.pdf");

                Console.WriteLine($"[CertificatePdfService] Generating PDF to: {tempFilePath}");
                await page.PdfAsync(tempFilePath, pdfOptions);
                Console.WriteLine($"[CertificatePdfService] PDF generated successfully. File size: {new FileInfo(tempFilePath).Length} bytes");

                string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                                     Environment.GetEnvironmentVariable("ENVIRONMENT");

                if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(environment, "Local", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(environment))
                {
                    string downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
                    Directory.CreateDirectory(downloadsPath);

                    string downloadFilePath = Path.Combine(downloadsPath, $"{fileName}.pdf");
                    int counter = 1;
                    while (File.Exists(downloadFilePath))
                    {
                        downloadFilePath = Path.Combine(downloadsPath, $"{fileName}_{counter}.pdf");
                        counter++;
                    }

                    File.Copy(tempFilePath, downloadFilePath, overwrite: false);
                    Console.WriteLine($"[CertificatePdfService] PDF saved to: {downloadFilePath}");
                }

                return tempFilePath;
            }
            catch (PuppeteerSharp.TargetClosedException ex)
            {
                Console.WriteLine($"[CertificatePdfService] CRITICAL: Chrome process crashed (TargetClosedException): {ex.Message}");
                Console.WriteLine("[CertificatePdfService] This usually means Chrome ran out of memory or was killed by OOM killer.");
                throw new Exception("Certificate PDF generation failed: Chrome process crashed. This may be due to insufficient memory or too many concurrent requests.", ex);
            }
            catch (PuppeteerSharp.NavigationException ex)
            {
                Console.WriteLine($"[CertificatePdfService] Navigation error during PDF generation: {ex.Message}");
                throw new Exception("Certificate PDF generation failed: Unable to load content into browser.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CertificatePdfService] Unexpected error during PDF generation: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[CertificatePdfService] Stack trace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                if (page != null)
                {
                    try
                    {
                        await page.CloseAsync();
                        await page.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CertificatePdfService] Error closing page: {ex.Message}");
                    }
                }

                _pdfGenerationSemaphore.Release();
                Console.WriteLine($"[CertificatePdfService] PDF generation completed. Active requests: {1 - _pdfGenerationSemaphore.CurrentCount}/1");
            }
        }

        private async Task<string> LoadFileContentAsync(string filePath) =>
            File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : string.Empty;

        private static string CreateCertificateDocument(
            string header, string body, string footer,
            string commonStyles, string headerStyles, string bodyStyles, string footerStyles)
        {
            var allStylesBuilder = new StringBuilder();
            allStylesBuilder.Append(commonStyles)
                           .Append(headerStyles)
                           .Append(bodyStyles)
                           .Append(footerStyles);

            return $@"
            <!DOCTYPE html>
            <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <title>GIDDH Certificate of Completion</title>
                    <style>
                        {allStylesBuilder}
                    </style>
                </head>
                <body>
                    <main class='certificate' aria-label='GIDDH Certificate of Completion'>
                        {header}
                        {body}
                        {footer}
                    </main>
                </body>
            </html>";
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Certificate" : sanitized;
        }
    }
}
