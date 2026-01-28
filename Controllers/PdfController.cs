using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using InvoiceData;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GiddhTemplate.Controllers
{
    [ApiController]
    public class MainController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult Get()
        {
            return Ok("Hello from Giddh template!");
        }
    }

    [ApiController]
    [Route("api/v1/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly PdfService _pdfService;
        private readonly ISlackService _slackService;
        private readonly string _environment;

        public PdfController(PdfService pdfService, ISlackService slackService, IConfiguration configuration)
        {
            _pdfService = pdfService;
            _slackService = slackService;
            _environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdfAsync([FromBody] object requestObj)
        {
            string? tempFilePath = null;
            
            try
            {
                // Deserialize request
                var jsonString = JsonSerializer.Serialize(requestObj);
                Root request = JsonSerializer.Deserialize<Root>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request == null || string.IsNullOrEmpty(request.Company?.Name))
                {
                    return BadRequest("Invalid request data. Ensure payload matches expected format.");
                }

                // Generate PDF to temporary file (reduces RAM usage)
                tempFilePath = await _pdfService.GeneratePdfToFileAsync(request);

                if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    await _slackService.SendErrorAlertAsync(
                        url: "api/v1/pdf",
                        environment: _environment,
                        error: "PDF generation returned empty result.",
                        stackTrace: "No stacktrace (service returned empty file path)."
                    );

                    return StatusCode(500, new { error = "Failed to generate PDF!" });
                }

                // Stream PDF from disk (memory efficient)
                var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(fileStream, "application/pdf", "invoice.pdf");
            }
            catch
            {
                // Clean up temp file on error
                if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }
                throw;
            }
        }
    }
}
