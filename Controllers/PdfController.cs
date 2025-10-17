using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using InvoiceData;
using Microsoft.AspNetCore.Mvc.Filters;
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
            try
            {
                var jsonString = JsonSerializer.Serialize(requestObj);
                Root request = JsonSerializer.Deserialize<Root>(jsonString, new JsonSerializerOptions
                {
                        PropertyNameCaseInsensitive = true
                });
                if (request == null || string.IsNullOrEmpty(request.Company?.Name))
                {
                            return BadRequest("Invalid request data. Ensure the payload matches the expected format.");
                }
                
                // Use ASP.NET Core's built-in ActionId for request correlation (no need to pass custom ID)
                byte[] pdfBytes = await _pdfService.GeneratePdfAsync(request);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return StatusCode(500, new { error = "Failed to generate PDF!" });
                }
                // Return the PDF as a file download
                return File(pdfBytes, "application/pdf", "invoice.pdf");
            }
            catch (Exception ex)
            {
                 var url = "api/v1/pdf";
                 var error = ex.Message;
                 var stackTrace = ex.StackTrace ?? "No stack trace available";
                 _ = Task.Run(async () => await _slackService.SendErrorAlertAsync(url, _environment, error, stackTrace));

                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
