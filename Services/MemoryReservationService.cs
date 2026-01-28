using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// Service to pre-allocate and reserve memory at application startup.
    /// This simulates JVM's -Xms behavior by keeping a minimum amount of memory allocated.
    /// </summary>
    public class MemoryReservationService : IHostedService
    {
        private readonly ILogger<MemoryReservationService> _logger;
        private readonly long _reservedMemoryBytes;
        private byte[]? _reservedMemory;
        private readonly bool _enabled;

        public MemoryReservationService(ILogger<MemoryReservationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            // Read configuration (default: 1GB = 1,073,741,824 bytes)
            _reservedMemoryBytes = configuration.GetValue<long>("MemoryReservation:ReservedMemoryBytes", 1073741824);
            _enabled = configuration.GetValue<bool>("MemoryReservation:Enabled", false);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Memory reservation is disabled");
                return Task.CompletedTask;
            }

            try
            {
                _logger.LogInformation("Reserving {MemoryMB} MB of memory at startup...", 
                    _reservedMemoryBytes / 1024.0 / 1024.0);

                // Allocate memory array
                _reservedMemory = new byte[_reservedMemoryBytes];
                
                // Touch the memory to ensure it's actually allocated
                // Write pattern to prevent compiler optimization
                for (long i = 0; i < _reservedMemoryBytes; i += 4096) // Touch every 4KB page
                {
                    _reservedMemory[i] = (byte)(i % 256);
                }

                // Pin the memory to prevent GC from collecting it
                GC.AddMemoryPressure(_reservedMemoryBytes);
                
                // Force GC to recognize the allocation
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();

                var totalMemory = GC.GetTotalMemory(false);
                _logger.LogInformation(
                    "Memory reservation complete. Reserved: {ReservedMB} MB, Total managed memory: {TotalMB} MB",
                    _reservedMemoryBytes / 1024.0 / 1024.0,
                    totalMemory / 1024.0 / 1024.0);

                return Task.CompletedTask;
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, 
                    "Failed to reserve {MemoryMB} MB - not enough memory available. Consider reducing reservation size.",
                    _reservedMemoryBytes / 1024.0 / 1024.0);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory reservation");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_reservedMemory != null && _enabled)
            {
                _logger.LogInformation("Releasing reserved memory...");
                
                // Remove memory pressure
                GC.RemoveMemoryPressure(_reservedMemoryBytes);
                
                // Release the array
                _reservedMemory = null;
                
                // Force GC to clean up
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                _logger.LogInformation("Reserved memory released");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get current memory statistics
        /// </summary>
        public MemoryStats GetMemoryStats()
        {
            return new MemoryStats
            {
                ReservedMemoryBytes = _enabled ? _reservedMemoryBytes : 0,
                ReservedMemoryMB = _enabled ? _reservedMemoryBytes / 1024.0 / 1024.0 : 0,
                TotalManagedMemoryBytes = GC.GetTotalMemory(false),
                TotalManagedMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                IsEnabled = _enabled
            };
        }
    }

    public class MemoryStats
    {
        public long ReservedMemoryBytes { get; set; }
        public double ReservedMemoryMB { get; set; }
        public long TotalManagedMemoryBytes { get; set; }
        public double TotalManagedMemoryMB { get; set; }
        public bool IsEnabled { get; set; }
    }
}
