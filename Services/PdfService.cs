using IronPdf;
using IronPdf.Rendering;
using System.IO;
using System.Threading.Tasks;
using InvoiceData;

namespace GiddhTemplate.Services
{
    public class PdfService
    {
        private readonly PdfRendererConfigService _rendererConfig;
        private readonly RazorTemplateService _razorTemplateService;
        private readonly Lazy<(string Common, string Header, string Footer, string Body)> _styles;

        public PdfService()
        {
            _rendererConfig = new PdfRendererConfigService();
            _razorTemplateService = new RazorTemplateService();

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Gst");
            _styles = new Lazy<(string, string, string, string)>(() => LoadStyles(templatePath));
        }

        private string LoadFileContent(string filePath)
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        }

        private (string Common, string Header, string Footer, string Body) LoadStyles(string basePath)
        {
            return (
                Common: LoadFileContent(Path.Combine(basePath, "Styles", "Styles.css")),
                Header: LoadFileContent(Path.Combine(basePath, "Styles", "Header.css")),
                Footer: LoadFileContent(Path.Combine(basePath, "Styles", "Footer.css")),
                Body: LoadFileContent(Path.Combine(basePath, "Styles", "Body.css"))
            );
        }

        private async Task<string> RenderTemplate(string templatePath, Root request)
        {
            return await _razorTemplateService.RenderTemplateAsync(templatePath, request);
        }

        private HtmlHeaderFooter CreateHtmlHeaderFooter(string styles, string content)
        {
            return new HtmlHeaderFooter
            {
                HtmlFragment = $"<style>{styles}</style>{content}",
                MaxHeight = HtmlHeaderFooter.FragmentHeight
            };
        }

        private PdfDocument CreatePdfDocument(string header, string body, string footer, string commonStyles, string headerStyles, string footerStyles, string bodyStyles, ChromePdfRenderer renderer, Root request)
        {
            switch (request?.TemplateType?.ToUpper())
            {
                case "TALLY":
                    if (request?.ShowSectionsInline == true)
                    {
                        renderer.RenderingOptions.MarginTop = 0;
                        renderer.RenderingOptions.MarginLeft = 0;
                        renderer.RenderingOptions.MarginRight = 0;
                        renderer.RenderingOptions.MarginBottom = 0;
                        return renderer.RenderHtmlAsPdf($"<style>{commonStyles}{bodyStyles}{headerStyles}</style>{header}{body}");
                    }
                    else
                    {
                        renderer.RenderingOptions.HtmlHeader = CreateHtmlHeaderFooter($"{commonStyles}{headerStyles}", header);
                        // renderer.RenderingOptions.HtmlFooter = CreateHtmlHeaderFooter($"{commonStyles}{footerStyles}", footer);
                        return renderer.RenderHtmlAsPdf($"<style>{commonStyles}{bodyStyles}</style>{body}");
                    }

                default:
                    renderer.RenderingOptions.MarginTop = 0;
                    renderer.RenderingOptions.MarginLeft = 0;
                    renderer.RenderingOptions.MarginRight = 0;
                    renderer.RenderingOptions.MarginBottom = 0;
                    renderer.RenderingOptions.HtmlHeader = CreateHtmlHeaderFooter($"{commonStyles}{headerStyles}", header);
                    renderer.RenderingOptions.HtmlFooter = CreateHtmlHeaderFooter($"{commonStyles}{footerStyles}", footer);
                    return renderer.RenderHtmlAsPdf($"<style>{commonStyles}{bodyStyles}</style>{body}");
            }
        }

        private void GenerateLocalPdfFile(PdfDocument pdf)
        {
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            string pdfName = "PDF_" + DateTimeOffset.Now.ToString("HHmmssfff") + ".pdf";
            string filePath = Path.Combine(rootPath, pdfName);
            pdf.SaveAs(filePath);
            Console.WriteLine("PDF Downloaded, Please check -> " + pdfName);
        }

        public async Task<string> GeneratePdfAsync(Root request)
        {
            Console.WriteLine("PDF Generation Started ...");

            var renderer = _rendererConfig.GetConfiguredRenderer();
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Gst");
            var (commonStyles, headerStyles, footerStyles, bodyStyles) = _styles.Value;

            string header = await RenderTemplate(Path.Combine(templatePath, "Header.cshtml"), request);
            string footer = await RenderTemplate(Path.Combine(templatePath, "Footer.cshtml"), request);
            string body = await RenderTemplate(Path.Combine(templatePath, "Body.cshtml"), request);
            // Console.WriteLine("Header HTML:" + $"<style>{commonStyles}{headerStyles}</style>{header}");
            // Console.WriteLine("Body HTML:" + $"<style>{commonStyles}{bodyStyles}</style>{body}");
            // Console.WriteLine("Footer HTML:" + $"<style>{commonStyles}{footerStyles}</style>{footer}");

            PdfDocument pdf = CreatePdfDocument(header, body, footer, commonStyles, headerStyles, footerStyles, bodyStyles, renderer, request);

            // Uncomment below line to save PDF file in local 
            GenerateLocalPdfFile(pdf);

            return Convert.ToBase64String(pdf.BinaryData);
        }
    }
}
