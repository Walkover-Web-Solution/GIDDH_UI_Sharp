using PuppeteerSharp;
using System.Text;
using InvoiceData;
using PuppeteerSharp.Media;
using GiddhTemplate.Models.Enums;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using GiddhTemplate.Extensions;

namespace GiddhTemplate.Services
{
    public class PdfService
    {
        private readonly RazorTemplateService _razorTemplateService;
        private PdfService? _self; // Reference to the proxy instance
        private string _openSansFontCSS = string.Empty; // Cache the Open Sans CSS
        private string _robotoFontCSS = string.Empty;   // Cache the Roboto CSS
        private string _latoFontCSS = string.Empty;     // Cache the Lato CSS
        private string _interFontCSS = string.Empty;    // Cache the Inter CSS
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static IBrowser? _browser;
        private int decreaseFontSize = 2;

        [LogMethod]
        public virtual async Task<IBrowser> GetBrowserAsync()
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
                            // ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
                            ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
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

        public PdfService()
        {
            _razorTemplateService = new RazorTemplateService();
        }

        // Method to set the proxy reference for internal calls
        public virtual void SetProxyReference(PdfService proxyInstance)
        {
            _self = proxyInstance;
        }

        private string LoadFileContent(string filePath) =>
            File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

        [LogMethod]
        public virtual (string Common, string Header, string Footer, string Body, string Background) LoadStyles(string basePath)
        {
            return (
                Common: LoadFileContent(Path.Combine(basePath, "Styles", "Styles.css")),
                Header: LoadFileContent(Path.Combine(basePath, "Styles", "Header.css")),
                Footer: LoadFileContent(Path.Combine(basePath, "Styles", "Footer.css")),
                Body: LoadFileContent(Path.Combine(basePath, "Styles", "Body.css")),
                Background: LoadFileContent(Path.Combine(basePath, "Styles", "Background.css"))
            );
        }

        [LogMethod]
        public virtual string LoadFontCSS(string fontFamily)
        {
            if (fontFamily == "Open Sans" && string.IsNullOrEmpty(_openSansFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "OpenSans");
                _openSansFontCSS = BuildFontCSS("Open Sans", fontPath);
            }
            else if (fontFamily == "Roboto" && string.IsNullOrEmpty(_robotoFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Roboto");
                _robotoFontCSS = BuildFontCSS("Roboto", fontPath);
            }
            else if (fontFamily == "Lato" && string.IsNullOrEmpty(_latoFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Lato");
                _latoFontCSS = BuildFontCSS("Lato", fontPath);
            }
            else if (string.IsNullOrEmpty(_interFontCSS))
            {
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "Inter");
                _interFontCSS = BuildFontCSS("Inter", fontPath);
            }

            return fontFamily switch
            {
                "Open Sans" => _openSansFontCSS,
                "Roboto"    => _robotoFontCSS,
                "Lato"      => _latoFontCSS,
                _           => _interFontCSS
            };
        }

        [LogMethod]
        public virtual string BuildFontCSS(string family, string path)
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
            foreach (var (style, weight, fontStyle) in styles)
            {
                sb.Append(
                    $"@font-face {{ font-family: '{family}'; src: url('{ConvertToBase64(Path.Combine(path, $"{family.Replace(" ", "")}-{style}.ttf"))}') format('truetype'); font-weight: {weight}; font-style: {fontStyle}; unicode-range: U+0020-007E, U+00A0-00FF; }}\n"
                );
            }
            return sb.ToString();
        }

        [LogMethod]
        public virtual async Task<string> RenderTemplate(string templatePath, Root request)
        {
            return await _razorTemplateService.RenderTemplateAsync(templatePath, request);
        }

        string ConvertToBase64(string filePath) =>
            "data:font/truetype;charset=utf-8;base64," + Convert.ToBase64String(File.ReadAllBytes(filePath));

