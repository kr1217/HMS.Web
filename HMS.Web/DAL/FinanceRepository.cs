
using HMS.Web.Data;
using HMS.Web.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Data.SqlClient;

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
            const string sql = @"SELECT TOP 1 us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                               FROM UserShifts us
                               LEFT JOIN Staff st ON us.UserId = st.UserId
                               WHERE us.UserId = @UserId AND us.Status = 'Open' 
                               ORDER BY us.StartTime DESC";

            var shifts = _db.ExecuteQuery(sql, MapUserShift, new[] { new SqlParameter("@UserId", userId) });
            return shifts.FirstOrDefault();
        }

        public List<UserShift> GetAllShifts()
        {
            const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                               FROM UserShifts us
                               LEFT JOIN Staff st ON us.UserId = st.UserId
                               ORDER BY us.StartTime DESC";
            return _db.ExecuteQuery(sql, MapUserShift);
        }

        public UserShift StartShift(string userId, decimal startingCash)
        {
            // Close any existing open shifts just in case
            const string closeOld = @"UPDATE UserShifts SET Status = 'Closed', EndTime = GETDATE(), Notes = 'Auto-closed by new shift' 
                                      WHERE UserId = @UserId AND Status = 'Open'";
            _db.ExecuteNonQuery(closeOld, new[] { new SqlParameter("@UserId", userId) });

            const string sql = @"INSERT INTO UserShifts (UserId, StartTime, StartingCash, Status) 
                               OUTPUT INSERTED.* 
                               VALUES (@UserId, GETDATE(), @StartingCash, 'Open')";

            var shifts = _db.ExecuteQuery(sql, MapUserShift, new[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@StartingCash", startingCash)
            });

            return shifts.Single();
        }

        public decimal GetShiftRevenue(int shiftId)
        {
            // Updated to check Payments table to get actual collected revenue for this shift
            const string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId";
            return Convert.ToDecimal(_db.ExecuteScalar(sql, new[] {
                new SqlParameter("@ShiftId", shiftId)
            }));
        }

        public void CloseShift(int shiftId, decimal actualCash, string? notes)
        {
            // Calculate EndingCash (System Expected) from CASH Payments only + Starting Cash
            const string calcSql = "SELECT ISNULL(SUM(Amount), 0) FROM Payments WHERE ShiftId = @ShiftId AND PaymentMethod = 'Cash'";
            decimal collectedCash = Convert.ToDecimal(_db.ExecuteScalar(calcSql, new[] {
                new SqlParameter("@ShiftId", shiftId)
            }));

            // Get Starting Cash
            const string startSql = "SELECT StartingCash FROM UserShifts WHERE ShiftId = @ShiftId";
            var startResult = _db.ExecuteScalar(startSql, new[] { new SqlParameter("@ShiftId", shiftId) });
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

            _db.ExecuteNonQuery(sql, new[] {
                new SqlParameter("@ShiftId", shiftId),
                new SqlParameter("@ActualCash", actualCash),
                new SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                new SqlParameter("@ExpectedCash", expectedCash)
            });
        }

        public List<UserShift> GetShiftsRecursively(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"SELECT us.*, st.FullName as TellerName, st.StaffId as EmployeeId 
                               FROM UserShifts us
                               LEFT JOIN Staff st ON us.UserId = st.UserId
                               WHERE us.StartTime BETWEEN @From AND @To 
                               ORDER BY us.StartTime DESC";

            return _db.ExecuteQuery(sql, MapUserShift, new[] {
                new SqlParameter("@From", fromDate),
                new SqlParameter("@To", toDate)
            });
        }

        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();

            // 1. Revenue (Actual Collected Cash/Card today)
            var revSql = "SELECT SUM(Amount) FROM Payments WHERE CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)";
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
            const string doctorSql = "SELECT CommissionRate FROM Doctors WHERE DoctorId = @DoctorId";
            var commissionRate = _db.ExecuteScalar(doctorSql, new[] {
                new SqlParameter("@DoctorId", doctorId)
            });

            if (commissionRate == null || commissionRate == DBNull.Value)
                return 0;

            decimal rate = Convert.ToDecimal(commissionRate) / 100;

            const string appointmentSql = @"
                SELECT ISNULL(SUM(d.ConsultationFee), 0) 
                FROM Appointments a
                INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
                WHERE a.DoctorId = @DoctorId 
                AND a.Status = 'Completed'
                AND a.AppointmentDate BETWEEN @PeriodStart AND @PeriodEnd";

            var totalFees = Convert.ToDecimal(_db.ExecuteScalar(appointmentSql, new[] {
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@PeriodStart", periodStart),
                new SqlParameter("@PeriodEnd", periodEnd)
            }));

            return totalFees * rate;
        }

        public void ProcessDoctorPayment(DoctorPayment payment)
        {
            const string sql = @"INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes)
                                VALUES (@DoctorId, @Amount, @PaymentDate, @PeriodStart, @PeriodEnd, @Status, @Notes)";

            _db.ExecuteNonQuery(sql, new[] {
                new SqlParameter("@DoctorId", payment.DoctorId),
                new SqlParameter("@Amount", payment.Amount),
                new SqlParameter("@PaymentDate", payment.PaymentDate),
                new SqlParameter("@PeriodStart", payment.PeriodStart),
                new SqlParameter("@PeriodEnd", payment.PeriodEnd),
                new SqlParameter("@Status", payment.Status),
                new SqlParameter("@Notes", (object?)payment.Notes ?? DBNull.Value)
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
                new SqlParameter("@DoctorId", doctorId)
            });
        }

        // --- NEW TELLER METHODS ---

        public List<Bill> GetPendingBills()
        {
            // Note: Assuming Patients table has FullName.
            const string sql = @"SELECT b.*, p.FullName as PatientName 
                                FROM Bills b
                                INNER JOIN Patients p ON b.PatientId = p.PatientId
                                WHERE b.Status IN ('Pending', 'Partial', 'Unpaid')
                                ORDER BY b.BillDate DESC";
            return _db.ExecuteQuery(sql, MapBill);
        }

        public Bill? GetBillById(int billId)
        {
            const string sql = @"SELECT b.*, p.FullName as PatientName 
                                FROM Bills b
                                INNER JOIN Patients p ON b.PatientId = p.PatientId
                                WHERE b.BillId = @BillId";
            var bills = _db.ExecuteQuery(sql, MapBill, new[] { new SqlParameter("@BillId", billId) });
            return bills.FirstOrDefault();
        }

        public void AddPayment(Payment payment)
        {
            // 1. Insert Payment
            const string sqlInfo = @"INSERT INTO Payments (BillId, Amount, PaymentMethod, PaymentDate, ReferenceNumber, TellerId, ShiftId, Remarks)
                                     VALUES (@BillId, @Amount, @Method, GETDATE(), @Ref, @Teller, @ShiftId, @Remarks)";

            _db.ExecuteNonQuery(sqlInfo, new[] {
                new SqlParameter("@BillId", payment.BillId),
                new SqlParameter("@Amount", payment.Amount),
                new SqlParameter("@Method", payment.PaymentMethod),
                new SqlParameter("@Ref", (object?)payment.ReferenceNumber ?? DBNull.Value),
                new SqlParameter("@Teller", payment.TellerId),
                new SqlParameter("@ShiftId", payment.ShiftId),
                new SqlParameter("@Remarks", (object?)payment.Remarks ?? DBNull.Value)
            });

            // 2. Update Bill Status
            const string sumSql = "SELECT SUM(Amount) FROM Payments WHERE BillId = @BillId";
            decimal totalPaid = Convert.ToDecimal(_db.ExecuteScalar(sumSql, new[] { new SqlParameter("@BillId", payment.BillId) }));

            const string billSql = "SELECT TotalAmount FROM Bills WHERE BillId = @BillId";
            decimal totalAmount = Convert.ToDecimal(_db.ExecuteScalar(billSql, new[] { new SqlParameter("@BillId", payment.BillId) }));

            decimal due = totalAmount - totalPaid;
            string status = (due <= 0.01m) ? "Paid" : "Partial"; // Tolerance for float math

            const string updateBill = @"UPDATE Bills SET PaidAmount = @Paid, DueAmount = @Due, Status = @Status WHERE BillId = @BillId";
            _db.ExecuteNonQuery(updateBill, new[] {
                new SqlParameter("@Paid", totalPaid),
                new SqlParameter("@Due", (due < 0 ? 0 : due)),
                new SqlParameter("@Status", status),
                new SqlParameter("@BillId", payment.BillId)
            });

            // 3. Auto-Discharge if Paid & Linked to Admission
            if (status == "Paid")
            {
                const string checkAdmission = "SELECT AdmissionId FROM Bills WHERE BillId = @BillId";
                var admObj = _db.ExecuteScalar(checkAdmission, new[] { new SqlParameter("@BillId", payment.BillId) });

                if (admObj != null && admObj != DBNull.Value)
                {
                    int admissionId = Convert.ToInt32(admObj);
                    const string dischargeSql = @"UPDATE Admissions SET Status = 'Discharged', DischargeDate = GETDATE() 
                                                WHERE AdmissionId = @AdmissionId AND Status != 'Discharged'";
                    _db.ExecuteNonQuery(dischargeSql, new[] { new SqlParameter("@AdmissionId", admissionId) });

                    // Also free up the bed
                    // This logic usually resides in FacilityRepository but we can run the SQL here for atomicity/simplicity in this context
                    const string updateBed = @"UPDATE Beds SET Status = 'Available' 
                                             WHERE BedId = (SELECT BedId FROM Admissions WHERE AdmissionId = @AdmissionId)";
                    _db.ExecuteNonQuery(updateBed, new[] { new SqlParameter("@AdmissionId", admissionId) });
                }

                // B. Auto-Confirm Operation if this was a deposit
                const string updateOp = @"UPDATE PatientOperations 
                                          SET Status = 'Scheduled' 
                                          WHERE Status = 'Pending Deposit' 
                                          AND PatientId = (SELECT PatientId FROM Bills WHERE BillId = @BillId)";

                _db.ExecuteNonQuery(updateOp, new[] { new SqlParameter("@BillId", payment.BillId) });

                // C. Create Notification for Admin
                const string getPatName = "SELECT p.FullName FROM Bills b JOIN Patients p ON b.PatientId = p.PatientId WHERE b.BillId = @BillId";
                var patName = _db.ExecuteScalar(getPatName, new[] { new SqlParameter("@BillId", payment.BillId) })?.ToString() ?? "Patient";

                const string notifSql = @"INSERT INTO Notifications (Title, Message, CreatedDate, IsRead, TargetRole) 
                                          VALUES (@Title, @Msg, GETDATE(), 0, 'OTStaff')";
                _db.ExecuteNonQuery(notifSql, new[] {
                    new SqlParameter("@Title", "Surgery Deposit Confirmed"),
                    new SqlParameter("@Msg", $"Deposit for {patName} has been processed by Teller. Surgery status updated to 'Scheduled'.")
                });
            }
        }


        // --- MAPPERS ---

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
                DoctorName = r.Table.Columns.Contains("DoctorName") ? r["DoctorName"].ToString() : null
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
                Notes = r["Notes"]?.ToString(),
                TellerName = r.Table.Columns.Contains("TellerName") && r["TellerName"] != DBNull.Value ? r["TellerName"].ToString() : "Unknown",
                EmployeeId = r.Table.Columns.Contains("EmployeeId") && r["EmployeeId"] != DBNull.Value ? (int?)r["EmployeeId"] : null,
            };
        }

        private Bill MapBill(System.Data.DataRow r)
        {
            return new Bill
            {
                BillId = (int)r["BillId"],
                PatientId = (int)r["PatientId"],
                TotalAmount = (decimal)r["TotalAmount"],
                PaidAmount = r["PaidAmount"] != DBNull.Value ? (decimal)r["PaidAmount"] : 0,
                DueAmount = r["DueAmount"] != DBNull.Value ? (decimal)r["DueAmount"] : 0,
                Status = r["Status"].ToString()!,
                BillDate = (DateTime)r["BillDate"],
                ShiftId = r["ShiftId"] != DBNull.Value ? (int)r["ShiftId"] : (int?)null,
                CreatedBy = r["CreatedBy"] != DBNull.Value ? r["CreatedBy"].ToString() : null,
                PatientName = r.Table.Columns.Contains("PatientName") ? r["PatientName"].ToString() : null
            };
        }
    }
}
