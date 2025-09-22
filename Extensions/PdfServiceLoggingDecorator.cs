using GiddhTemplate.Services;
using InvoiceData;
using Serilog;
using System.Diagnostics;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// Decorator for PdfService that adds automatic logging to all method calls
    /// </summary>
    public class PdfServiceLoggingDecorator : PdfService
    {
        private readonly PdfService _innerService;
        private readonly Serilog.ILogger _logger;

        public PdfServiceLoggingDecorator(PdfService innerService)
        {
            _innerService = innerService;
            _logger = Log.Logger;
        }

        public override async Task<byte[]?> GeneratePdfAsync(Root request)
        {
            var stopwatch = Stopwatch.StartNew();
            var methodName = "PdfService.GeneratePdfAsync";
            var parameters = new { 
                CompanyName = request?.Company?.Name,
                TemplateType = request?.TemplateType,
                PdfRename = request?.PdfRename
            };

            _logger.Information("→ Entering {MethodName} with parameters: {@Parameters}", 
                methodName, parameters);

            try
            {
                var result = await _innerService.GeneratePdfAsync(request);
                stopwatch.Stop();

                var resultInfo = new { 
                    Success = result != null,
                    SizeBytes = result?.Length ?? 0
                };

                _logger.Information("← Exiting {MethodName} successfully in {ElapsedMs}ms with result: {@Result}", 
                    methodName, stopwatch.ElapsedMilliseconds, resultInfo);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.Error(ex, "✗ Exception in {MethodName} after {ElapsedMs}ms with parameters: {@Parameters}", 
                    methodName, stopwatch.ElapsedMilliseconds, parameters);
                
                throw;
            }
        }
    }
}
