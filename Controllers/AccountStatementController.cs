using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using GiddhTemplate.Models;
using InvoiceData;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GiddhTemplate.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/v1/account-statement")]
    public class AccountStatementController : ControllerBase
    {
        private readonly AccountStatementPdfService _accountStatementPdfService;
        private readonly ISlackService _slackService;
        private readonly string _environment;

        public AccountStatementController(
            AccountStatementPdfService accountStatementPdfService,
            ISlackService slackService,
            IConfiguration configuration)
        {
            _accountStatementPdfService = accountStatementPdfService;
            _slackService = slackService;
            _environment = configuration.GetValue<string>("Environment") ?? "Development";
        }

        [HttpPost]
        public async Task<IActionResult> GenerateAccountStatementPdfAsync([FromBody] object requestObj)
        {
            string? tempFilePath = null;
            
            try
            {
                // Deserialize request
                var jsonString = JsonSerializer.Serialize(requestObj);
                GiddhTemplate.Models.Root request = JsonSerializer.Deserialize<GiddhTemplate.Models.Root>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                if (request == null || string.IsNullOrEmpty(request.AccountName))
                {
                    return BadRequest("Invalid request data. Ensure payload matches expected format.");
                }

                // Generate PDF to temporary file (reduces RAM usage)
                tempFilePath = await _accountStatementPdfService.GenerateAccountStatementPdfToFileAsync(request);

                if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    await _slackService.SendErrorAlertAsync(
                        url: "api/v1/account-statement",
                        environment: _environment,
                        error: "Account statement PDF generation returned empty result.",
                        stackTrace: "No stacktrace (service returned empty file path)."
                    );

                    return StatusCode(500, new { error = "Failed to generate account statement PDF!" });
                }

                // Stream PDF from disk (memory efficient)
                var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(fileStream, "application/pdf", "account-statement.pdf");
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
