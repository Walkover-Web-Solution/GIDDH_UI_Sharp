using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using GiddhTemplate.Models;
using System.Text.Json;

namespace GiddhTemplate.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly CertificatePdfService _certificatePdfService;
        private readonly ISlackService _slackService;
        private readonly string _environment;

        public CertificateController(CertificatePdfService certificatePdfService, ISlackService slackService, IConfiguration configuration)
        {
            _certificatePdfService = certificatePdfService;
            _slackService = slackService;
            _environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdfAsync([FromBody] JsonElement requestObj)
        {
            string? tempFilePath = null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                CertificateRequest? request = requestObj.Deserialize<CertificateRequest>(options);

                if (request == null || string.IsNullOrEmpty(request.name))
                {
                    return BadRequest(new { error = "Invalid request data. Ensure payload includes name, score, issuedAt and validUntil." });
                }

                tempFilePath = await _certificatePdfService.GenerateCertificatePdfToFileAsync(request);

                if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    await _slackService.SendErrorAlertAsync(
                        url: "api/v1/certificate",
                        environment: _environment,
                        error: "PDF generation returned empty result.",
                        stackTrace: "No stacktrace (service returned empty file path)."
                    );

                    return StatusCode(500, new { error = "Failed to generate certificate!" });
                }

                var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(fileStream, "application/pdf", "certificate.pdf");
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }

                await _slackService.SendErrorAlertAsync(
                    url: "api/v1/certificate",
                    environment: _environment,
                    error: $"Certificate generation failed: {ex.Message}",
                    stackTrace: ex.StackTrace ?? "No stacktrace available"
                );

                return StatusCode(500, new { error = "Certificate generation failed", details = ex.Message });
            }
        }
    }
}
