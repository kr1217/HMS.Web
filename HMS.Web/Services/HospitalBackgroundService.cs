/*
 * FILE: HospitalBackgroundService.cs
 * PURPOSE: Background service for automated maintenance tasks: Daily Room Accrual and Revenue Rollups.
 * COMMUNICATES WITH: DatabaseHelper
 */
using HMS.Web.DAL;
using HMS.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HMS.Web.Services
{
    /// <summary>
    /// Background service for automated maintenance tasks: Daily Room Accrual and Revenue Rollups.
    /// OPTIMIZATION: [Background Job Scaling] Moves heavy I/O tasks out of the web request path.
    /// WHY: Heavy updates (like updating 500 admissions at once) would cause UI lag if done during a page load.
    /// HOW: Runs on a low-priority background thread during off-peak hours (midnight).
    /// </summary>
    public class HospitalBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HospitalBackgroundService> _logger;

        public HospitalBackgroundService(IServiceProvider serviceProvider, ILogger<HospitalBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hospital Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // Run daily at midnight
                if (now.Hour == 0 && now.Minute == 0)
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var db = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();
                            await PerformDailyAccruals(db);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during daily accrual process.");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task PerformDailyAccruals(DatabaseHelper db)
        {
            _logger.LogInformation("Starting Daily Room Rent Accruals...");

            // Logic: For every active admission, add a daily room rent item to their current bill
            // This is done server-side to avoid C# loop overhead if possible, but for clarity we use a stored proc logic or SQL script
            string sql = @"
                INSERT INTO BillItems (BillId, Description, Amount, Category)
                SELECT b.BillId, 
                       'Daily Room Rent - ' + CAST(CAST(GETDATE() AS DATE) AS NVARCHAR), 
                       rt.DailyRate, 
                       'Room'
                FROM Admissions a
                JOIN Bills b ON a.AdmissionId = b.AdmissionId
                JOIN Beds bed ON a.BedId = bed.BedId
                JOIN Rooms r ON bed.RoomId = r.RoomId
                JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                WHERE a.Status = 'Admitted' 
                  AND b.Status = 'Pending'";

            int affected = await db.ExecuteNonQueryAsync(sql);
            _logger.LogInformation($"Accrual complete. {affected} bills updated with room rent.");
        }
    }
}