        private string CreatePdfDocument(
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
            // Console.WriteLine("Load Font Start: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            themeCSS.Append((_self ?? this).LoadFontCSS(request?.Theme?.Font?.Family ?? string.Empty));
            // Console.WriteLine("Load Font End: " + DateTime.Now.ToString("HH:mm:ss.fff"));

            themeCSS.Append("html, body {");
            var fontFamily =
                request?.Theme?.Font?.Family == "Open Sans" ? "Open Sans" :
                request?.Theme?.Font?.Family == "Lato"      ? "Lato" :
                request?.Theme?.Font?.Family == "Roboto"    ? "Roboto" : "Inter";
            themeCSS.Append($"--font-family: \"{fontFamily}\";");
            themeCSS.Append($"--font-size-default: {request?.Theme?.Font?.FontSizeDefault - decreaseFontSize}px;");
            themeCSS.Append($"--font-size-large: {request?.Theme?.Font?.FontSizeDefault}px;");
            themeCSS.Append($"--font-size-small: {request?.Theme?.Font?.FontSizeSmall - decreaseFontSize}px;");
            themeCSS.Append($"--font-size-medium: {request?.Theme?.Font?.FontSizeMedium - decreaseFontSize}px;");
            themeCSS.Append($"--color-primary: {request?.Theme?.PrimaryColor};");
            themeCSS.Append($"--color-secondary: {request?.Theme?.SecondaryColor};");
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

        [LogMethod]
        public virtual async Task<byte[]?> GeneratePdfAsync(Root request)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            var _pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                Landscape = false,
                MarginOptions = new MarginOptions
                {
                    Top    = $"{Math.Max(request?.Theme?.Margin?.Top ?? 0, 10)}px",
                    Bottom = $"{Math.Max(request?.Theme?.Margin?.Bottom ?? 0, 15)}px",
                    Left   = $"{Math.Max(request?.Theme?.Margin?.Left ?? 0, 10)}px",
                    Right  = $"{Math.Max(request?.Theme?.Margin?.Right ?? 0, 10)}px"
                },
                PrintBackground = true,
                PreferCSSPageSize = true,
                DisplayHeaderFooter = false
            };


            try
            {
                // Console.WriteLine("PDF Generation Started ...");
                // Console.WriteLine("First : " + DateTime.Now.ToString("HH:mm:ss.fff"));

                string templateType = request?.TemplateType?.ToUpper();

                string templateFolderName;
                switch (templateType)
                {
                    case "TALLY":
                        templateFolderName = "Tally";
                        break;
                    case "THERMAL":
                        templateFolderName = "Thermal";
                        break;
                    default:
                        templateFolderName = "TemplateA";
                        break;
                }


                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFolderName);

                var styles = (_self ?? this).LoadStyles(templatePath);

                string headerFile = null, bodyFile = null, footerFile = "Footer.cshtml";
                bool isReceiptOrPayment = false;
                bool isThermal = false;

                switch (templateFolderName)
                {
                    case "Tally":
                        headerFile = "Header.cshtml";
                        bodyFile = "Body.cshtml";
                        footerFile = "Footer.cshtml";
                        break;
                    case "TemplateA":
                        if (
                            string.Equals(request?.VoucherType, VoucherTypeEnums.Receipt.GetVoucherTypeEnumValue(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(request?.VoucherType, VoucherTypeEnums.Payment.GetVoucherTypeEnumValue(), StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            headerFile = null;
                            bodyFile = "Receipt_Payment_Body.cshtml";
                            isReceiptOrPayment = true;
                        }
                        else if (string.Equals(request?.VoucherType, VoucherTypeEnums.PurchaseOrder.GetVoucherTypeEnumValue(), StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(request?.VoucherType, VoucherTypeEnums.PurchaseBill.GetVoucherTypeEnumValue(), StringComparison.OrdinalIgnoreCase))
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
                        isThermal = true;
                        break;
                    default:
                        headerFile = "Header.cshtml";
                        bodyFile = "Body.cshtml";
                        footerFile = "Footer.cshtml";
                        break;
                }


                // Render logic
                Task<string>[] renderTasks;
                string header = null, footer = null, body;

                if (isReceiptOrPayment)
                {
                    renderTasks = new[]
                    {
                        (_self ?? this).RenderTemplate(Path.Combine(templatePath, bodyFile), request)
                    };
                    await Task.WhenAll(renderTasks);
                    body = renderTasks[0].Result;
                }
                else if (isThermal)
                {
                    renderTasks = new[]
                    {
                        (_self ?? this).RenderTemplate(Path.Combine(templatePath, bodyFile), request)
                    };
                    await Task.WhenAll(renderTasks);
                    body = renderTasks[0].Result;
                }
                else
                {
                    renderTasks = new[]
                    {
                        (_self ?? this).RenderTemplate(Path.Combine(templatePath, headerFile), request),
                        (_self ?? this).RenderTemplate(Path.Combine(templatePath, footerFile), request),
                        (_self ?? this).RenderTemplate(Path.Combine(templatePath, bodyFile), request)
                    };
                    await Task.WhenAll(renderTasks);
                    header = renderTasks[0].Result;
                    footer = renderTasks[1].Result;
                    body = renderTasks[2].Result;
                }

                string template = CreatePdfDocument(header, body, footer, styles.Common, styles.Header, styles.Footer, styles.Body, request, styles.Background);
                await page.SetContentAsync(template);
                await page.EmulateMediaTypeAsync(MediaType.Print);
                var pdfData = await page.PdfDataAsync(_pdfOptions);
                
                return pdfData;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                await page.CloseAsync();
                await page.DisposeAsync();
            }
        }
    }
}