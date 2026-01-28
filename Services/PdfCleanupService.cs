using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GiddhTemplate.Services
{
    public class PdfCleanupService : BackgroundService
    {
        private readonly ILogger<PdfCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _fileMaxAge = TimeSpan.FromHours(2);

        public PdfCleanupService(ILogger<PdfCleanupService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PDF Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    await CleanupOldPdfFilesAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("PDF Cleanup Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during PDF cleanup");
                }
            }
        }

        private async Task CleanupOldPdfFilesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "GiddhPdfs");
                    
                    if (!Directory.Exists(tempPath))
                    {
                        return;
                    }

                    var files = Directory.GetFiles(tempPath, "*.pdf");
                    var cutoffTime = DateTime.Now - _fileMaxAge;
                    int deletedCount = 0;
                    long freedBytes = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            
                            if (fileInfo.LastAccessTime < cutoffTime)
                            {
                                long fileSize = fileInfo.Length;
                                File.Delete(file);
                                deletedCount++;
                                freedBytes += fileSize;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete temp PDF file: {FilePath}", file);
                        }
                    }

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation(
                            "Cleaned up {Count} temporary PDF files, freed {SizeMB:F2} MB",
                            deletedCount,
                            freedBytes / 1024.0 / 1024.0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accessing temp PDF directory");
                }
            });
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PDF Cleanup Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
