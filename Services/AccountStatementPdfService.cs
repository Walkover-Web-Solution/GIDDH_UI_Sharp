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

        public AccountStatementPdfService(PdfService pdfService)
        {
            _razorTemplateService = new RazorTemplateService();
            _pdfService = pdfService;
            _templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
        }

        public async Task<byte[]> GenerateAccountStatementPdfAsync(Root request)
        {
            var browser = await _pdfService.GetBrowserAsync();
            var page = await browser.NewPageAsync();

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

            try
            {
                string templatePath = Path.Combine(_templateBasePath, "AccountStatement");
                
                // Render the account statement body
                string body = await _razorTemplateService.RenderTemplateAsync(
                    Path.Combine(templatePath, "body.cshtml"), request);

                // Load only the required CSS files
                string commonStyles = LoadFileContent(Path.Combine(templatePath, "Styles", "Styles.css"));
                string bodyStyles = LoadFileContent(Path.Combine(templatePath, "Styles", "Body.css"));

                // Create the complete PDF document
                string htmlContent = CreateAccountStatementDocument(
                    body,
                    commonStyles,
                    bodyStyles,
                    request);
                Console.WriteLine(htmlContent);
                await page.SetContentAsync(htmlContent);
                await page.EmulateMediaTypeAsync(MediaType.Print);

                var pdfBytes = await page.PdfDataAsync(pdfOptions);

                // Save PDF to Downloads folder - only in local/development environment
                string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                                     Environment.GetEnvironmentVariable("ENVIRONMENT");
                
                if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(environment, "Local", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(environment))
                {
                    string fileName = !string.IsNullOrWhiteSpace(request?.AccountName) 
                        ? $"AccountStatement_{request.AccountName}_{DateTime.Now:yyyyMMddHHmmss}"
                        : $"AccountStatement_{DateTime.Now:yyyyMMddHHmmss}";

                    string downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
                    
                    if (!Directory.Exists(downloadsPath))
                    {
                        Directory.CreateDirectory(downloadsPath);
                    }

                    string sanitizedFileName = SanitizeFileName(fileName);
                    string fullPath = Path.Combine(downloadsPath, $"{sanitizedFileName}.pdf");
                    
                    int counter = 1;
                    while (File.Exists(fullPath))
                    {
                        fullPath = Path.Combine(downloadsPath, $"{sanitizedFileName}_{counter}.pdf");
                        counter++;
                    }

                    await File.WriteAllBytesAsync(fullPath, pdfBytes);
                    Console.WriteLine($"PDF saved to: {fullPath}");
                }

                return pdfBytes;
            }
            finally
            {
                await page.CloseAsync();
                await page.DisposeAsync();
            }
        }

        private string LoadFileContent(string filePath) =>
            File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

        private string CreateAccountStatementDocument(
            string body,
            string commonStyles,
            string bodyStyles,
            Root request)
        {
            var themeCSS = new StringBuilder();

            // Load font CSS using PdfService method with proper validation
            try
            {
                string fontCSS = _pdfService.LoadFontCSS("Inter");
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

            var allStyles = $"{themeCSS} {commonStyles} {bodyStyles}";

            return $@"
            <!DOCTYPE html>
            <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        {allStyles}
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
