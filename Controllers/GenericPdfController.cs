using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Dynamic;

namespace GiddhTemplate.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GenericPdfController : ControllerBase
    {
        private readonly GenericPdfService _pdfService;
        private readonly ISlackService _slackService;
        private readonly string _environment;

        public GenericPdfController(GenericPdfService pdfService, ISlackService slackService, IConfiguration configuration)
        {
            _pdfService = pdfService;
            _slackService = slackService;
            _environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdfAsync([FromBody] JsonElement requestObj)
        {
            string? tempFilePath = null;
            
            try
            {
                if (requestObj.ValueKind == JsonValueKind.Null || requestObj.ValueKind == JsonValueKind.Undefined)
                {
                    return BadRequest("Request body cannot be empty.");
                }

                // Pass JsonElement directly to service
                tempFilePath = await _pdfService.GeneratePdfToFileAsync(requestObj);

                if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    await _slackService.SendErrorAlertAsync(
                        url: "api/v1/genericpdf",
                        environment: _environment,
                        error: "PDF generation returned empty result.",
                        stackTrace: "No stacktrace (service returned empty file path)."
                    );

                    return StatusCode(500, new { error = "Failed to generate PDF!" });
                }

                var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(fileStream, "application/pdf", "document.pdf");
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }

                await _slackService.SendErrorAlertAsync(
                    url: "api/v1/pdf",
                    environment: _environment,
                    error: $"PDF generation failed: {ex.Message}",
                    stackTrace: ex.StackTrace ?? "No stacktrace available"
                );

                return StatusCode(500, new { error = "PDF generation failed", details = ex.Message });
            }
        }
    }
}
