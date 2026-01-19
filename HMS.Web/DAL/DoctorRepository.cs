/*
 * FILE: DoctorRepository.cs
 * PURPOSE: Manages doctor profiles and retrieval.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Appointments.razor, Doctor/Dashboard.razor
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing doctor-related database operations and statistics.
    /// OPTIMIZATION: [Join Strategy] Uses LEFT JOIN with Departments to provide human-readable names in a single round-trip.
    /// OPTIMIZATION: [Aggregated Dashboards] Uses correlated subqueries for fast statistics retrieval.
    /// </summary>
    public class DoctorRepository
    {
        private readonly DatabaseHelper _db;
        private readonly AuditRepository _audit;
        public DoctorRepository(DatabaseHelper db, AuditRepository audit)
        {
            _db = db;
            _audit = audit;
        }

        private const string DoctorColumns = "DoctorId, UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization, MedicalLicenseNumber, LicenseIssuingAuthority, YearsOfExperience, DepartmentId, HospitalJoiningDate, ConsultationFee, FollowUpFee, AvailableDays, AvailableTimeSlots, RoomNumber, IsOnCall, IsActive, IsVerified, CreatedAt, IsAvailable, CommissionRate, SurgeryCommission, RecommendationCommission";

        /// <summary>
        /// Retrieves all active doctors with their department details.
        /// </summary>
        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            try
            {
                string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.IsActive = 1";
                return await _db.ExecuteQueryAsync(query, MapDoctor);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all doctors: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Retrieves a specific doctor by their ID.
        /// </summary>
        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            try
            {
                if (id <= 0) return null;
                string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.DoctorId = @Id";
                var parameters = new[] { new SqlParameter("@Id", id) };
                var list = await _db.ExecuteQueryAsync(query, MapDoctor, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor {id}: {ex.Message}", ex);
            }
        }


        public async Task<Doctor?> GetDoctorByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.UserId = @UserId";
            var list = await _db.ExecuteQueryAsync(query, MapDoctor, new[] { new SqlParameter("@UserId", userId) });
            return list.FirstOrDefault();
        }


        /// <summary>
        /// Retrieves all doctors belonging to a specific department.
        /// </summary>
        public async Task<List<Doctor>> GetDoctorsByDepartmentAsync(int departmentId)
        {
            try
            {
                if (departmentId <= 0) return new List<Doctor>();
                string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.DepartmentId = @DeptId AND d.IsActive = 1";
                var parameters = new[] { new SqlParameter("@DeptId", departmentId) };
                return await _db.ExecuteQueryAsync(query, MapDoctor, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctors for department {departmentId}: {ex.Message}", ex);
            }
        }


        public async Task UpdateDoctorAsync(Doctor d)
        {
            try
            {
                if (d == null || d.DoctorId <= 0) throw new ArgumentException("Invalid doctor data.");

                // Audit: Check for sensitive changes
                var old = await GetDoctorByIdAsync(d.DoctorId);
                if (old != null)
                {
                    var financialAudit = EvaluateFinancialChanges(old, d);
                    if (financialAudit.Changed)
                    {
                        await _audit.LogActionAsync("SYSTEM", "User-Action", "Doctor_Price_Override", "Doctors", d.DoctorId.ToString(), financialAudit.Details);
                    }
                }

                string query = @"UPDATE Doctors 
                                 SET FullName = @FullName, 
                                     Gender = @Gender, 
                                     ContactNumber = @Phone, 
                                     Email = @Email, 
                                     Qualification = @Qual, 
                                     Specialization = @Spec,
                                     MedicalLicenseNumber = @License,
                                     YearsOfExperience = @Exp,
                                     DepartmentId = @DeptId,
                                     ConsultationFee = @Fee,
                                     FollowUpFee = @FollowFee,
                                     AvailableDays = @Days,
                                     AvailableTimeSlots = @Slots,
                                     RoomNumber = @Room,
                                     IsOnCall = @OnCall,
                                     IsActive = @Active,
                                     IsAvailable = @Available,
                                     CommissionRate = @Comm,
                                     SurgeryCommission = @SurgComm,
                                     RecommendationCommission = @RecComm
                                 WHERE DoctorId = @Id";
                var parameters = new[]
                {
                    new SqlParameter("@FullName", d.FullName ?? ""),
                    new SqlParameter("@Gender", d.Gender ?? ""),
                    new SqlParameter("@Phone", d.ContactNumber ?? ""),
                    new SqlParameter("@Email", d.Email ?? ""),
                    new SqlParameter("@Qual", d.Qualification ?? ""),
                    new SqlParameter("@Spec", d.Specialization ?? ""),
                    new SqlParameter("@License", d.MedicalLicenseNumber ?? ""),
                    new SqlParameter("@Exp", d.YearsOfExperience),
                    new SqlParameter("@DeptId", d.DepartmentId),
                    new SqlParameter("@Fee", d.ConsultationFee),
                    new SqlParameter("@FollowFee", d.FollowUpFee),
                    new SqlParameter("@Days", d.AvailableDays ?? ""),
                    new SqlParameter("@Slots", d.AvailableTimeSlots ?? ""),
                    new SqlParameter("@Room", d.RoomNumber ?? ""),
                    new SqlParameter("@OnCall", d.IsOnCall),
                    new SqlParameter("@Active", d.IsActive),
                    new SqlParameter("@Available", d.IsAvailable),
                    new SqlParameter("@Comm", d.CommissionRate),
                    new SqlParameter("@SurgComm", d.SurgeryCommission),
                    new SqlParameter("@RecComm", d.RecommendationCommission),
                    new SqlParameter("@Id", d.DoctorId)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update doctor {d?.DoctorId}: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Retrieves real-time dashboard statistics for a specific doctor.
        /// OPTIMIZATION: [Compute at Source] Uses SQL subqueries to calculate appointment counts and revenue directly on the DB server.
        /// WHY: This prevents pulling thousands of individual appointment rows into memory just to count them.
        /// </summary>
        public async Task<DoctorDashboardStats> GetDoctorDashboardStatsAsync(int doctorId)
        {
            if (doctorId <= 0) return new DoctorDashboardStats();

            // I/O (Async)
            string query = @"SELECT 
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Scheduled' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)) as TodayAppointments,
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Pending') as NewRequests,
                (SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @Id) as TotalPatients,
                (SELECT ISNULL(SUM(Amount), 0) FROM Payments p JOIN Bills b ON p.BillId = b.BillId WHERE b.PatientId IN (SELECT PatientId FROM Appointments WHERE DoctorId = @Id) AND MONTH(p.PaymentDate) = MONTH(GETDATE())) as Revenue";

            var rawStats = await _db.ExecuteQueryAsync(query, r => new
            {
                Today = r.GetInt32(0),
                Pending = r.GetInt32(1),
                Patients = r.GetInt32(2),
                Revenue = r.GetDecimal(3)
            }, new[] { new SqlParameter("@Id", doctorId) });

            // Domain Logic: Assembly (Sync)
            return AssembleDoctorStats(rawStats);
        }

        private DoctorDashboardStats AssembleDoctorStats(IEnumerable<dynamic> rawStats)
        {
            var data = rawStats.FirstOrDefault();
            if (data == null) return new DoctorDashboardStats();

            return new DoctorDashboardStats
            {
                AppointmentsToday = data.Today,
                PendingApprovals = data.Pending,
                TotalPatientsServed = data.Patients,
                MonthlyCommission = data.Revenue
            };
        }

        private (bool Changed, string Details) EvaluateFinancialChanges(Doctor old, Doctor d)
        {
            bool changed = old.ConsultationFee != d.ConsultationFee ||
                           old.CommissionRate != d.CommissionRate ||
                           old.SurgeryCommission != d.SurgeryCommission ||
                           old.RecommendationCommission != d.RecommendationCommission;

            string details = changed ? $"Financial terms changed. Fee: {old.ConsultationFee}->{d.ConsultationFee}, Comm: {old.CommissionRate}->{d.CommissionRate}" : "";
            return (changed, details);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Doctor model.
        /// </summary>
        private Doctor MapDoctor(SqlDataReader reader)
        {
            var d = new Doctor
            {
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                UserId = reader["UserId"]?.ToString() ?? "",
                FullName = reader["FullName"]?.ToString() ?? "",
                Gender = reader["Gender"]?.ToString() ?? "",
                ContactNumber = reader["ContactNumber"]?.ToString() ?? "",
                Email = reader["Email"]?.ToString() ?? "",
                Qualification = reader["Qualification"]?.ToString() ?? "",
                Specialization = reader["Specialization"]?.ToString() ?? "",
                MedicalLicenseNumber = reader["MedicalLicenseNumber"]?.ToString() ?? "",
                LicenseIssuingAuthority = reader.IsDBNull(reader.GetOrdinal("LicenseIssuingAuthority")) ? null : reader["LicenseIssuingAuthority"]?.ToString(),
                YearsOfExperience = reader.GetInt32(reader.GetOrdinal("YearsOfExperience")),
                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                HospitalJoiningDate = reader.GetDateTime(reader.GetOrdinal("HospitalJoiningDate")),
                ConsultationFee = reader.GetDecimal(reader.GetOrdinal("ConsultationFee")),
                FollowUpFee = reader.GetDecimal(reader.GetOrdinal("FollowUpFee")),
                AvailableDays = reader["AvailableDays"]?.ToString() ?? "",
                AvailableTimeSlots = reader["AvailableTimeSlots"]?.ToString() ?? "",
                RoomNumber = reader["RoomNumber"]?.ToString() ?? "",
                IsOnCall = reader.GetBoolean(reader.GetOrdinal("IsOnCall")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsVerified = reader.GetBoolean(reader.GetOrdinal("IsVerified")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                CommissionRate = reader.GetDecimal(reader.GetOrdinal("CommissionRate")),
                SurgeryCommission = reader.HasColumn("SurgeryCommission") ? (reader.IsDBNull(reader.GetOrdinal("SurgeryCommission")) ? 0 : reader.GetDecimal(reader.GetOrdinal("SurgeryCommission"))) : 0,
                RecommendationCommission = reader.HasColumn("RecommendationCommission") ? (reader.IsDBNull(reader.GetOrdinal("RecommendationCommission")) ? 0 : reader.GetDecimal(reader.GetOrdinal("RecommendationCommission"))) : 0
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "DepartmentName") d.DepartmentName = reader[i]?.ToString() ?? "";
            }

            return d;
        }
    }
}
