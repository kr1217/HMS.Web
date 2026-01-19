/*
 * FILE: AnalyticsRepository.cs
 * PURPOSE: Provides high-level aggregated data for executive decision making.
 * COMMUNICATES WITH: DatabaseHelper
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HMS.Web.DAL
{
    public class AnalyticsRepository
    {
        private readonly DatabaseHelper _db;
        public AnalyticsRepository(DatabaseHelper db) { _db = db; }

        public async Task<decimal> GetOccupancyRateAsync()
        {
            string query = @"SELECT 
                             CAST(SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END) AS DECIMAL(10,2)) / 
                             CAST(NULLIF(COUNT(*), 0) AS DECIMAL(10,2)) * 100
                             FROM Beds WHERE Status != 'Maintenance'";
            var result = await _db.ExecuteScalarAsync(query);
            return Convert.ToDecimal(result ?? 0);
        }

        public async Task<decimal> GetNetRevenueAsync(int month, int year)
        {
            // Total Payments Received - Total Commissions Paid
            // Note: Commissions Paid tracked in DoctorLedger where Status = 'Paid'

            string revenueSql = @"SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE MONTH(PaymentDate) = @M AND YEAR(PaymentDate) = @Y";
            string costSql = @"SELECT ISNULL(SUM(Amount), 0) FROM DoctorLedger WHERE Status = 'Paid' AND MONTH(SettledDate) = @M AND YEAR(SettledDate) = @Y";

            decimal revenue = await _db.ExecuteScalarAsync<decimal>(revenueSql, new[] { new SqlParameter("@M", month), new SqlParameter("@Y", year) });
            decimal cost = await _db.ExecuteScalarAsync<decimal>(costSql, new[] { new SqlParameter("@M", month), new SqlParameter("@Y", year) });

            return revenue - cost;
        }

        public async Task<List<DailyRevenue>> GetRevenueTrendAsync(int days = 30)
        {
            string query = @"SELECT CAST(PaymentDate as DATE) as Date, SUM(Amount) as Amount
                             FROM Payments
                             WHERE PaymentDate >= DATEADD(day, -@Days, GETDATE())
                             GROUP BY CAST(PaymentDate as DATE)
                             ORDER BY CAST(PaymentDate as DATE)";

            return await _db.ExecuteQueryAsync(query, reader => new DailyRevenue
            {
                Date = reader.GetDateTime(0),
                Amount = reader.GetDecimal(1)
            }, new[] { new SqlParameter("@Days", days) });
        }

        public async Task<int> GetActiveDoctorCountAsync()
        {
            return await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Doctors WHERE IsActive = 1");
        }

        public async Task<int> GetTotalAdmissionsTodayAsync()
        {
            return await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Admissions WHERE CAST(AdmissionDate AS DATE) = CAST(GETDATE() AS DATE)");
        }

        public async Task<decimal> GetPendingCollectionTotalAsync()
        {
            return await _db.ExecuteScalarAsync<decimal>("SELECT ISNULL(SUM(DueAmount), 0) FROM Bills WHERE Status != 'Paid'");
        }

        // --- Department-Level Cost & Performance Control (Phase 4.3) ---

        public async Task<List<DepartmentRevenue>> GetRevenueByDepartmentAsync()
        {
            string query = @"SELECT w.WardName as Department, SUM(p.Amount) as Revenue
                             FROM Payments p
                             JOIN Bills b ON p.BillId = b.BillId
                             JOIN Admissions a ON b.AdmissionId = a.AdmissionId
                             JOIN Beds bd ON a.BedId = bd.BedId
                             JOIN Rooms r ON bd.RoomId = r.RoomId
                             JOIN Wards w ON r.WardId = w.WardId
                             GROUP BY w.WardName";

            return await _db.ExecuteQueryAsync(query, reader => new DepartmentRevenue
            {
                Department = reader.GetString(0),
                Revenue = reader.GetDecimal(1)
            });
        }

        public async Task<List<WardOccupancy>> GetBedOccupancyByWardAsync()
        {
            string query = @"SELECT w.WardName, 
                             COUNT(*) as TotalBeds,
                             SUM(CASE WHEN b.Status = 'Occupied' THEN 1 ELSE 0 END) as OccupiedBeds
                             FROM Wards w
                             JOIN Rooms r ON w.WardId = r.WardId
                             JOIN Beds b ON r.RoomId = b.RoomId
                             WHERE b.IsActive = 1
                             GROUP BY w.WardName";

            return await _db.ExecuteQueryAsync(query, reader => new WardOccupancy
            {
                WardName = reader.GetString(0),
                TotalBeds = reader.GetInt32(1),
                OccupiedBeds = reader.GetInt32(2)
            });
        }

        public async Task<List<DoctorProductivity>> GetDoctorProductivityAsync()
        {
            string query = @"SELECT d.FullName, 
                             COUNT(DISTINCT a.AppointmentId) as ApptCount,
                             COUNT(DISTINCT op.OperationId) as OpCount,
                             ISNULL(SUM(dl.Amount), 0) as TotalEarned
                             FROM Doctors d
                             LEFT JOIN Appointments a ON d.DoctorId = a.DoctorId AND a.Status = 'Completed'
                             LEFT JOIN PatientOperations op ON d.DoctorId = op.DoctorId AND op.Status = 'Completed'
                             LEFT JOIN DoctorLedger dl ON d.DoctorId = dl.DoctorId AND dl.Status = 'Paid'
                             GROUP BY d.FullName";

            return await _db.ExecuteQueryAsync(query, reader => new DoctorProductivity
            {
                DoctorName = reader.GetString(0),
                AppointmentCount = reader.GetInt32(1),
                OperationCount = reader.GetInt32(2),
                TotalEarned = reader.GetDecimal(3)
            });
        }

        public async Task<List<OTLoad>> GetSurgeryLoadPerOTAsync()
        {
            string query = @"SELECT ot.TheaterName, COUNT(*) as SurgeryCount
                             FROM OperationTheaters ot
                             JOIN PatientOperations po ON ot.TheaterId = po.TheaterId
                             WHERE po.Status != 'Cancelled'
                             GROUP BY ot.TheaterName";

            return await _db.ExecuteQueryAsync(query, reader => new OTLoad
            {
                TheaterName = reader.GetString(0),
                SurgeryCount = reader.GetInt32(1)
            });
        }
    }

    public class DepartmentRevenue { public string Department { get; set; } = ""; public decimal Revenue { get; set; } }
    public class WardOccupancy { public string WardName { get; set; } = ""; public int TotalBeds { get; set; } public int OccupiedBeds { get; set; } public decimal OccupancyRate => TotalBeds == 0 ? 0 : (decimal)OccupiedBeds / TotalBeds * 100; }
    public class DoctorProductivity { public string DoctorName { get; set; } = ""; public int AppointmentCount { get; set; } public int OperationCount { get; set; } public decimal TotalEarned { get; set; } }
    public class OTLoad { public string TheaterName { get; set; } = ""; public int SurgeryCount { get; set; } }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
