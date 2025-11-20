using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using InvoiceData;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using GiddhTemplate.Aspects;

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
    [Log]  // Metalama: logs all actions, parameters, exceptions
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
            // Deserialize request
            var jsonString = JsonSerializer.Serialize(requestObj);
            Root request = JsonSerializer.Deserialize<Root>(jsonString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null || string.IsNullOrEmpty(request.Company?.Name))
            {
                return BadRequest("Invalid request data. Ensure payload matches expected format.");
            }

            // Generate PDF
            byte[] pdfBytes = await _pdfService.GeneratePdfAsync(request);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await _slackService.SendErrorAlertAsync(
                    url: "api/v1/pdf",
                    environment: _environment,
                    error: "PDF generation returned empty result.",
                    stackTrace: "No stacktrace (service returned empty bytes)."
                );

                return StatusCode(500, new { error = "Failed to generate PDF!" });
            }

            // Return PDF
            return File(pdfBytes, "application/pdf", "invoice.pdf");
        }
    }
}
