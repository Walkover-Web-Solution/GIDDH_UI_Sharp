using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using GiddhTemplate.Services;

namespace GiddhTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly MemoryReservationService? _memoryReservationService;

        public DiagnosticsController(IServiceProvider serviceProvider)
        {
            // Try to get the service (it may not be registered if disabled)
            _memoryReservationService = serviceProvider.GetService<MemoryReservationService>();
        }

        [HttpGet("memory")]
        public IActionResult GetMemoryStats()
        {
            var process = Process.GetCurrentProcess();
            var reservationStats = _memoryReservationService?.GetMemoryStats();
            
            var memoryStats = new
            {
                Timestamp = DateTime.UtcNow,
                Memory = new
                {
                    TotalManagedMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                    WorkingSetMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2),
                    VirtualMemoryMB = Math.Round(process.VirtualMemorySize64 / 1024.0 / 1024.0, 2)
                },
                Reservation = reservationStats != null ? new
                {
                    Enabled = reservationStats.IsEnabled,
                    ReservedMemoryMB = Math.Round(reservationStats.ReservedMemoryMB, 2),
                    TotalManagedMemoryMB = Math.Round(reservationStats.TotalManagedMemoryMB, 2)
                } : null,
                GarbageCollection = new
                {
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),
                    TotalCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2)
                },
                Process = new
                {
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    UptimeSeconds = (DateTime.Now - process.StartTime).TotalSeconds
                }
            };

            return Ok(memoryStats);
        }

        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "GiddhTemplate",
                Version = "1.0.0"
            });
        }

        [HttpPost("gc")]
        public IActionResult ForceGarbageCollection()
        {
            var beforeMemory = GC.GetTotalMemory(false);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemory = GC.GetTotalMemory(true);
            var freedMemoryMB = Math.Round((beforeMemory - afterMemory) / 1024.0 / 1024.0, 2);

            return Ok(new
            {
                Message = "Garbage collection completed",
                BeforeMemoryMB = Math.Round(beforeMemory / 1024.0 / 1024.0, 2),
                AfterMemoryMB = Math.Round(afterMemory / 1024.0 / 1024.0, 2),
                FreedMemoryMB = freedMemoryMB,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
