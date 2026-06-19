using GiddhTemplate.Services;

namespace GiddhTemplate.Services
{
    public class ChromeRestartService : BackgroundService
    {
        private readonly ILogger<ChromeRestartService> _logger;
        private readonly PdfService _pdfService;
        private readonly TimeSpan _restartTime = new TimeSpan(9, 0, 0); // 9:00 AM
        private readonly TimeZoneInfo _istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        public ChromeRestartService(ILogger<ChromeRestartService> logger, PdfService pdfService)
        {
            _logger = logger;
            _pdfService = pdfService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ChromeRestartService] Chrome restart service started. Will restart Chrome daily at 9:00 AM IST.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowUtc = DateTime.UtcNow;
                    var nowIst = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _istTimeZone);
                    var nextRestartIst = nowIst.Date.Add(_restartTime);
                    
                    // If 9 AM IST has already passed today, schedule for tomorrow
                    if (nowIst > nextRestartIst)
                    {
                        nextRestartIst = nextRestartIst.AddDays(1);
                    }
                    
                    var delay = nextRestartIst - nowIst;
                   _logger.LogInformation("[ChromeRestartService] Next Chrome restart scheduled at {NextRestart} IST (in {Hours}h {Minutes}m)",
                                          nextRestartIst.ToString("yyyy-MM-dd HH:mm:ss"), (int)delay.TotalHours, delay.Minutes);

                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation("[ChromeRestartService] Scheduled Chrome restart initiated...");

                    // Step 1: Capture PID of existing Chrome before anything else
                    int? oldPid = null;
                    try
                    {
                        var pidProcess = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "/bin/bash",
                                Arguments = "-c \"pgrep -f 'google-chrome.*headless' | head -n 1\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        pidProcess.Start();
                        var output = await pidProcess.StandardOutput.ReadToEndAsync();
                        await pidProcess.WaitForExitAsync();
                        if (int.TryParse(output.Trim(), out var parsedPid) && parsedPid > 0)
                        {
                            oldPid = parsedPid;
                            _logger.LogInformation("[ChromeRestartService] Captured old Chrome PID: {Pid}", oldPid);
                        }
                    }
                    catch (Exception pidEx)
                    {
                        _logger.LogWarning(pidEx, "[ChromeRestartService] Could not capture old Chrome PID.");
                    }
                    
                    // Step 2: Dispose old browser connection
                    await PdfService.DisposeBrowserAsync();

                    // Step 3: If we captured a PID, wait for the old process to die before launching new one
                    if (oldPid.HasValue)
                    {
                        _logger.LogInformation("[ChromeRestartService] Waiting for old Chrome (PID {Pid}) to terminate...", oldPid.Value);
                        var killTimeout = DateTime.UtcNow.AddSeconds(15);
                        while (System.Diagnostics.Process.GetProcesses().Any(p => p.Id == oldPid.Value && !p.HasExited))
                        {
                            if (DateTime.UtcNow > killTimeout)
                            {
                                try
                                {
                                    var proc = System.Diagnostics.Process.GetProcessById(oldPid.Value);
                                    proc.Kill();
                                    _logger.LogWarning("[ChromeRestartService] Forced kill on old Chrome PID {Pid}.", oldPid.Value);
                                }
                                catch { }
                                break;
                            }
                            await Task.Delay(200, stoppingToken);
                        }
                    }
                    else
                    {
                        // Fallback: pkill if we couldn't get a PID
                        try
                        {
                            var killProcess = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "/bin/bash",
                                    Arguments = "-c \"pkill -9 -f 'google-chrome.*headless'\"",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };
                            killProcess.Start();
                            await killProcess.WaitForExitAsync();
                            _logger.LogInformation("[ChromeRestartService] Old Chrome processes killed via pkill.");
                        }
                        catch { }
                    }
                    
                    // Step 4: Launch new Chrome instance
                    _logger.LogInformation("[ChromeRestartService] Launching new Chrome instance...");
                    await _pdfService.GetBrowserAsync();
                    _logger.LogInformation("[ChromeRestartService] New Chrome instance launched successfully.");
                    
                    _logger.LogInformation("[ChromeRestartService] Chrome browser restarted successfully. Next restart tomorrow at 9:00 AM IST.");
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
