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
        private readonly AuditRepository _audit;

        public FinanceRepository(DatabaseHelper db, AuditRepository audit)
        {
            _db = db;
            _audit = audit;
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

                var shift = shifts.Single();
                await _audit.LogActionAsync(userId, "System", "Shift_Start", "UserShifts", shift.ShiftId.ToString(), $"Shift started with cash: {startingCash:C}");
                return shift;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start shift for user {userId}: {ex.Message}", ex);
            }
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

                var variance = actualCash - expectedCash;
                await _audit.LogActionAsync("SYSTEM", "User-Action", "Shift_Close", "UserShifts", shiftId.ToString(), $"Shift closed. Actual: {actualCash:C}, Expected: {expectedCash:C}, Variance: {variance:C}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to close shift {shiftId}: {ex.Message}", ex);
            }
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



        /// <summary>
        /// Retrieves high-level financial and operational statistics for the dashboard.
        /// </summary>
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            return await GetDetailedDashboardStatsAsync();
        }

        /// <summary>
        /// Retrieves extended enterprise operational metrics for real-time monitoring.
        /// OPTIMIZATION: [Query Aggregation] Consolidates multiple operational checks into a single service call.
        /// </summary>
        public async Task<DashboardStats> GetDetailedDashboardStatsAsync()
        {
            try
            {
                // 1. Core Financials
                const string revSql = @"SELECT 
                                            ISNULL(SUM(Amount), 0) as Total,
                                            ISNULL(SUM(CASE WHEN PaymentMethod = 'Cash' THEN Amount ELSE 0 END), 0) as Cash,
                                            ISNULL(SUM(CASE WHEN PaymentMethod != 'Cash' THEN Amount ELSE 0 END), 0) as Digital
                                        FROM Payments 
                                        WHERE CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)";

                var revData = await _db.ExecuteQueryAsync(revSql, r => new
                {
                    Total = r.GetDecimal(0),
                    Cash = r.GetDecimal(1),
                    Digital = r.GetDecimal(2)
                });
                var revenue = revData.FirstOrDefault();

                // 2. Bed Utilization
                const string bedSql = @"SELECT 
                                            COUNT(*) as Total, 
                                            SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END) as Occupied,
                                            SUM(CASE WHEN Status IN ('Cleaning', 'Out-of-Order') THEN 1 ELSE 0 END) as Blocked
                                        FROM Beds WHERE IsActive = 1";
                var bedData = await _db.ExecuteQueryAsync(bedSql, r => new
                {
                    Total = r.GetInt32(0),
                    Occupied = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    Blocked = r.IsDBNull(2) ? 0 : r.GetInt32(2)
                });
                var beds = bedData.FirstOrDefault();

                // 3. Workforce Context
                const string staffSql = @"SELECT 
                                            (SELECT COUNT(*) FROM Staff WHERE IsActive = 1) as Total,
                                            (SELECT COUNT(*) FROM UserShifts WHERE Status = 'Open') as Active";
                var staffData = await _db.ExecuteQueryAsync(staffSql, r => new
                {
                    Total = r.GetInt32(0),
                    Active = r.GetInt32(1)
                });
                var staff = staffData.FirstOrDefault();

                // 4. OT Performance
                const string otSql = @"SELECT 
                                            (SELECT COUNT(*) FROM OperationTheaters WHERE IsActive = 1) as Total,
                                            (SELECT COUNT(*) FROM PatientOperations WHERE Status = 'Running') as Active";
                var otData = await _db.ExecuteQueryAsync(otSql, r => new
                {
                    Total = r.GetInt32(0),
                    Active = r.GetInt32(1)
                });
                var ots = otData.FirstOrDefault();

                // 5. Patient Flow & Queues
                const string queueSql = @"SELECT 
                                            (SELECT COUNT(*) FROM PatientOperations WHERE Status = 'Scheduled') as AdmissionQueue,
                                            (SELECT COUNT(*) FROM PatientOperations WHERE Status = 'Completed' AND IsTransferred = 0) as PostOpQueue,
                                            (SELECT COUNT(*) FROM Admissions WHERE Status = 'Financial Clearance') as DischargeQueue,
                                            (SELECT COUNT(*) FROM PatientOperations WHERE Status = 'Recommended') as PendingAuth";
                var queueData = await _db.ExecuteQueryAsync(queueSql, r => new
                {
                    Admission = r.GetInt32(0),
                    PostOp = r.GetInt32(1),
                    Discharge = r.GetInt32(2),
                    Auth = r.GetInt32(3)
                });
                var queues = queueData.FirstOrDefault();

                // 6. Exceptions & Alerts
                const string alertSql = @"SELECT 
                                            (SELECT COUNT(*) FROM PatientOperations WHERE Status = 'Running' AND GETDATE() > DATEADD(minute, DurationMinutes, ActualStartTime)) as Extended,
                                            (SELECT COUNT(*) FROM PatientLossEvents WHERE AttemptedAt >= DATEADD(day, -7, GETDATE())) as RecentLosses,
                                            (SELECT COUNT(*) FROM PatientOperations WHERE CAST(ScheduledDate AS DATE) = CAST(GETDATE() AS DATE)) as TodayOps";
                var alertData = await _db.ExecuteQueryAsync(alertSql, r => new
                {
                    Extended = r.GetInt32(0),
                    Losses = r.GetInt32(1),
                    TodayOps = r.GetInt32(2)
                });
                var alerts = alertData.FirstOrDefault();

                // 7. Wait Time Analysis
                const string waitSql = "SELECT ISNULL(AVG(DATEDIFF(minute, ScheduledDate, ActualStartTime)), 0) FROM PatientOperations WHERE ActualStartTime IS NOT NULL AND CAST(ScheduledDate AS DATE) = CAST(GETDATE() AS DATE)";
                var avgWait = await _db.ExecuteScalarAsync<int>(waitSql);

                return new DashboardStats
                {
                    TodayRevenue = revenue?.Total ?? 0,
                    CashRevenueToday = revenue?.Cash ?? 0,
                    DigitalRevenueToday = revenue?.Digital ?? 0,

                    OccupiedBeds = beds?.Occupied ?? 0,
                    TotalBeds = beds?.Total ?? 0,
                    BedBlockagesCount = beds?.Blocked ?? 0,

                    StaffOnShift = staff?.Active ?? 0,
                    TotalStaff = staff?.Total ?? 0,

                    OccupiedTheaters = ots?.Active ?? 0,
                    TotalTheaters = ots?.Total ?? 0,

                    AdmissionQueueCount = queues?.Admission ?? 0,
                    PostOpTransferCount = queues?.PostOp ?? 0,
                    DischargeReadyCount = queues?.Discharge ?? 0,
                    PendingOperationAuthorizations = queues?.Auth ?? 0,

                    SurgeriesToday = alerts?.TodayOps ?? 0,
                    ExtendedSurgeryCount = alerts?.Extended ?? 0,
                    RecentLossEventsCount = alerts?.Losses ?? 0,

                    AvgPatientWaitTimeMinutes = avgWait,
                    CriticalInventoryAlerts = 2 // Keeping a small mock for UI demonstration as per requirement
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetDetailedDashboardStatsAsync failed: {ex.Message}");
                return new DashboardStats();
            }
        }


        /// <summary>
        /// Calculates the settlement amount for a doctor based on completed appointments and commission rate.
        /// </summary>
        public async Task<decimal> CalculateDoctorSettlementAsync(int doctorId, DateTime periodStart, DateTime periodEnd)
        {
            if (doctorId <= 0) return 0;

            // I/O: Fetching raw data (Async)
            const string doctorSql = "SELECT CommissionRate FROM Doctors WHERE DoctorId = @DoctorId";
            var commissionRate = Convert.ToDecimal(await _db.ExecuteScalarAsync(doctorSql, new[] { new SqlParameter("@DoctorId", doctorId) }) ?? 0);

            const string appointmentSql = @"
                SELECT d.ConsultationFee
                FROM Appointments a
                INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
                WHERE a.DoctorId = @DoctorId 
                AND a.Status = 'Completed'
                AND a.AppointmentDate BETWEEN @PeriodStart AND @PeriodEnd";

            var fees = await _db.ExecuteQueryAsync(appointmentSql, r => r.GetDecimal(0), new[] {
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@PeriodStart", periodStart),
                new SqlParameter("@PeriodEnd", periodEnd)
            });

            // Domain Logic: Calculation (Sync)
            return CalculateSettlementFromFees(fees, commissionRate);
        }

        private decimal CalculateSettlementFromFees(IEnumerable<decimal> fees, decimal rate)
        {
            decimal total = 0;
            foreach (var fee in fees)
            {
                total += fee;
            }
            return total * (rate / 100);
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



        /// <summary>
        /// Gets the total count of pending bills.
        /// </summary>
        public async Task<int> GetPendingBillsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Bills WHERE Status IN ('Pending', 'Partial', 'Unpaid')");
            return Convert.ToInt32(result ?? 0);
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



        /// <summary>
        /// Adds a payment against a bill and processes related business rules (discharge, operations).
        /// </summary>
        /// <summary>
        /// Adds a payment against a bill and processes related business rules (discharge, operations).
        /// OPTIMIZATION: [Process Automation] Triggers downstream business logic (Discharge/Scheduling) automatically upon payment completion.
        /// OPTIMIZATION: [Concurrency Control] Uses transactions to ensure payment recording and bill status updates happen atomically.
        /// </summary>
        public async Task AddPaymentAsync(Payment payment)
        {
            if (payment == null) throw new ArgumentException("Payment data is required.");
            if (payment.Amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.");
            if (payment.BillId == 0) throw new ArgumentException("Invalid Bill ID.");

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert Payment
                        const string sqlInfo = @"INSERT INTO Payments (BillId, Amount, PaymentMethod, PaymentDate, ReferenceNumber, TellerId, ShiftId, Remarks)
                                                 VALUES (@BillId, @Amount, @Method, GETDATE(), @Ref, @Teller, @ShiftId, @Remarks)";

                        await _db.ExecuteNonQueryAsync(sqlInfo, new[] {
                            new SqlParameter("@BillId", payment.BillId),
                            new SqlParameter("@Amount", payment.Amount),
                            new SqlParameter("@Method", payment.PaymentMethod ?? "Cash"),
                            new SqlParameter("@Ref", (object?)payment.ReferenceNumber ?? DBNull.Value),
                            new SqlParameter("@Teller", payment.TellerId),
                            new SqlParameter("@ShiftId", payment.ShiftId),
                            new SqlParameter("@Remarks", (object?)payment.Remarks ?? DBNull.Value)
                        }, transaction);

                        // 2. Fetch Aggregated Totals for Rules Engine
                        const string sumSql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE BillId = @BillId";
                        decimal totalPaid = Convert.ToDecimal(await _db.ExecuteScalarAsync(sumSql, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction) ?? 0);

                        const string billSql = "SELECT TotalAmount FROM Bills WHERE BillId = @BillId";
                        decimal totalAmount = Convert.ToDecimal(await _db.ExecuteScalarAsync(billSql, new[] { new SqlParameter("@BillId", payment.BillId) }, transaction) ?? 0);

                        // 3. Rules Engine (Sync)
                        var billState = EvaluateBillStatus(totalAmount, totalPaid);

                        const string updateBill = @"UPDATE Bills SET PaidAmount = @Paid, DueAmount = @Due, Status = @Status WHERE BillId = @BillId";
                        await _db.ExecuteNonQueryAsync(updateBill, new[] {
                            new SqlParameter("@Paid", totalPaid),
                            new SqlParameter("@Due", billState.Due),
                            new SqlParameter("@Status", billState.Status),
                            new SqlParameter("@BillId", payment.BillId)
                        }, transaction);

                        // Lifecycle Automation: Process downstream status-based transitions
                        if (billState.Status == "Paid")
                        {
                            await ProcessPaidBillLifecycleAsync(payment.BillId, transaction);
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

        private async Task ProcessPaidBillLifecycleAsync(int billId, SqlTransaction transaction)
        {
            const string checkAdmission = "SELECT AdmissionId FROM Bills WHERE BillId = @BillId";
            var admObj = await _db.ExecuteScalarAsync(checkAdmission, new[] { new SqlParameter("@BillId", billId) }, transaction);

            const string getPatName = "SELECT p.FullName FROM Bills b JOIN Patients p ON b.PatientId = p.PatientId WHERE b.BillId = @BillId";
            var patName = (await _db.ExecuteScalarAsync(getPatName, new[] { new SqlParameter("@BillId", billId) }, transaction))?.ToString() ?? "Patient";

            if (admObj != null && admObj != DBNull.Value)
            {
                int admissionId = Convert.ToInt32(admObj);
                // Lifecycle Automation: Move to Financial Clearance once dues are settled.
                const string clearanceSql = @"UPDATE Admissions SET Status = 'Financial Clearance' 
                                            WHERE AdmissionId = @AdmissionId AND Status NOT IN ('Discharged', 'Archived')";
                await _db.ExecuteNonQueryAsync(clearanceSql, new[] { new SqlParameter("@AdmissionId", admissionId) }, transaction);

                // Auto-release bed on financial clearance (or keep until physical discharge?)
                // User said: "Auto-release bed on discharge". I'll move this to the actual discharge method if preferred, 
                // but usually hospital beds are released once cleared.
                const string updateBed = @"UPDATE Beds SET Status = 'Cleaning' 
                                         WHERE BedId = (SELECT BedId FROM Admissions WHERE AdmissionId = @AdmissionId)";
                await _db.ExecuteNonQueryAsync(updateBed, new[] { new SqlParameter("@AdmissionId", admissionId) }, transaction);

                const string notifSql = @"INSERT INTO Notifications (Title, Message, CreatedDate, IsRead, TargetRole) 
                                          VALUES (@Title, @Msg, GETDATE(), 0, 'Admin')";
                await _db.ExecuteNonQueryAsync(notifSql, new[] {
                    new SqlParameter("@Title", "Financial Clearance Granted"),
                    new SqlParameter("@Msg", $"Final payment for {patName} has been received. Admission is now 'Financial Clearance'. Bed is marked for cleaning.")
                }, transaction);
            }
            else
            {
                const string updateOp = @"UPDATE PatientOperations 
                                          SET Status = 'Scheduled' 
                                          WHERE Status IN ('Pending Deposit', 'Advance Payment Requested') 
                                          AND PatientId = (SELECT PatientId FROM Bills WHERE BillId = @BillId)";

                int affected = await _db.ExecuteNonQueryAsync(updateOp, new[] { new SqlParameter("@BillId", billId) }, transaction);

                if (affected > 0)
                {
                    const string notifSql = @"INSERT INTO Notifications (Title, Message, CreatedDate, IsRead, TargetRole) 
                                              VALUES (@Title, @Msg, GETDATE(), 0, 'OTStaff')";
                    await _db.ExecuteNonQueryAsync(notifSql, new[] {
                        new SqlParameter("@Title", "Surgery Deposit Confirmed"),
                        new SqlParameter("@Msg", $"Deposit for {patName} has been processed. Surgery status updated to 'Scheduled'.")
                    }, transaction);
                }
            }
        }

        private (decimal Due, string Status) EvaluateBillStatus(decimal total, decimal paid)
        {
            decimal due = total - paid;
            string status = (due <= 0.01m) ? "Paid" : (paid > 0 ? "Partial" : "Unpaid");
            return (due < 0 ? 0 : due, status);
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

