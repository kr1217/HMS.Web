
using HMS.Web.Data;
using HMS.Web.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HMS.Web.DAL
{
    public class FinanceRepository
    {
        private readonly DatabaseHelper _db;

        public FinanceRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public UserShift? GetCurrentShift(string userId)
        {
            const string sql = @"SELECT TOP 1 * FROM UserShifts 
                               WHERE UserId = @UserId AND Status = 'Open' 
                               ORDER BY StartTime DESC";

            var shifts = _db.ExecuteQuery(sql, MapUserShift, new[] { new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId) });
            return shifts.FirstOrDefault();
        }

        public List<UserShift> GetAllShifts()
        {
            const string sql = @"SELECT * FROM UserShifts ORDER BY StartTime DESC";
            return _db.ExecuteQuery(sql, MapUserShift);
        }

        public UserShift StartShift(string userId, decimal startingCash)
        {
            // Close any existing open shifts just in case (shouldn't happen if UI is good, but safety)
            const string closeOld = @"UPDATE UserShifts SET Status = 'Closed', EndTime = GETDATE(), Notes = 'Auto-closed by new shift' 
                                      WHERE UserId = @UserId AND Status = 'Open'";
            _db.ExecuteNonQuery(closeOld, new[] { new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId) });

            const string sql = @"INSERT INTO UserShifts (UserId, StartTime, StartingCash, Status) 
                               OUTPUT INSERTED.* 
                               VALUES (@UserId, GETDATE(), @StartingCash, 'Open')";

            var shifts = _db.ExecuteQuery(sql, MapUserShift, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId),
                new Microsoft.Data.SqlClient.SqlParameter("@StartingCash", startingCash)
            });

            return shifts.Single();
        }

        public decimal GetShiftRevenue(int shiftId)
        {
            const string sql = "SELECT ISNULL(SUM(TotalAmount), 0) FROM Bills WHERE ShiftId = @ShiftId";
            return Convert.ToDecimal(_db.ExecuteScalar(sql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@ShiftId", shiftId)
            }));
        }

        public void CloseShift(int shiftId, decimal actualCash, string? notes)
        {
            // Calculate EndingCash (System Expected) 
            // Sum of all bills processed during this shift
            const string calcSql = "SELECT ISNULL(SUM(TotalAmount), 0) FROM Bills WHERE ShiftId = @ShiftId";
            decimal expectedCash = Convert.ToDecimal(_db.ExecuteScalar(calcSql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@ShiftId", shiftId)
            }));

            // Update shift record
            const string sql = @"UPDATE UserShifts 
                               SET Status = 'Closed', 
                                   EndTime = GETDATE(), 
                                   ActualCash = @ActualCash, 
                                   Notes = @Notes,
                                   EndingCash = @ExpectedCash
                               WHERE ShiftId = @ShiftId";

            _db.ExecuteNonQuery(sql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@ShiftId", shiftId),
                new Microsoft.Data.SqlClient.SqlParameter("@ActualCash", actualCash),
                new Microsoft.Data.SqlClient.SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                new Microsoft.Data.SqlClient.SqlParameter("@ExpectedCash", expectedCash)
            });
        }

        public List<UserShift> GetShiftsRecursively(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"SELECT * FROM UserShifts 
                               WHERE StartTime BETWEEN @From AND @To 
                               ORDER BY StartTime DESC";

            return _db.ExecuteQuery(sql, MapUserShift, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@From", fromDate),
                new Microsoft.Data.SqlClient.SqlParameter("@To", toDate)
            });
        }

        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();

            // 1. Revenue
            var revSql = "SELECT SUM(TotalAmount) FROM Bills WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
            var revenue = _db.ExecuteScalar(revSql);
            stats.TodayRevenue = (revenue != null && revenue != DBNull.Value) ? Convert.ToDecimal(revenue) : 0;

            // 2. Bed Occupancy
            var bedSql = "SELECT COUNT(*) as Total, SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END) as Occupied FROM Beds WHERE IsActive = 1";
            var bedDataTable = _db.ExecuteDataTable(bedSql);
            if (bedDataTable != null && bedDataTable.Rows.Count > 0)
            {
                stats.TotalBeds = Convert.ToInt32(bedDataTable.Rows[0]["Total"]);
                var occupied = bedDataTable.Rows[0]["Occupied"];
                stats.OccupiedBeds = (occupied != null && occupied != DBNull.Value) ? Convert.ToInt32(occupied) : 0;
            }

            // 3. Staff On Shift
            var staffSql = "SELECT COUNT(*) FROM UserShifts WHERE Status = 'Open'";
            var staffCount = _db.ExecuteScalar(staffSql);
            stats.StaffOnShift = (staffCount != null && staffCount != DBNull.Value) ? Convert.ToInt32(staffCount) : 0;

            // 4. Surgeries Today
            var surgerySql = "SELECT COUNT(*) FROM PatientOperations WHERE CAST(ScheduledDate AS DATE) = CAST(GETDATE() AS DATE)";
            var surgeryCount = _db.ExecuteScalar(surgerySql);
            stats.SurgeriesToday = (surgeryCount != null && surgeryCount != DBNull.Value) ? Convert.ToInt32(surgeryCount) : 0;

            return stats;
        }

        public decimal CalculateDoctorSettlement(int doctorId, DateTime periodStart, DateTime periodEnd)
        {
            // Get doctor's commission rate
            const string doctorSql = "SELECT CommissionRate FROM Doctors WHERE DoctorId = @DoctorId";
            var commissionRate = _db.ExecuteScalar(doctorSql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@DoctorId", doctorId)
            });

            if (commissionRate == null || commissionRate == DBNull.Value)
                return 0;

            decimal rate = Convert.ToDecimal(commissionRate) / 100; // Convert percentage to decimal

            // Sum all completed appointments in the period
            const string appointmentSql = @"
                SELECT ISNULL(SUM(d.ConsultationFee), 0) 
                FROM Appointments a
                INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
                WHERE a.DoctorId = @DoctorId 
                AND a.Status = 'Completed'
                AND a.AppointmentDate BETWEEN @PeriodStart AND @PeriodEnd";

            var totalFees = Convert.ToDecimal(_db.ExecuteScalar(appointmentSql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@DoctorId", doctorId),
                new Microsoft.Data.SqlClient.SqlParameter("@PeriodStart", periodStart),
                new Microsoft.Data.SqlClient.SqlParameter("@PeriodEnd", periodEnd)
            }));

            return totalFees * rate;
        }

        public void ProcessDoctorPayment(DoctorPayment payment)
        {
            const string sql = @"INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes)
                                VALUES (@DoctorId, @Amount, @PaymentDate, @PeriodStart, @PeriodEnd, @Status, @Notes)";

            _db.ExecuteNonQuery(sql, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@DoctorId", payment.DoctorId),
                new Microsoft.Data.SqlClient.SqlParameter("@Amount", payment.Amount),
                new Microsoft.Data.SqlClient.SqlParameter("@PaymentDate", payment.PaymentDate),
                new Microsoft.Data.SqlClient.SqlParameter("@PeriodStart", payment.PeriodStart),
                new Microsoft.Data.SqlClient.SqlParameter("@PeriodEnd", payment.PeriodEnd),
                new Microsoft.Data.SqlClient.SqlParameter("@Status", payment.Status),
                new Microsoft.Data.SqlClient.SqlParameter("@Notes", (object?)payment.Notes ?? DBNull.Value)
            });
        }

        public List<DoctorPayment> GetDoctorPayments(int doctorId)
        {
            const string sql = @"SELECT p.*, d.FullName as DoctorName 
                                FROM DoctorPayments p
                                INNER JOIN Doctors d ON p.DoctorId = d.DoctorId
                                WHERE p.DoctorId = @DoctorId
                                ORDER BY p.PaymentDate DESC";

            return _db.ExecuteQuery(sql, MapDoctorPayment, new[] {
                new Microsoft.Data.SqlClient.SqlParameter("@DoctorId", doctorId)
            });
        }

        private DoctorPayment MapDoctorPayment(System.Data.DataRow r)
        {
            return new DoctorPayment
            {
                PaymentId = (int)r["PaymentId"],
                DoctorId = (int)r["DoctorId"],
                Amount = (decimal)r["Amount"],
                PaymentDate = (DateTime)r["PaymentDate"],
                PeriodStart = (DateTime)r["PeriodStart"],
                PeriodEnd = (DateTime)r["PeriodEnd"],
                Status = r["Status"].ToString()!,
                Notes = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : null,
                DoctorName = r["DoctorName"]?.ToString()
            };
        }

        private UserShift MapUserShift(System.Data.DataRow r)
        {
            return new UserShift
            {
                ShiftId = (int)r["ShiftId"],
                UserId = r["UserId"].ToString()!,
                StartTime = (DateTime)r["StartTime"],
                EndTime = r["EndTime"] != DBNull.Value ? (DateTime?)r["EndTime"] : null,
                StartingCash = (decimal)r["StartingCash"],
                EndingCash = r["EndingCash"] != DBNull.Value ? (decimal?)r["EndingCash"] : null,
                ActualCash = r["ActualCash"] != DBNull.Value ? (decimal?)r["ActualCash"] : null,
                Status = r["Status"].ToString()!,
                Notes = r["Notes"]?.ToString()
            };
        }
    }
}
