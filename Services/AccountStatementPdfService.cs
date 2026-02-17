using PuppeteerSharp;
using PuppeteerSharp.Media;
using GiddhTemplate.Models;
using System.Text;

namespace GiddhTemplate.Services
{
    public class AccountStatementPdfService
    {
        private readonly RazorTemplateService _razorTemplateService;
        private readonly PdfService _pdfService;
        private readonly string _templateBasePath;
        private static readonly SemaphoreSlim _pdfGenerationSemaphore = new(1, 1);

        public AccountStatementPdfService(PdfService pdfService, RazorTemplateService razorTemplateService)
        {
            _razorTemplateService = razorTemplateService;
            _pdfService = pdfService;
            _templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
        }

        public async Task<string> GenerateAccountStatementPdfToFileAsync(Root request)
        {
            await _pdfGenerationSemaphore.WaitAsync();
            Console.WriteLine($"[AccountStatementPdfService] PDF generation started. Active requests: {1 - _pdfGenerationSemaphore.CurrentCount}/1");
            
            IPage? page = null;
            try
            {
                var browser = await _pdfService.GetBrowserAsync();
                
                Console.WriteLine("[AccountStatementPdfService] Creating new page...");
                var pageTask = browser.NewPageAsync();
                page = await pageTask.WaitAsync(TimeSpan.FromSeconds(30));
                Console.WriteLine("[AccountStatementPdfService] Page created successfully.");

                var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                Landscape = false,
                PrintBackground = true,
                PreferCSSPageSize = true,
                DisplayHeaderFooter = false,
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Bottom = "25px",
                    Left = "20px",
                    Right = "20px"
                }
            };

                string templatePath = Path.Combine(_templateBasePath, "AccountStatement");
                
                // Render the account statement body
                string body = await _razorTemplateService.RenderTemplateAsync(
                    Path.Combine(templatePath, "body.cshtml"), request);

                // Load only the required CSS files
                var stylesTasks = new[]
                {
                    LoadFileContentAsync(Path.Combine(templatePath, "Styles", "Styles.css")),
                    LoadFileContentAsync(Path.Combine(templatePath, "Styles", "Body.css"))
                };
                await Task.WhenAll(stylesTasks);
                string commonStyles = stylesTasks[0].Result;
                string bodyStyles = stylesTasks[1].Result;

                // Create the complete PDF document
                string htmlContent = await CreateAccountStatementDocumentAsync(
                    body,
                    commonStyles,
                    bodyStyles,
                    request);
                
                Console.WriteLine($"[AccountStatementPdfService] Setting page content (HTML size: {htmlContent.Length} bytes)...");
                await page.SetContentAsync(htmlContent, new NavigationOptions
                {
                    Timeout = 120000,
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });
                
                Console.WriteLine("[AccountStatementPdfService] Emulating print media type...");
                await page.EmulateMediaTypeAsync(MediaType.Print);

                // Generate temporary file path
                string tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                Directory.CreateDirectory(tempPath);
                
                string fileName = !string.IsNullOrWhiteSpace(request?.AccountName) 
                    ? SanitizeFileName($"AccountStatement_{request.AccountName}_{DateTime.Now:yyyyMMddHHmmss}")
                    : $"AccountStatement_{DateTime.Now:yyyyMMddHHmmss}";
                
                string tempFilePath = Path.Combine(tempPath, $"{fileName}_{Guid.NewGuid():N}.pdf");

                Console.WriteLine($"[AccountStatementPdfService] Generating PDF to: {tempFilePath}");
                await page.PdfAsync(tempFilePath, pdfOptions);
                Console.WriteLine($"[AccountStatementPdfService] PDF generated successfully. File size: {new FileInfo(tempFilePath).Length} bytes");

                // Save to Downloads folder in dev environment (optional copy)
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
                    Console.WriteLine($"PDF saved to: {downloadFilePath}");
                }

                return tempFilePath;
            }
            catch (PuppeteerSharp.TargetClosedException ex)
            {
                Console.WriteLine($"[AccountStatementPdfService] CRITICAL: Chrome process crashed (TargetClosedException): {ex.Message}");
                Console.WriteLine("[AccountStatementPdfService] This usually means Chrome ran out of memory or was killed by OOM killer.");
                Console.WriteLine("[AccountStatementPdfService] Browser will be recreated on next request.");
                
                throw new Exception("Account Statement PDF generation failed: Chrome process crashed. This may be due to insufficient memory or too many concurrent requests.", ex);
            }
            catch (PuppeteerSharp.NavigationException ex)
            {
                Console.WriteLine($"[AccountStatementPdfService] Navigation error during PDF generation: {ex.Message}");
                throw new Exception("Account Statement PDF generation failed: Unable to load content into browser.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountStatementPdfService] Unexpected error during PDF generation: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[AccountStatementPdfService] Stack trace: {ex.StackTrace}");
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
                        Console.WriteLine($"[AccountStatementPdfService] Error closing page: {ex.Message}");
                    }
                }
                
                _pdfGenerationSemaphore.Release();
                Console.WriteLine($"[AccountStatementPdfService] PDF generation completed. Active requests: {1 - _pdfGenerationSemaphore.CurrentCount}/1");
            }
        }

        private async Task<string> LoadFileContentAsync(string filePath) =>
            File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : string.Empty;

        private async Task<string> CreateAccountStatementDocumentAsync(
            string body,
            string commonStyles,
            string bodyStyles,
            Root request)
        {
            var themeCSS = new StringBuilder();

            // Load font CSS using PdfService method with proper validation
            try
            {
                string fontCSS = await _pdfService.LoadFontCSSAsync("Inter");
                if (!string.IsNullOrEmpty(fontCSS))
                {
                    // Ensure the font CSS is properly formatted and doesn't contain invalid characters
                    fontCSS = fontCSS.Trim();
                    if (fontCSS.StartsWith("@font-face") && fontCSS.Contains("font-family"))
                    {
                        themeCSS.Append(fontCSS);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font loading failed: {ex.Message}");
            }

            var allStylesBuilder = new StringBuilder();
            allStylesBuilder.Append(themeCSS)
                           .Append(" ")
                           .Append(commonStyles)
                           .Append(" ")
                           .Append(bodyStyles);

            return $@"
            <!DOCTYPE html>
            <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        {allStylesBuilder}
                    </style>
                </head>
                <body>
                    {body}
                </body>
            </html>";
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "PDF" : sanitized;
        }
    }
}
