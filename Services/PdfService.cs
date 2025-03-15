using PuppeteerSharp;
using System.Text;
using InvoiceData;
using PuppeteerSharp.Media;
using System.Collections.Concurrent;

namespace GiddhTemplate.Services
{
    public class PdfService
    {
        private readonly RazorTemplateService _razorTemplateService;
        private string _openSansFontCSS = ""; // Cache the Open Sans CSS
        private string _openRobotoFontCSS = ""; // Cache the Roboto CSS
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static IBrowser? _browser;
        private static readonly ConcurrentDictionary<string, string> _renderedTemplates = new ConcurrentDictionary<string, string>();
        private static readonly PdfOptions _cachedPdfOptions = new PdfOptions
        {
            Format = PaperFormat.A4,
            Landscape = false,
            MarginOptions = new MarginOptions
            {
                Top = "15px",
                Bottom = "30px",
                Left = "0px",
                Right = "0px"
            },
            PrintBackground = true,
            PreferCSSPageSize = true,
            DisplayHeaderFooter = false,
        };

        public static async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null || !_browser.IsConnected)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_browser == null || !_browser.IsConnected)
                    {
                        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                        {
                            Headless = true,
                            ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
                            // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path
                            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
                        });
                    }
                }
                catch (PuppeteerSharp.ProcessException ex)
                {
                    Console.WriteLine($"Error launching browser: {ex.Message}");
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

        public PdfService()
        {
            _razorTemplateService = new RazorTemplateService();
        }

        private string LoadFileContent(string filePath)
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        }

        private (string Common, string Header, string Footer, string Body, string Background) LoadStyles(string basePath)
        {
            return (
                LoadFileContent(Path.Combine(basePath, "Styles", "Styles.css")),
                LoadFileContent(Path.Combine(basePath, "Styles", "Header.css")),
                LoadFileContent(Path.Combine(basePath, "Styles", "Footer.css")),
                LoadFileContent(Path.Combine(basePath, "Styles", "Body.css")),
                LoadFileContent(Path.Combine(basePath, "Styles", "Background.css"))
            );
        }

        private string LoadOpenSansFontCSS()
        {
            if (_openSansFontCSS != null) // Load only once
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "OpenSans");
                _openSansFontCSS = $@"
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-Light.ttf")}') format('truetype'); font-weight: 200; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-LightItalic.ttf")}') format('truetype'); font-weight: 200; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-Regular.ttf")}') format('truetype'); font-weight: 400; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-Italic.ttf")}') format('truetype'); font-weight: 400; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-Medium.ttf")}') format('truetype'); font-weight: 500; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-MediumItalic.ttf")}') format('truetype'); font-weight: 500; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-Bold.ttf")}') format('truetype'); font-weight: 700; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: 'Open Sans'; src: url('{ConvertToBase64(fontPath + "/OpenSans-BoldItalic.ttf")}') format('truetype'); font-weight: 700; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                    ";
            }
            return _openSansFontCSS ?? string.Empty;
        }
        private string LoadRobotoFontCSS()
        {
            if (_openRobotoFontCSS != null) // Load only once
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Roboto");
                _openRobotoFontCSS = $@"
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-Light.ttf")}') format('truetype'); font-weight: 200; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-LightItalic.ttf")}') format('truetype'); font-weight: 200; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-Regular.ttf")}') format('truetype'); font-weight: 400; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-Italic.ttf")}') format('truetype'); font-weight: 400; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-Medium.ttf")}') format('truetype'); font-weight: 500; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-MediumItalic.ttf")}') format('truetype'); font-weight: 500; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-Bold.ttf")}') format('truetype'); font-weight: 700; font-style: normal; unicode-range: U+0020-007E, U+00A0-00FF; }}
                        @font-face {{ font-family: Roboto; src: url('{ConvertToBase64(fontPath + "/Roboto-BoldItalic.ttf")}') format('truetype'); font-weight: 700; font-style: italic; unicode-range: U+0020-007E, U+00A0-00FF; }}
                    ";
            }
            return _openRobotoFontCSS ?? string.Empty;
        }

        private async Task<string> RenderTemplate(string templatePath, Root request)
        {
            string cacheKey = $"{templatePath}-{request.GetHashCode()}";
            if (_renderedTemplates.TryGetValue(cacheKey, out string cachedResult))
            {
                return cachedResult;
            }

            string renderedTemplate = await _razorTemplateService.RenderTemplateAsync(templatePath, request);
            _renderedTemplates.TryAdd(cacheKey, renderedTemplate);
            return renderedTemplate;
        }

        string ConvertToBase64(string filePath)
        {
            byte[] fontBytes = File.ReadAllBytes(filePath);
            return "data:font/truetype;charset=utf-8;base64," + Convert.ToBase64String(fontBytes);
        }

        private string CreatePdfDocument(string header, string body, string footer, string commonStyles, string headerStyles, string footerStyles, string bodyStyles, Root request, string backgroundStyles)
        {
            var themeCSS = new StringBuilder();
            Console.WriteLine("Load Font Start: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            if (request?.Theme?.Font?.Family == "Open Sans")
            {
                themeCSS.Append(LoadOpenSansFontCSS());
            }
            else if (request?.Theme?.Font?.Family == "Roboto")
            {
                themeCSS.Append(LoadRobotoFontCSS());
            }
            Console.WriteLine("Load Font End: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            themeCSS.Append("html, body {");
            themeCSS.Append($"--font-family: \"{request?.Theme?.Font?.Family}\";");
            themeCSS.Append($"--font-size-default: {request?.Theme?.Font?.FontSizeDefault}px;");
            themeCSS.Append($"--font-size-large: {request?.Theme?.Font?.FontSizeDefault + 4}px;");
            themeCSS.Append($"--font-size-small: {request?.Theme?.Font?.FontSizeSmall}px;");
            themeCSS.Append($"--font-size-medium: {request?.Theme?.Font?.FontSizeMedium}px;");
            themeCSS.Append($"--color-primary: {request?.Theme?.PrimaryColor};");
            themeCSS.Append($"--color-secondary: {request?.Theme?.SecondaryColor};");
            if (request?.Theme?.Font?.Family == "Roboto")
            {
                themeCSS.Append($"font-weight: var(--font-weight-500, 500);");
            }
            themeCSS.Append("}");

            var allStyles = $"{commonStyles}{headerStyles}{bodyStyles}{footerStyles}{themeCSS}";
            bool repeatHeaderFooter = request?.ShowSectionsInline != true;
            return $@"<html> 
                <head> 
                    <style>
                        {allStyles}
                        {(repeatHeaderFooter ? backgroundStyles : string.Empty)}
                    </style>
                </head> 
                <body class={(repeatHeaderFooter ? "repeat-header-footer" : "")}>
                    <div style='display: flex; flex-direction: column; height: -webkit-fill-available;'>
                        {header}
                        {body}
                        {footer}
                    </div>
                </body> 
            </html>";
        }

        private string GetFileNameWithPath(Root request)
        {
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            Directory.CreateDirectory(rootPath);
            string pdfName = $"{(string.IsNullOrWhiteSpace(request?.PdfRename) ? "PDF" : request.PdfRename)}_{DateTimeOffset.Now:HHmmssfff}.pdf";
            return Path.Combine(rootPath, pdfName);
        }

        public async Task<string?> GeneratePdfAsync(Root request)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            try
            {
                Console.WriteLine("PDF Generation Started ...");
                Console.WriteLine("First : " + DateTime.Now.ToString("HH:mm:ss.fff"));
                string templateFolderName = "TemplateA";
                if (request?.TemplateType?.ToUpper() == "TALLY")
                {
                    templateFolderName = "Tally";
                }
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolderName);
                var (commonStyles, headerStyles, footerStyles, bodyStyles, BackgroundStyles) = LoadStyles(templatePath);
                Console.WriteLine("Get Styles " + DateTime.Now.ToString("HH:mm:ss"));

                // Run template rendering in parallel
                var renderTasks = new[]
                {
                    RenderTemplate(Path.Combine(templatePath, "Header.cshtml"), request),
                    RenderTemplate(Path.Combine(templatePath, "Footer.cshtml"), request),
                    RenderTemplate(Path.Combine(templatePath, "Body.cshtml"), request)
                };
                await Task.WhenAll(renderTasks);

                string header = renderTasks[0].Result;
                string footer = renderTasks[1].Result;
                string body = renderTasks[2].Result;

                Console.WriteLine("Get Templates " + DateTime.Now.ToString("HH:mm:ss.fff"));
                
                // Final HTML store in "template"
                string template = CreatePdfDocument(header, body, footer, commonStyles, headerStyles, footerStyles, bodyStyles, request, BackgroundStyles);
                Console.WriteLine("Get CreatePdfDocument " + DateTime.Now.ToString("HH:mm:ss.fff"));
                await page.SetContentAsync(template);

                await page.EmulateMediaTypeAsync(MediaType.Print);

                // Uncomment below line to save PDF file in local
                // string pdfName = GetFileNameWithPath(request);
                // Console.WriteLine($"PDF Downloaded, Please check -> {pdfName}");
                // await page.PdfAsync(pdfName, _cachedPdfOptions);

                var pdfBytes = await page.PdfDataAsync(_cachedPdfOptions);
                Console.WriteLine("pdfBytes " + DateTime.Now.ToString("HH:mm:ss.fff"));
                return Convert.ToBase64String(pdfBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
            }
            finally
            {
                await page.CloseAsync();
                await page.DisposeAsync();
                page = null;
            }
            return null;
        }
    }
}
