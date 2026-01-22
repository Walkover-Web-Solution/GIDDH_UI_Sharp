using PuppeteerSharp;
using System.Text;
using InvoiceData;
using PuppeteerSharp.Media;
using GiddhTemplate.Models.Enums;
using System.Text.Json;

namespace GiddhTemplate.Services
{
    public class PdfService
    {
        private readonly RazorTemplateService _razorTemplateService;

        private string _openSansFontCSS = string.Empty;
        private string _robotoFontCSS = string.Empty;
        private string _latoFontCSS = string.Empty;
        private string _interFontCSS = string.Empty;

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static IBrowser? _browser;

        private readonly int decreaseFontSize = 2;

        public PdfService(RazorTemplateService razorTemplateService)
        {
            _razorTemplateService = razorTemplateService;
        }

        public async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null || !_browser.IsConnected)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_browser == null || !_browser.IsConnected)
                    {
                        var launchOptions = new LaunchOptions
                        {
                            Headless = true,
                            // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
                          ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
                         // ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
                            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
                        };

                        _browser = await Puppeteer.LaunchAsync(launchOptions);
                    }
                }
                catch (PuppeteerSharp.ProcessException)
                {
                    _browser = null;
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _browser!;
        }

        public static async Task DisposeBrowserAsync()
        {
            if (_browser != null)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_browser != null)
                    {
                        await _browser.CloseAsync();
                        await _browser.DisposeAsync();
                        _browser = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing browser: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private async Task<string> LoadFileContentAsync(string filePath) =>
            File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : string.Empty;

        public async Task<(string Common, string Header, string Footer, string Body, string Background)>
            LoadStylesAsync(string basePath)
        {
            var tasks = new[]
            {
                LoadFileContentAsync(Path.Combine(basePath, "Styles", "Styles.css")),
                LoadFileContentAsync(Path.Combine(basePath, "Styles", "Header.css")),
                LoadFileContentAsync(Path.Combine(basePath, "Styles", "Footer.css")),
                LoadFileContentAsync(Path.Combine(basePath, "Styles", "Body.css")),
                LoadFileContentAsync(Path.Combine(basePath, "Styles", "Background.css"))
            };

            await Task.WhenAll(tasks);

            return (
                Common: tasks[0].Result,
                Header: tasks[1].Result,
                Footer: tasks[2].Result,
                Body: tasks[3].Result,
                Background: tasks[4].Result
            );
        }

        public async Task<string> LoadFontCSSAsync(string fontFamily)
        {
            if (fontFamily == "Open Sans" && string.IsNullOrEmpty(_openSansFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "OpenSans");
                _openSansFontCSS = await BuildFontCSSAsync("Open Sans", fontPath);
            }
            else if (fontFamily == "Roboto" && string.IsNullOrEmpty(_robotoFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Roboto");
                _robotoFontCSS = await BuildFontCSSAsync("Roboto", fontPath);
            }
            else if (fontFamily == "Lato" && string.IsNullOrEmpty(_latoFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Lato");
                _latoFontCSS = await BuildFontCSSAsync("Lato", fontPath);
            }
            else if (string.IsNullOrEmpty(_interFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Inter");
                _interFontCSS = await BuildFontCSSAsync("Inter", fontPath);
            }

            return fontFamily switch
            {
                "Open Sans" => _openSansFontCSS,
                "Roboto"    => _robotoFontCSS,
                "Lato"      => _latoFontCSS,
                _           => _interFontCSS
            };
        }

        public async Task<string> BuildFontCSSAsync(string family, string path)
        {
            var styles = new[]
            {
                ("Light", 200, "normal"),
                ("LightItalic", 200, "italic"),
                ("Regular", 400, "normal"),
                ("Italic", 400, "italic"),
                ("Medium", 500, "normal"),
                ("MediumItalic", 500, "italic"),
                ("Bold", 700, "normal"),
                ("BoldItalic", 700, "italic")
            };

            var sb = new StringBuilder();
            var tasks = new List<Task<string>>();

            foreach (var (style, weight, fontStyle) in styles)
            {
                string file = Path.Combine(path, $"{family.Replace(" ", "")}-{style}.ttf");
                tasks.Add(ConvertToBase64Async(file));
            }

            var base64Results = await Task.WhenAll(tasks);

            for (int i = 0; i < styles.Length; i++)
            {
                var (style, weight, fontStyle) = styles[i];
                sb.Append(
                    $"@font-face {{ font-family: '{family}'; " +
                    $"src: url('{base64Results[i]}') format('truetype'); " +
                    $"font-weight: {weight}; font-style: {fontStyle}; " +
                    $"unicode-range: U+0020-007E, U+00A0-00FF; }}\n"
                );
            }

            return sb.ToString();
        }

        public async Task<string> RenderTemplate(string templatePath, Root request)
        {
            return await _razorTemplateService.RenderTemplateAsync(templatePath, request);
        }

        private async Task<string> ConvertToBase64Async(string filePath)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            return "data:font/truetype;charset=utf-8;base64," + Convert.ToBase64String(fileBytes);
        }

        /// <summary>
        /// Sanitizes a filename by removing invalid characters
        /// </summary>
        /// <param name="fileName">The filename to sanitize</param>
        /// <returns>A sanitized filename safe for file system use</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return $"PDF_{DateTime.Now:yyyyMMddHHmmss}";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();

            foreach (char character in fileName)
            {
                if (!invalidChars.Contains(character))
                {
                    sanitized.Append(character);
                }
                else
                {
                    sanitized.Append('_');
                }
            }

            return sanitized.ToString().Trim('_');
        }

        private async Task<string> CreatePdfDocumentAsync(
            string header,
            string body,
            string footer,
            string commonStyles,
            string headerStyles,
            string footerStyles,
            string bodyStyles,
            Root request,
            string backgroundStyles)
        {
            var themeCSS = new StringBuilder();

            themeCSS.Append(await LoadFontCSSAsync(request?.Theme?.Font?.Family ?? string.Empty));


            themeCSS.Append("html, body {");

            var fontFamily = request?.Theme?.Font?.Family switch
            {
                "Open Sans" => "Open Sans",
                "Lato"      => "Lato",
                "Roboto"    => "Roboto",
                _           => "Inter"
            };

            themeCSS.Append($"--font-family: \"{fontFamily}\";");
            themeCSS.Append($"--font-size-default: {request?.Theme?.Font?.FontSizeDefault - decreaseFontSize}px;");
            themeCSS.Append($"--font-size-large: {request?.Theme?.Font?.FontSizeDefault}px;");
            themeCSS.Append($"--font-size-small: {request?.Theme?.Font?.FontSizeSmall - decreaseFontSize}px;");
            themeCSS.Append($"--font-size-medium: {request?.Theme?.Font?.FontSizeMedium - decreaseFontSize}px;");
            themeCSS.Append($"--color-primary: {request?.Theme?.PrimaryColor};");
            themeCSS.Append($"--color-secondary: {request?.Theme?.SecondaryColor};");
            themeCSS.Append("}");

            var allStylesBuilder = new StringBuilder();
            allStylesBuilder.Append(commonStyles)
                           .Append(headerStyles)
                           .Append(bodyStyles)
                           .Append(footerStyles)
                           .Append(themeCSS);

            bool repeatHeaderFooter = request?.ShowSectionsInline != true;

            return $@"
            <html>
                <head>
                    <style>
                        {allStylesBuilder}
                        {(repeatHeaderFooter ? backgroundStyles : string.Empty)}
                    </style>
                </head>
                <body class='{(repeatHeaderFooter ? "repeat-header-footer" : "")}'>
                    <div style='display: flex; flex-direction: column; height: -webkit-fill-available;'>
                        {header}
                        {body}
                        {footer}
                    </div>
                </body>
            </html>";
        }

        public async Task<string> GeneratePdfToFileAsync(Root request)
        {
            var browser = await GetBrowserAsync();
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
                    Top = $"{Math.Max(request?.Theme?.Margin?.Top ?? 0, 10)}px",
                    Bottom = $"{Math.Max(request?.Theme?.Margin?.Bottom ?? 0, 15)}px",
                    Left = $"{Math.Max(request?.Theme?.Margin?.Left ?? 0, 10)}px",
                    Right = $"{Math.Max(request?.Theme?.Margin?.Right ?? 0, 10)}px"
                }
            };

            try
            {
                string templateType = request?.TemplateType?.ToUpper();

                string templateFolderName = templateType switch
                {
                    "TALLY" => "Tally",
                    "THERMAL" => "Thermal",
                    _ => "TemplateA"
                };

                string templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Templates",
                    templateFolderName
                );

                var styles = await LoadStylesAsync(templatePath);

                string headerFile = null;
                string bodyFile = null;
                string footerFile = "Footer.cshtml";

                bool isReceiptOrPayment = false;
                bool isThermal = templateFolderName == "Thermal";

                switch (templateFolderName)
                {
                    case "Tally":
                        headerFile = "Header.cshtml";
                        bodyFile = "Body.cshtml";
                        footerFile = "Footer.cshtml";
                        break;

                    case "TemplateA":
                        if (string.Equals(request?.VoucherType,
                                VoucherTypeEnums.Receipt.GetVoucherTypeEnumValue(),
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(request?.VoucherType,
                                VoucherTypeEnums.Payment.GetVoucherTypeEnumValue(),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            isReceiptOrPayment = true;
                            bodyFile = "Receipt_Payment_Body.cshtml";
                        }
                        else if (
                            string.Equals(request?.VoucherType,
                                VoucherTypeEnums.PurchaseOrder.GetVoucherTypeEnumValue(),
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(request?.VoucherType,
                                VoucherTypeEnums.PurchaseBill.GetVoucherTypeEnumValue(),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            headerFile = "PO_PB_Header.cshtml";
                            bodyFile = "PO_PB_Body.cshtml";
                        }
                        else
                        {
                            headerFile = "Header.cshtml";
                            bodyFile = "Body.cshtml";
                        }

                        break;

                    case "Thermal":
                        bodyFile = "Body.cshtml";
                        break;

                    default:
                        headerFile = "Header.cshtml";
                        bodyFile = "Body.cshtml";
                        break;
                }

                string header = null, footer = null, body;

                if (isReceiptOrPayment || isThermal)
                {
                    body = await RenderTemplate(Path.Combine(templatePath, bodyFile), request);
                }
                else
                {
                    var tasks = new[]
                    {
                        RenderTemplate(Path.Combine(templatePath, headerFile), request),
                        RenderTemplate(Path.Combine(templatePath, footerFile), request),
                        RenderTemplate(Path.Combine(templatePath, bodyFile), request)
                    };

                    await Task.WhenAll(tasks);

                    header = tasks[0].Result;
                    footer = tasks[1].Result;
                    body = tasks[2].Result;
                }

                string html = await CreatePdfDocumentAsync(
                    header,
                    body,
                    footer,
                    styles.Common,
                    styles.Header,
                    styles.Footer,
                    styles.Body,
                    request,
                    styles.Background
                );

                await page.SetContentAsync(html);
                await page.EmulateMediaTypeAsync(MediaType.Print);

                // Generate temporary file path
                string tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                Directory.CreateDirectory(tempPath);
                
                string fileName = !string.IsNullOrWhiteSpace(request?.PdfRename) 
                    ? SanitizeFileName(request.PdfRename) 
                    : $"PDF_{DateTime.Now:yyyyMMddHHmmss}";
                
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
                }

                return tempFilePath;
            }
            finally
            {
                await page.CloseAsync();
                await page.DisposeAsync();
            }
        }
    }
}
