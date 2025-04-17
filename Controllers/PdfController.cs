using Microsoft.AspNetCore.Mvc;
using GiddhTemplate.Services;
using InvoiceData;

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

        public PdfController()
        {
            _pdfService = new PdfService();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdfAsync([FromBody] Root request)
        {
            if (request == null || string.IsNullOrEmpty(request.Company?.Name))
            {
                return BadRequest("Invalid request data. Ensure the payload matches the expected format.");
            }

            try
            {
                var base64Pdf = await _pdfService.GeneratePdfAsync(request);
                if (string.IsNullOrEmpty(base64Pdf))
                {
                   return  StatusCode(500, new { error = "Failed to generate PDF !" });
                } else {
                    return Ok(base64Pdf);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
