/*
 * FILE: FinanceRepository.cs
 * PURPOSE: Handles financial reporting and tracking.
 * COMMUNICATES WITH: DatabaseHelper, Admin/Settlements.razor, Teller/TellerDashboard.razor
 */
using HMS.Web.Data;
using HMS.Web.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing financial operations, user shifts, and revenue tracking.
    /// OPTIMIZATION: [Audit Trails] Every cash movement is tied to a ShiftID to enable strict end-of-day reconciliation.
    /// </summary>
    public class FinanceRepository
    {
        private readonly DatabaseHelper _db;

        public FinanceRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves the currently open shift for a specific user.
        /// </summary>
        public async Task<UserShift?> GetCurrentShiftAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;
                const string sql = @"SELECT TOP 1 us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                                   FROM UserShifts us
                                   LEFT JOIN Staff st ON us.UserId = st.UserId
                                   WHERE us.UserId = @UserId AND us.Status = 'Open' 
                                   ORDER BY us.StartTime DESC";

                var shifts = await _db.ExecuteQueryAsync(sql, MapUserShift, new[] { new SqlParameter("@UserId", userId) });
                return shifts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving current shift for user {userId}: {ex.Message}", ex);
            }
        }

        public UserShift? GetCurrentShift(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            const string sql = @"SELECT TOP 1 us.*, st.FullName as TellerName, st.StaffId as EmployeeId FROM UserShifts us LEFT JOIN Staff st ON us.UserId = st.UserId WHERE us.UserId = @UserId AND us.Status = 'Open' ORDER BY us.StartTime DESC";
            return _db.ExecuteQuery(sql, MapUserShift, new[] { new SqlParameter("@UserId", userId) }).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all historical and active user shifts.
        /// </summary>
        public async Task<List<UserShift>> GetAllShiftsAsync()
        {
            try
            {
                const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                                   FROM UserShifts us
                                   LEFT JOIN Staff st ON us.UserId = st.UserId
                                   ORDER BY us.StartTime DESC";
                return await _db.ExecuteQueryAsync(sql, MapUserShift);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all shifts: {ex.Message}", ex);
            }
        }

        public List<UserShift> GetAllShifts()
        {
            const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId FROM UserShifts us LEFT JOIN Staff st ON us.UserId = st.UserId ORDER BY us.StartTime DESC";
            return _db.ExecuteQuery(sql, MapUserShift);
        }

        /// <summary>
        /// Asynchronously starts a new shift for a user, closing any previously open ones.
        /// </summary>
        public async Task<UserShift> StartShiftAsync(string userId, decimal startingCash)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID is required to start a shift.");

                // Atomic Start: We ensure that the user doesn't have multiple open shifts.
                // This maintains the integrity of the cash drawer audit.
                const string closeOld = @"UPDATE UserShifts SET Status = 'Closed', EndTime = GETDATE(), Notes = 'Auto-closed by new shift' 
                                          WHERE UserId = @UserId AND Status = 'Open'";
                await _db.ExecuteNonQueryAsync(closeOld, new[] { new SqlParameter("@UserId", userId) });

                const string sql = @"INSERT INTO UserShifts (UserId, StartTime, StartingCash, Status) 
                                   OUTPUT INSERTED.* 
                                   VALUES (@UserId, GETDATE(), @StartingCash, 'Open')";

                var shifts = await _db.ExecuteQueryAsync(sql, MapUserShift, new[] {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@StartingCash", startingCash)
                });

                return shifts.Single();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start shift for user {userId}: {ex.Message}", ex);
            }
        }

        public UserShift StartShift(string userId, decimal startingCash)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID is required to start a shift.");
            const string closeOld = @"UPDATE UserShifts SET Status = 'Closed', EndTime = GETDATE(), Notes = 'Auto-closed by new shift' WHERE UserId = @UserId AND Status = 'Open'";
            _db.ExecuteNonQuery(closeOld, new[] { new SqlParameter("@UserId", userId) });
            const string sql = @"INSERT INTO UserShifts (UserId, StartTime, StartingCash, Status) OUTPUT INSERTED.* VALUES (@UserId, GETDATE(), @StartingCash, 'Open')";
            return _db.ExecuteQuery(sql, MapUserShift, new[] { new SqlParameter("@UserId", userId), new SqlParameter("@StartingCash", startingCash) }).Single();
        }

        /// <summary>
        /// Gets the total revenue collected during a specific shift.
        /// </summary>
        public async Task<decimal> GetShiftRevenueAsync(int shiftId)
        {
            try
            {
                if (shiftId <= 0) return 0;
                const string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId";
                var result = await _db.ExecuteScalarAsync(sql, new[] { new SqlParameter("@ShiftId", shiftId) });
                return Convert.ToDecimal(result ?? 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating revenue for shift {shiftId}: {ex.Message}", ex);
            }
        }

        public decimal GetShiftRevenue(int shiftId)
        {
            if (shiftId <= 0) return 0;
            const string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId";
            return Convert.ToDecimal(_db.ExecuteScalar(sql, new[] { new SqlParameter("@ShiftId", shiftId) }) ?? 0);
        }

        /// <summary>
        /// Asynchronously closes a shift, performing final cash reconciliation.
        /// </summary>
        public async Task CloseShiftAsync(int shiftId, decimal actualCash, string? notes)
        {
            try
            {
                if (shiftId <= 0) throw new ArgumentException("Invalid Shift ID.");

                // Reconciliation Logic:
                // We calculate the expected amount based on the starting balance and the recorded payments.
                // Any discrepancy between this and 'actualCash' is logged for administrative audit.
                const string calcSql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId AND PaymentMethod = 'Cash'";
                decimal collectedCash = Convert.ToDecimal(await _db.ExecuteScalarAsync(calcSql, new[] {
                    new SqlParameter("@ShiftId", shiftId)
                }) ?? 0);

                const string startSql = "SELECT StartingCash FROM UserShifts WHERE ShiftId = @ShiftId";
                var startResult = await _db.ExecuteScalarAsync(startSql, new[] { new SqlParameter("@ShiftId", shiftId) });
                decimal startingCash = (startResult != null && startResult != DBNull.Value) ? Convert.ToDecimal(startResult) : 0;

                decimal expectedCash = startingCash + collectedCash;

                // Update shift record
                const string sql = @"UPDATE UserShifts 
                                   SET Status = 'Closed', 
                                       EndTime = GETDATE(), 
                                       ActualCash = @ActualCash, 
                                       Notes = @Notes,
                                       EndingCash = @ExpectedCash
                                   WHERE ShiftId = @ShiftId";

                await _db.ExecuteNonQueryAsync(sql, new[] {
                    new SqlParameter("@ShiftId", shiftId),
                    new SqlParameter("@ActualCash", actualCash),
                    new SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                    new SqlParameter("@ExpectedCash", expectedCash)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to close shift {shiftId}: {ex.Message}", ex);
            }
        }

        public void CloseShift(int shiftId, decimal actualCash, string? notes)
        {
            if (shiftId <= 0) throw new ArgumentException("Invalid Shift ID.");
            const string calcSql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId AND PaymentMethod = 'Cash'";
            decimal collectedCash = Convert.ToDecimal(_db.ExecuteScalar(calcSql, new[] { new SqlParameter("@ShiftId", shiftId) }) ?? 0);
            const string startSql = "SELECT StartingCash FROM UserShifts WHERE ShiftId = @ShiftId";
            var startResult = _db.ExecuteScalar(startSql, new[] { new SqlParameter("@ShiftId", shiftId) });
            decimal startingCash = (startResult != null && startResult != DBNull.Value) ? Convert.ToDecimal(startResult) : 0;
            decimal expectedCash = startingCash + collectedCash;
            const string sql = @"UPDATE UserShifts SET Status = 'Closed', EndTime = GETDATE(), ActualCash = @ActualCash, Notes = @Notes, EndingCash = @ExpectedCash WHERE ShiftId = @ShiftId";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@ShiftId", shiftId), new SqlParameter("@ActualCash", actualCash), new SqlParameter("@Notes", (object?)notes ?? DBNull.Value), new SqlParameter("@ExpectedCash", expectedCash) });
        }

        /// <summary>
        /// Retrieves user shifts within a specific date range.
        /// </summary>
        public async Task<List<UserShift>> GetShiftsRecursivelyAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                                   FROM UserShifts us
                                   LEFT JOIN Staff st ON us.UserId = st.UserId
                                   WHERE us.StartTime BETWEEN @From AND @To 
                                   ORDER BY us.StartTime DESC";

                return await _db.ExecuteQueryAsync(sql, MapUserShift, new[] {
                    new SqlParameter("@From", fromDate),
                    new SqlParameter("@To", toDate)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving shifts between {fromDate:d} and {toDate:d}: {ex.Message}", ex);
            }
        }

        public List<UserShift> GetShiftsRecursively(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId FROM UserShifts us LEFT JOIN Staff st ON us.UserId = st.UserId WHERE us.StartTime BETWEEN @From AND @To ORDER BY us.StartTime DESC";
            return _db.ExecuteQuery(sql, MapUserShift, new[] { new SqlParameter("@From", fromDate), new SqlParameter("@To", toDate) });
        }

        /// <summary>
        /// Retrieves high-level financial and operational statistics for the dashboard.
        /// </summary>
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var stats = new DashboardStats();

                // 1. Revenue (Actual Collected Cash/Card today)
                var revSql = "SELECT SUM(Amount) FROM Payments WHERE CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)";
                var revenue = await _db.ExecuteScalarAsync(revSql);
                stats.TodayRevenue = (revenue != null && revenue != DBNull.Value) ? Convert.ToDecimal(revenue) : 0;

                // 2. Bed Occupancy
                var bedSql = "SELECT COUNT(*) as Total, SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END) as Occupied FROM Beds WHERE IsActive = 1";
                var bedStats = await _db.ExecuteQueryAsync(bedSql, reader => new
                {
                    Total = reader.GetInt32(0),
                    Occupied = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                });

                if (bedStats.Any())
                {
                    stats.TotalBeds = bedStats[0].Total;
                    stats.OccupiedBeds = bedStats[0].Occupied;
                }

                // 3. Staff On Shift
                var staffSql = "SELECT COUNT(*) FROM UserShifts WHERE Status = 'Open'";
                var staffCount = await _db.ExecuteScalarAsync(staffSql);
                stats.StaffOnShift = (staffCount != null && staffCount != DBNull.Value) ? Convert.ToInt32(staffCount) : 0;

                // 4. Surgeries Today
                var surgerySql = "SELECT COUNT(*) FROM PatientOperations WHERE CAST(ScheduledDate AS DATE) = CAST(GETDATE() AS DATE)";
                var surgeryCount = await _db.ExecuteScalarAsync(surgerySql);
                stats.SurgeriesToday = (surgeryCount != null && surgeryCount != DBNull.Value) ? Convert.ToInt32(surgeryCount) : 0;

                return stats;
            }
            catch
            {
                return new DashboardStats();
            }
        }

        public DashboardStats GetDashboardStats()
        {
            try
            {
                var stats = new DashboardStats();
                var revSql = "SELECT SUM(Amount) FROM Payments WHERE CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)";
                var revenue = _db.ExecuteScalar(revSql);
                stats.TodayRevenue = (revenue != null && revenue != DBNull.Value) ? Convert.ToDecimal(revenue) : 0;
                var bedSql = "SELECT COUNT(*) as Total, SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END) as Occupied FROM Beds WHERE IsActive = 1";
                var bedStats = _db.ExecuteQuery(bedSql, reader => new
                {
                    Total = reader.GetInt32(0),
                    Occupied = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                });

                if (bedStats.Any())
                {
                    stats.TotalBeds = bedStats[0].Total;
                    stats.OccupiedBeds = bedStats[0].Occupied;
                }
                var staffSql = "SELECT COUNT(*) FROM UserShifts WHERE Status = 'Open'";
                var staffCount = _db.ExecuteScalar(staffSql);
                stats.StaffOnShift = (staffCount != null && staffCount != DBNull.Value) ? Convert.ToInt32(staffCount) : 0;
                var surgerySql = "SELECT COUNT(*) FROM PatientOperations WHERE CAST(ScheduledDate AS DATE) = CAST(GETDATE() AS DATE)";
                var surgeryCount = _db.ExecuteScalar(surgerySql);
                stats.SurgeriesToday = (surgeryCount != null && surgeryCount != DBNull.Value) ? Convert.ToInt32(surgeryCount) : 0;
                return stats;
            }
            catch { return new DashboardStats(); }
        }

        /// <summary>
        /// Calculates the settlement amount for a doctor based on completed appointments and commission rate.
        /// </summary>
        public async Task<decimal> CalculateDoctorSettlementAsync(int doctorId, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                if (doctorId <= 0) return 0;
                const string doctorSql = "SELECT CommissionRate FROM Doctors WHERE DoctorId = @DoctorId";
                var commissionRate = await _db.ExecuteScalarAsync(doctorSql, new[] {
                    new SqlParameter("@DoctorId", doctorId)
                });

                if (commissionRate == null || commissionRate == DBNull.Value)
                    return 0;

                decimal rate = Convert.ToDecimal(commissionRate) / 100;

                // Business Rule: Doctor's take is calculated as a percentage of the consultation fees.
                // Total fees are aggregated for the period and multiplied by the doctor's commission rate.
                const string appointmentSql = @"
                    SELECT ISNULL(SUM(d.ConsultationFee), 0) 
                    FROM Appointments a
                    INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
                    WHERE a.DoctorId = @DoctorId 
                    AND a.Status = 'Completed'
                    AND a.AppointmentDate BETWEEN @PeriodStart AND @PeriodEnd";

                var totalFees = Convert.ToDecimal(await _db.ExecuteScalarAsync(appointmentSql, new[] {
                    new SqlParameter("@DoctorId", doctorId),
                    new SqlParameter("@PeriodStart", periodStart),
                    new SqlParameter("@PeriodEnd", periodEnd)
                }) ?? 0);

                return totalFees * rate;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating settlement for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public decimal CalculateDoctorSettlement(int doctorId, DateTime periodStart, DateTime periodEnd)
        {
            if (doctorId <= 0) return 0;
            const string doctorSql = "SELECT CommissionRate FROM Doctors WHERE DoctorId = @DoctorId";
            var commissionRate = _db.ExecuteScalar(doctorSql, new[] { new SqlParameter("@DoctorId", doctorId) });
            if (commissionRate == null || commissionRate == DBNull.Value) return 0;
            decimal rate = Convert.ToDecimal(commissionRate) / 100;
            const string appointmentSql = @"SELECT ISNULL(SUM(d.ConsultationFee), 0) FROM Appointments a INNER JOIN Doctors d ON a.DoctorId = d.DoctorId WHERE a.DoctorId = @DoctorId AND a.Status = 'Completed' AND a.AppointmentDate BETWEEN @PeriodStart AND @PeriodEnd";
            var totalFees = Convert.ToDecimal(_db.ExecuteScalar(appointmentSql, new[] { new SqlParameter("@DoctorId", doctorId), new SqlParameter("@PeriodStart", periodStart), new SqlParameter("@PeriodEnd", periodEnd) }) ?? 0);
            return totalFees * rate;
        }

        /// <summary>
        /// Asynchronously records a payment made to a doctor.
        /// </summary>
        public async Task ProcessDoctorPaymentAsync(DoctorPayment payment)
        {
            try
            {
                if (payment == null || payment.DoctorId <= 0 || payment.Amount <= 0)
                    throw new ArgumentException("Invalid payment data.");

                const string sql = @"INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes)
                                    VALUES (@DoctorId, @Amount, @PaymentDate, @PeriodStart, @PeriodEnd, @Status, @Notes)";

                await _db.ExecuteNonQueryAsync(sql, new[] {
                    new SqlParameter("@DoctorId", payment.DoctorId),
                    new SqlParameter("@Amount", payment.Amount),
                    new SqlParameter("@PaymentDate", payment.PaymentDate),
                    new SqlParameter("@PeriodStart", payment.PeriodStart),
                    new SqlParameter("@PeriodEnd", payment.PeriodEnd),
                    new SqlParameter("@Status", payment.Status ?? "Paid"),
                    new SqlParameter("@Notes", (object?)payment.Notes ?? DBNull.Value)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process doctor payment: {ex.Message}", ex);
            }
        }

        public void ProcessDoctorPayment(DoctorPayment payment)
        {
            if (payment == null || payment.DoctorId <= 0 || payment.Amount <= 0) throw new ArgumentException("Invalid payment data.");
            const string sql = @"INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes) VALUES (@DoctorId, @Amount, @PaymentDate, @PeriodStart, @PeriodEnd, @Status, @Notes)";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@DoctorId", payment.DoctorId), new SqlParameter("@Amount", payment.Amount), new SqlParameter("@PaymentDate", payment.PaymentDate), new SqlParameter("@PeriodStart", payment.PeriodStart), new SqlParameter("@PeriodEnd", payment.PeriodEnd), new SqlParameter("@Status", payment.Status ?? "Paid"), new SqlParameter("@Notes", (object?)payment.Notes ?? DBNull.Value) });
        }

        /// <summary>
        /// Retrieves payment history for a specific doctor.
        /// </summary>
        public async Task<List<DoctorPayment>> GetDoctorPaymentsAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<DoctorPayment>();
                const string sql = @"SELECT p.*, d.FullName as DoctorName 
                                    FROM DoctorPayments p
                                    INNER JOIN Doctors d ON p.DoctorId = d.DoctorId
                                    WHERE p.DoctorId = @DoctorId
                                    ORDER BY p.PaymentDate DESC";

                return await _db.ExecuteQueryAsync(sql, MapDoctorPayment, new[] {
                    new SqlParameter("@DoctorId", doctorId)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving payments for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<DoctorPayment> GetDoctorPayments(int doctorId)
        {
            if (doctorId <= 0) return new List<DoctorPayment>();
            const string sql = @"SELECT p.*, d.FullName as DoctorName FROM DoctorPayments p INNER JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.DoctorId = @DoctorId ORDER BY p.PaymentDate DESC";
            return _db.ExecuteQuery(sql, MapDoctorPayment, new[] { new SqlParameter("@DoctorId", doctorId) });
        }

        /// <summary>
        /// Retrieves currently pending bills (limited to top 100).
        /// </summary>
        public async Task<List<Bill>> GetPendingBillsAsync()
        {
            try
            {
                const string sql = @"SELECT TOP 100 b.*, p.FullName as PatientName 
                                    FROM Bills b
                                    INNER JOIN Patients p ON b.PatientId = p.PatientId
                                    WHERE b.Status IN ('Pending', 'Partial', 'Unpaid')
                                    ORDER BY b.BillDate DESC";
                return await _db.ExecuteQueryAsync(sql, MapBill);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving pending bills: {ex.Message}", ex);
            }
        }

        public List<Bill> GetPendingBills()
        {
            const string sql = @"SELECT TOP 100 b.*, p.FullName as PatientName FROM Bills b INNER JOIN Patients p ON b.PatientId = p.PatientId WHERE b.Status IN ('Pending', 'Partial', 'Unpaid') ORDER BY b.BillDate DESC";
            return _db.ExecuteQuery(sql, MapBill);
        }

        /// <summary>
        /// Retrieves a paged list of pending bills.
        /// </summary>
        public async Task<List<Bill>> GetPendingBillsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "BillDate DESC" : orderBy;
                string sql = $@"SELECT b.*, p.FullName as PatientName 
                                FROM Bills b
                                INNER JOIN Patients p ON b.PatientId = p.PatientId
                                WHERE b.Status IN ('Pending', 'Partial', 'Unpaid')
                                ORDER BY {orderClause}
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                return await _db.ExecuteQueryAsync(sql, MapBill, new[] {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                });
            }
            catch { return new List<Bill>(); }
        }

        public List<Bill> GetPendingBillsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "BillDate DESC" : orderBy;
            string sql = $@"SELECT b.*, p.FullName as PatientName FROM Bills b INNER JOIN Patients p ON b.PatientId = p.PatientId WHERE b.Status IN ('Pending', 'Partial', 'Unpaid') ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            return _db.ExecuteQuery(sql, MapBill, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
        }

        /// <summary>
        /// Gets the total count of pending bills.
        /// </summary>
        public async Task<int> GetPendingBillsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Bills WHERE Status IN ('Pending', 'Partial', 'Unpaid')");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetPendingBillsCount()
        {
            return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Bills WHERE Status IN ('Pending', 'Partial', 'Unpaid')") ?? 0);
        }

        /// <summary>
        /// Retrieves a single bill record by its ID.
        /// </summary>
        public async Task<Bill?> GetBillByIdAsync(int billId)
        {
            try
            {
                if (billId <= 0) return null;
                const string sql = @"SELECT b.*, p.FullName as PatientName 
                                    FROM Bills b
                                    INNER JOIN Patients p ON b.PatientId = p.PatientId
                                    WHERE b.BillId = @BillId";
                var bills = await _db.ExecuteQueryAsync(sql, MapBill, new[] { new SqlParameter("@BillId", billId) });
                return bills.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bill {billId}: {ex.Message}", ex);
            }
        }

        public Bill? GetBillById(int billId)
        {
            if (billId <= 0) return null;
            const string sql = @"SELECT b.*, p.FullName as PatientName FROM Bills b INNER JOIN Patients p ON b.PatientId = p.PatientId WHERE b.BillId = @BillId";
            return _db.ExecuteQuery(sql, MapBill, new[] { new SqlParameter("@BillId", billId) }).FirstOrDefault();
        }

        /// <summary>
        /// Adds a payment against a bill and processes related business rules (discharge, operations).
        /// </summary>
        /// <summary>
        /// Adds a payment against a bill and processes related business rules (discharge, operations).
        /// OPTIMIZATION: [Process Automation] Triggers downstream business logic (Discharge/Scheduling) automatically upon payment completion.
        /// OPTIMIZATION: [Concurrency Control] Uses transactions to ensure payment recording and bill status updates happen atomically.
        /// </summary>
        public void AddPayment(Payment payment)
        {
            if (payment == null) throw new ArgumentException("Payment data is required.");
            if (payment.Amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.");
            if (payment.BillId == 0) throw new ArgumentException("Invalid Bill ID.");

            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert Payment
                        const string sqlInfo = @"INSERT INTO Payments (BillId, Amount, PaymentMethod, PaymentDate, ReferenceNumber, TellerId, ShiftId, Remarks)
                                                 VALUES (@BillId, @Amount, @Method, GETDATE(), @Ref, @Teller, @ShiftId, @Remarks)";

                        _db.ExecuteNonQuery(sqlInfo, new[] {
                            new SqlParameter("@BillId", payment.BillId),
                            new SqlParameter("@Amount", payment.Amount),
                            new SqlParameter("@Method", payment.PaymentMethod ?? "Cash"),
                            new SqlParameter("@Ref", (object?)payment.ReferenceNumber ?? DBNull.Value),
                            new SqlParameter("@Teller", payment.TellerId),
                            new SqlParameter("@ShiftId", payment.ShiftId),
                            new SqlParameter("@Remarks", (object?)payment.Remarks ?? DBNull.Value)
                        }, transaction);

                        // 2. Update Bill Status
                        const string sumSql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE BillId = @BillId";
                        decimal totalPaid = Convert.ToDecimal(_db.ExecuteScalar(sumSql, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction) ?? 0);

                        const string billSql = "SELECT TotalAmount FROM Bills WHERE BillId = @BillId";
                        decimal totalAmount = Convert.ToDecimal(_db.ExecuteScalar(billSql, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction) ?? 0);

                        decimal due = totalAmount - totalPaid;
                        string status = (due <= 0.01m) ? "Paid" : "Partial";

                        const string updateBill = @"UPDATE Bills SET PaidAmount = @Paid, DueAmount = @Due, Status = @Status WHERE BillId = @BillId";
                        _db.ExecuteNonQuery(updateBill, new[] {
                            new SqlParameter("@Paid", totalPaid),
                            new SqlParameter("@Due", (due < 0 ? 0 : due)),
                            new SqlParameter("@Status", status),
                            new SqlParameter("@BillId", payment.BillId)
                        }, transaction);

                        // Lifecycle Automation:
                        // If a bill is cleared, we trigger the relevant downstream process.
                        // For In-Patients: Automate the discharge process and free the bed.
                        // For Surgeries: Confirm the deposit and update the surgery status to 'Scheduled'.
                        if (status == "Paid")
                        {
                            const string checkAdmission = "SELECT AdmissionId FROM Bills WHERE BillId = @BillId";
                            // ... logic follows ...
                            var admObj = _db.ExecuteScalar(checkAdmission, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction);

                            const string getPatName = "SELECT p.FullName FROM Bills b JOIN Patients p ON b.PatientId = p.PatientId WHERE b.BillId = @BillId";
                            var patName = _db.ExecuteScalar(getPatName, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction)?.ToString() ?? "Patient";

                            if (admObj != null && admObj != DBNull.Value)
                            {
                                int admissionId = Convert.ToInt32(admObj);
                                const string dischargeSql = @"UPDATE Admissions SET Status = 'Discharged', DischargeDate = GETDATE() 
                                                            WHERE AdmissionId = @AdmissionId AND Status != 'Discharged'";
                                _db.ExecuteNonQuery(dischargeSql, new[] { new SqlParameter("@AdmissionId", admissionId) }, transaction);

                                const string updateBed = @"UPDATE Beds SET Status = 'Available' 
                                                         WHERE BedId = (SELECT BedId FROM Admissions WHERE AdmissionId = @AdmissionId)";
                                _db.ExecuteNonQuery(updateBed, new[] { new SqlParameter("@AdmissionId", admissionId) }, transaction);

                                const string notifSql = @"INSERT INTO Notifications (Title, Message, CreatedDate, IsRead, TargetRole) 
                                                          VALUES (@Title, @Msg, GETDATE(), 0, 'Admin')";
                                _db.ExecuteNonQuery(notifSql, new[] {
                                    new SqlParameter("@Title", "Patient Discharged"),
                                    new SqlParameter("@Msg", $"Final payment for {patName} has been received. Patient is officially discharged.")
                                }, transaction);
                            }
                            else
                            {
                                const string updateOp = @"UPDATE PatientOperations 
                                                          SET Status = 'Scheduled' 
                                                          WHERE Status IN ('Pending Deposit', 'Advance Payment Requested') 
                                                          AND PatientId = (SELECT PatientId FROM Bills WHERE BillId = @BillId)";

                                int affected = _db.ExecuteNonQuery(updateOp, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction);

                                if (affected > 0)
                                {
                                    const string notifSql = @"INSERT INTO Notifications (Title, Message, CreatedDate, IsRead, TargetRole) 
                                                              VALUES (@Title, @Msg, GETDATE(), 0, 'OTStaff')";
                                    _db.ExecuteNonQuery(notifSql, new[] {
                                        new SqlParameter("@Title", "Surgery Deposit Confirmed"),
                                        new SqlParameter("@Msg", $"Deposit for {patName} has been processed. Surgery status updated to 'Scheduled'.")
                                    }, transaction);
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Failed to add payment for bill {payment.BillId}: {ex.Message}", ex);
                    }
                }
            }
        }

        // --- Mappings ---

        private DoctorPayment MapDoctorPayment(SqlDataReader r)
        {
            return new DoctorPayment
            {
                PaymentId = r.GetInt32(r.GetOrdinal("PaymentId")),
                DoctorId = r.GetInt32(r.GetOrdinal("DoctorId")),
                Amount = r.GetDecimal(r.GetOrdinal("Amount")),
                PaymentDate = r.GetDateTime(r.GetOrdinal("PaymentDate")),
                PeriodStart = r.GetDateTime(r.GetOrdinal("PeriodStart")),
                PeriodEnd = r.GetDateTime(r.GetOrdinal("PeriodEnd")),
                Status = r["Status"]?.ToString() ?? "Paid",
                Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r["Notes"]?.ToString(),
                DoctorName = r.HasColumn("DoctorName") ? r["DoctorName"]?.ToString() : null
            };
        }

        private UserShift MapUserShift(SqlDataReader r)
        {
            return new UserShift
            {
                ShiftId = r.GetInt32(r.GetOrdinal("ShiftId")),
                UserId = r["UserId"]?.ToString() ?? "",
                StartTime = r.GetDateTime(r.GetOrdinal("StartTime")),
                EndTime = r.IsDBNull(r.GetOrdinal("EndTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("EndTime")),
                StartingCash = r.GetDecimal(r.GetOrdinal("StartingCash")),
                EndingCash = r.IsDBNull(r.GetOrdinal("EndingCash")) ? null : (decimal?)r.GetDecimal(r.GetOrdinal("EndingCash")),
                ActualCash = r.IsDBNull(r.GetOrdinal("ActualCash")) ? null : (decimal?)r.GetDecimal(r.GetOrdinal("ActualCash")),
                Status = r["Status"]?.ToString() ?? "Closed",
                Notes = r["Notes"]?.ToString() ?? "",
                TellerName = r.HasColumn("TellerName") && !r.IsDBNull(r.GetOrdinal("TellerName")) ? r["TellerName"].ToString() : "Unknown",
                EmployeeId = r.HasColumn("EmployeeId") && !r.IsDBNull(r.GetOrdinal("EmployeeId")) ? (int?)r.GetInt32(r.GetOrdinal("EmployeeId")) : null,
            };
        }

        private Bill MapBill(SqlDataReader r)
        {
            return new Bill
            {
                BillId = r.GetInt32(r.GetOrdinal("BillId")),
                PatientId = r.GetInt32(r.GetOrdinal("PatientId")),
                TotalAmount = r.GetDecimal(r.GetOrdinal("TotalAmount")),
                PaidAmount = r.IsDBNull(r.GetOrdinal("PaidAmount")) ? 0 : r.GetDecimal(r.GetOrdinal("PaidAmount")),
                DueAmount = r.IsDBNull(r.GetOrdinal("DueAmount")) ? 0 : r.GetDecimal(r.GetOrdinal("DueAmount")),
                Status = r["Status"]?.ToString() ?? "Unpaid",
                BillDate = r.GetDateTime(r.GetOrdinal("BillDate")),
                ShiftId = r.IsDBNull(r.GetOrdinal("ShiftId")) ? (int?)null : r.GetInt32(r.GetOrdinal("ShiftId")),
                CreatedBy = r.IsDBNull(r.GetOrdinal("CreatedBy")) ? null : r["CreatedBy"].ToString(),
                PatientName = r.HasColumn("PatientName") ? r["PatientName"]?.ToString() : null
            };
        }
    }
}

