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

        public AccountStatementPdfService(PdfService pdfService, RazorTemplateService razorTemplateService)
        {
            _razorTemplateService = razorTemplateService;
            _pdfService = pdfService;
            _templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
        }

        public async Task<string> GenerateAccountStatementPdfToFileAsync(Root request)
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
                Console.WriteLine(htmlContent);
                await page.SetContentAsync(htmlContent);
                await page.EmulateMediaTypeAsync(MediaType.Print);

                // Generate temporary file path
                string tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                Directory.CreateDirectory(tempPath);
                
                string fileName = !string.IsNullOrWhiteSpace(request?.AccountName) 
                    ? SanitizeFileName($"AccountStatement_{request.AccountName}_{DateTime.Now:yyyyMMddHHmmss}")
                    : $"AccountStatement_{DateTime.Now:yyyyMMddHHmmss}";
                
                string tempFilePath = Path.Combine(tempPath, $"{fileName}_{Guid.NewGuid():N}.pdf");

                // Write PDF directly to disk using streaming to minimize memory usage
                await page.PdfAsync(tempFilePath, pdfOptions);

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
            finally
            {
                await page.CloseAsync();
                await page.DisposeAsync();
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
