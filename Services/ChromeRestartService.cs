using GiddhTemplate.Services;

namespace GiddhTemplate.Services
{
    public class ChromeRestartService : BackgroundService
    {
        private readonly ILogger<ChromeRestartService> _logger;
        private readonly TimeSpan _restartTime = new TimeSpan(9, 0, 0); // 9:00 AM

        public ChromeRestartService(ILogger<ChromeRestartService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ChromeRestartService] Chrome restart service started. Will restart Chrome daily at 9:00 AM.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRestart = now.Date.Add(_restartTime);
                    
                    // If 9 AM has already passed today, schedule for tomorrow
                    if (now > nextRestart)
                    {
                        nextRestart = nextRestart.AddDays(1);
                    }
                    
                    var delay = nextRestart - now;
                   _logger.LogInformation("[ChromeRestartService] Next Chrome restart scheduled at {NextRestart} (in {Hours}h {Minutes}m)",
                                          nextRestart.ToString("yyyy-MM-dd HH:mm:ss"), (int)delay.TotalHours, delay.Minutes);

                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation("[ChromeRestartService] Scheduled Chrome restart initiated...");
                    
                    await PdfService.DisposeBrowserAsync();
                    
                    _logger.LogInformation("[ChromeRestartService] Chrome browser restarted successfully. Next restart tomorrow at 9:00 AM.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[ChromeRestartService] Chrome restart service is stopping...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ChromeRestartService] Error during scheduled Chrome restart: {Message}", ex.Message);
                }
            }

            _logger.LogInformation("[ChromeRestartService] Chrome restart service stopped.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[ChromeRestartService] Stopping Chrome restart service and disposing browser...");
            
            try
            {
                await PdfService.DisposeBrowserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChromeRestartService] Error disposing browser during shutdown: {Message}", ex.Message);
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
