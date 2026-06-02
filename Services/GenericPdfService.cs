using PuppeteerSharp;
using System.Text;
using PuppeteerSharp.Media;
using GiddhTemplate.Models.Enums;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Dynamic;

namespace GiddhTemplate.Services
{
    public class GenericPdfService
    {
        private readonly GenericRazorTemplateService _razorTemplateService;
        private readonly PdfService _pdfService;
        private static SemaphoreSlim _pdfGenerationSemaphore = new SemaphoreSlim(2);
        private static int _maxConcurrentPdfs = 2;

        public GenericPdfService(GenericRazorTemplateService razorTemplateService, PdfService pdfService)
        {
            _razorTemplateService = razorTemplateService;
            _pdfService = pdfService;
        }

        public async Task<string> GeneratePdfToFileAsync<T>(T request)
        {
            return await GeneratePdfToFileAsyncInternal(request);
        }

        private async Task<string> GeneratePdfToFileAsyncInternal<T>(T request)
        {
            CheckMemoryPressure();
            
            var semaphoreWaitStart = DateTime.UtcNow;
            bool acquired = await _pdfGenerationSemaphore.WaitAsync(TimeSpan.FromSeconds(90));
            
            if (!acquired)
            {
                Console.WriteLine("[GenericPdfService] PDF generation queue timeout - another request is taking too long. Rejecting request.");
                throw new TimeoutException("PDF generation service is busy. Please retry after a few seconds.");
            }
            
            var waitDuration = (DateTime.UtcNow - semaphoreWaitStart).TotalSeconds;
            int activeSlots = 2 - _pdfGenerationSemaphore.CurrentCount;
            Console.WriteLine($"[GenericPdfService] Acquired semaphore slot (waited {waitDuration:F2}s, {activeSlots} active slots)");

            try
            {
                string templateType = GetPropertyValue(request, "templateType")?.ToString()?.ToUpper() ?? "DEFAULT";
                string templateFolderName = templateType switch
                {
                    "TALLY" => "Tally",
                    "THERMAL" => "Thermal",
                    "TEMPLATE_A" => "other-template/template_a",
                    "TEMPLATE_B" => "other-template/template_b",
                    "TEMPLATE_C" => "other-template/template_c",
                    "TEMPLATE_D" => "other-template/template_d",
                    "BULK_INVOICE_FAILURE" => "other-template/bulk_invoice_failure",
                    "DAYBOOK_ADMIN_CONDENSED" => "other-template/daybook_admin_condensed",
                    "DAYBOOK_ADMIN_DETAILED" => "other-template/daybook_admin_detailed",
                    "ENTRY" => "other-template/entry",
                    "GST_TEMPLATE_A" => "other-template/gst_template_a",
                    "GST_TEMPLATE_A_1" => "other-template/gst_template_a_1",
                    "GST_TEMPLATE_A_V2" => "other-template/gst_template_a_v2",
                    "GST_TEMPLATE_BACKUP" => "other-template/gst_template_backup",
                    "PURCHASE_BILL_EMAIL_TEMPLATE" => "other-template/purchase_bill_email_template",
                    "LEDGER_TEMPLATE" => "other-template/ledger_template",
                    "INVENTORY_REPORT_FOR_COMPANY" => "other-template/inventory_report_for_company",
                    "INVENTORY_REPORT_FOR_STOCK" => "other-template/inventory_report_for_stock",
                    "LEDGER_ADMIN_CONDENSED" => "other-template/ledger_admin_condensed",
                    "LEDGER_ADMIN_CONDENSED_COMPANY" => "other-template/ledger_admin_condensed_company",
                    "LEDGER_ADMIN_DETAILED" => "other-template/ledger_admin_detailed",
                    "LEDGER_ADMIN_DETAILED_COMPANY" => "other-template/ledger_admin_detailed_company",
                    "LEDGER_VIEW_CONDENSED" => "other-template/ledger_view_condensed",
                    "LEDGER_VIEW_DETAILED" => "other-template/ledger_view_detailed",
                    "LEDGER_TEMPLATE_UPPER" => "other-template/LedgerTemplate",
                    "PURCHASE_ORDER_EMAIL_TEMPLATE" => "other-template/purchase_order_email_template",
                    "RECEIPT" => "other-template/receipt",
                    "STOCK_REPORT_V2" => "other-template/stock_report_v2",
                    "WAREHOUSE_REPORT" => "other-template/warehouse_report",
                    "TEMPLATE_E_1" => "other-template/template_e_1",
                    "VOUCHER_TEMPLATE_A" => "other-template/voucher_template_a",
                    "VOUCHER_TMEPLATE_B" => "other-template/voucher_tmeplate_b",
                    "THERMAL_TEMPLATE" => "other-template/thermal_template",
                    "TRANSFER_TEMPLATE_A_V1" => "other-template/transfer_template_a_v1",
                    "TALLY_TEMPLATE" => "other-template/tally_template",
                    "PO_TEMPLATE" => "other-template/po_template",
                    _ => "other-template/template_a"
                };

                string templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Templates",
                    templateFolderName
                );

                if (!Directory.Exists(templatePath))
                {
                    throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
                }

                var styles = await LoadStylesAsync(templatePath);

                string templateFile = "Template.cshtml";
                string html = await RenderTemplate(Path.Combine(templatePath, templateFile), request);

                var tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                Directory.CreateDirectory(tempPath);
                
                var pdfRename = GetPropertyValue(request, "PdfRename")?.ToString();
                string fileName = !string.IsNullOrWhiteSpace(pdfRename) 
                    ? SanitizeFileName(pdfRename) 
                    : $"PDF_{DateTime.Now:yyyyMMddHHmmss}";
                
                string tempFilePath = Path.Combine(tempPath, $"{fileName}_{Guid.NewGuid():N}.pdf");

                Console.WriteLine($"[GenericPdfService] Generating PDF to: {tempFilePath}");
                
                var browser = await _pdfService.GetBrowserAsync();
                var page = await browser.NewPageAsync();

                try
                {
                    dynamic pdfOptions = new
                    {
                        Format = "A4",
                        Landscape = false,
                        PrintBackground = true,
                        PreferCSSPageSize = true,
                        DisplayHeaderFooter = false,
                        MarginOptions = new
                        {
                            Top = "10px",
                            Bottom = "15px",
                            Left = "10px",
                            Right = "10px"
                        }
                    };

                    await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
                    await page.EmulateMediaTypeAsync(MediaType.Print);
                    await page.PdfAsync(tempFilePath, new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        Landscape = false,
                        PrintBackground = true,
                        PreferCSSPageSize = true,
                        DisplayHeaderFooter = false,
                        MarginOptions = new MarginOptions
                        {
                            Top = "10px",
                            Bottom = "15px",
                            Left = "10px",
                            Right = "10px"
                        }
                    });

                    var fileSize = new FileInfo(tempFilePath).Length;
                    Console.WriteLine($"[GenericPdfService] PDF generated successfully: {fileSize} bytes");
                    
                    return tempFilePath;
                }
                finally
                {
                    if (page != null)
                    {
                        await page.CloseAsync();
                    }
                }
            }
            finally
            {
                _pdfGenerationSemaphore.Release();
                Console.WriteLine("[GenericPdfService] Released semaphore slot");
            }
        }

        private async Task<string> RenderTemplate(string templatePath, dynamic data)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            return await _razorTemplateService.RenderTemplateAsync(templatePath, data);
        }

        private async Task<string> CreatePdfDocumentAsync(string header, string body, string footer, string commonStyles, string headerStyles, string footerStyles, string bodyStyles, dynamic data, string backgroundStyles)
        {
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html>");
            html.Append("<html>");
            html.Append("<head>");
            html.Append("<meta charset=\"UTF-8\">");
            html.Append("<style>");
            
            if (!string.IsNullOrWhiteSpace(commonStyles))
                html.Append(commonStyles);
            if (!string.IsNullOrWhiteSpace(headerStyles))
                html.Append(headerStyles);
            if (!string.IsNullOrWhiteSpace(bodyStyles))
                html.Append(bodyStyles);
            if (!string.IsNullOrWhiteSpace(footerStyles))
                html.Append(footerStyles);
            if (!string.IsNullOrWhiteSpace(backgroundStyles))
                html.Append(backgroundStyles);
            
            html.Append("</style>");
            html.Append("</head>");
            html.Append("<body>");
            
            if (!string.IsNullOrWhiteSpace(header))
                html.Append(header);
            if (!string.IsNullOrWhiteSpace(body))
                html.Append(body);
            if (!string.IsNullOrWhiteSpace(footer))
                html.Append(footer);
            
            html.Append("</body>");
            html.Append("</html>");

            return html.ToString();
        }

        private async Task<(string Common, string Header, string Footer, string Body, string Background)> LoadStylesAsync(string templatePath)
        {
            var commonPath = Path.Combine(templatePath, "common.css");
            var headerPath = Path.Combine(templatePath, "header.css");
            var footerPath = Path.Combine(templatePath, "footer.css");
            var bodyPath = Path.Combine(templatePath, "body.css");
            var backgroundPath = Path.Combine(templatePath, "background.css");

            var common = File.Exists(commonPath) ? await File.ReadAllTextAsync(commonPath) : "";
            var header = File.Exists(headerPath) ? await File.ReadAllTextAsync(headerPath) : "";
            var footer = File.Exists(footerPath) ? await File.ReadAllTextAsync(footerPath) : "";
            var body = File.Exists(bodyPath) ? await File.ReadAllTextAsync(bodyPath) : "";
            var background = File.Exists(backgroundPath) ? await File.ReadAllTextAsync(backgroundPath) : "";

            return (common, header, footer, body, background);
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        }

        private void CheckMemoryPressure()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var totalMemoryMB = totalMemory / 1024 / 1024;
            var totalAvailableMB = GC.GetTotalMemory(false) / 1024 / 1024;

            const int highThresholdMB = 800;
            const int lowThresholdMB = 400;

            if (totalMemoryMB > highThresholdMB)
            {
                if (_maxConcurrentPdfs > 1)
                {
                    _maxConcurrentPdfs = 1;
                    Console.WriteLine($"[GenericPdfService] ⚠️ High memory pressure ({totalMemoryMB}MB / {totalAvailableMB}MB total). Limiting to 1 concurrent PDF generation.");
                }
            }
            else if (totalMemoryMB < lowThresholdMB)
            {
                if (_maxConcurrentPdfs == 1)
                {
                    _maxConcurrentPdfs = 2;
                    Console.WriteLine($"[GenericPdfService] ✅ Memory pressure normalized ({totalMemoryMB}MB / {totalAvailableMB}MB total). Restoring to 2 concurrent PDF generations.");
                }
            }
        }


        private object GetPropertyValue<T>(T obj, string propertyName)
        {
            if (obj == null) return null;

            // Handle JsonElement
            if (obj is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(propertyName, out var value))
                {
                    return value;
                }
                return null;
            }

            // Handle ExpandoObject
            if (obj is ExpandoObject expandoObj)
            {
                var dict = (IDictionary<string, object>)expandoObj;
                return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
            }

            // Handle Dictionary
            if (obj is IDictionary<string, object> dictObj)
            {
                return dictObj.ContainsKey(propertyName) ? dictObj[propertyName] : null;
            }

            // Handle regular objects via reflection
            var property = obj.GetType().GetProperty(propertyName, 
                System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public);
            return property?.GetValue(obj);
        }
    }
}
