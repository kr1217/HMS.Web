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
        public DoctorRepository(DatabaseHelper db) { _db = db; }

        private const string DoctorColumns = "DoctorId, UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization, MedicalLicenseNumber, LicenseIssuingAuthority, YearsOfExperience, DepartmentId, HospitalJoiningDate, ConsultationFee, FollowUpFee, AvailableDays, AvailableTimeSlots, RoomNumber, IsOnCall, IsActive, IsVerified, CreatedAt, IsAvailable, CommissionRate";

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

        public List<Doctor> GetAllDoctors()
        {
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.IsActive = 1";
            return _db.ExecuteQuery(query, MapDoctor);
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

        public Doctor? GetDoctorById(int id)
        {
            if (id <= 0) return null;
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.DoctorId = @Id";
            var parameters = new[] { new SqlParameter("@Id", id) };
            return _db.ExecuteQuery(query, MapDoctor, parameters).FirstOrDefault();
        }

        public async Task<Doctor?> GetDoctorByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.UserId = @UserId";
            var list = await _db.ExecuteQueryAsync(query, MapDoctor, new[] { new SqlParameter("@UserId", userId) });
            return list.FirstOrDefault();
        }

        public Doctor? GetDoctorByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.UserId = @UserId";
            return _db.ExecuteQuery(query, MapDoctor, new[] { new SqlParameter("@UserId", userId) }).FirstOrDefault();
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

        public List<Doctor> GetDoctorsByDepartment(int departmentId)
        {
            if (departmentId <= 0) return new List<Doctor>();
            string query = $"SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.DepartmentId = @DeptId AND d.IsActive = 1";
            var parameters = new[] { new SqlParameter("@DeptId", departmentId) };
            return _db.ExecuteQuery(query, MapDoctor, parameters);
        }

        /// <summary>
        /// Dashboard statistics for a specific doctor.
        /// </summary>

        /// <summary>
        /// Asynchronously updates a doctor's profile.
        /// </summary>
        public async Task UpdateDoctorAsync(Doctor d)
        {
            try
            {
                if (d == null || d.DoctorId <= 0) throw new ArgumentException("Invalid doctor data.");

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
                                     CommissionRate = @Comm
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
                    new SqlParameter("@Id", d.DoctorId)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update doctor {d?.DoctorId}: {ex.Message}", ex);
            }
        }

        public void UpdateDoctor(Doctor d)
        {
            if (d == null || d.DoctorId <= 0) throw new ArgumentException("Invalid doctor data.");
            string query = @"UPDATE Doctors SET FullName = @FullName, Gender = @Gender, ContactNumber = @Phone, Email = @Email, Qualification = @Qual, Specialization = @Spec, MedicalLicenseNumber = @License, YearsOfExperience = @Exp, DepartmentId = @DeptId, ConsultationFee = @Fee, FollowUpFee = @FollowFee, AvailableDays = @Days, AvailableTimeSlots = @Slots, RoomNumber = @Room, IsOnCall = @OnCall, IsActive = @Active, IsAvailable = @Available, CommissionRate = @Comm WHERE DoctorId = @Id";
            var parameters = new[] { new SqlParameter("@FullName", d.FullName ?? ""), new SqlParameter("@Gender", d.Gender ?? ""), new SqlParameter("@Phone", d.ContactNumber ?? ""), new SqlParameter("@Email", d.Email ?? ""), new SqlParameter("@Qual", d.Qualification ?? ""), new SqlParameter("@Spec", d.Specialization ?? ""), new SqlParameter("@License", d.MedicalLicenseNumber ?? ""), new SqlParameter("@Exp", d.YearsOfExperience), new SqlParameter("@DeptId", d.DepartmentId), new SqlParameter("@Fee", d.ConsultationFee), new SqlParameter("@FollowFee", d.FollowUpFee), new SqlParameter("@Days", d.AvailableDays ?? ""), new SqlParameter("@Slots", d.AvailableTimeSlots ?? ""), new SqlParameter("@Room", d.RoomNumber ?? ""), new SqlParameter("@OnCall", d.IsOnCall), new SqlParameter("@Active", d.IsActive), new SqlParameter("@Available", d.IsAvailable), new SqlParameter("@Comm", d.CommissionRate), new SqlParameter("@Id", d.DoctorId) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Retrieves real-time dashboard statistics for a specific doctor.
        /// OPTIMIZATION: [Compute at Source] Uses SQL subqueries to calculate appointment counts and revenue directly on the DB server.
        /// WHY: This prevents pulling thousands of individual appointment rows into memory just to count them.
        /// </summary>
        public DoctorDashboardStats GetDoctorDashboardStats(int doctorId)
        {
            if (doctorId <= 0) return new DoctorDashboardStats();
            string query = @"SELECT 
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Scheduled' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)) as TodayAppointments,
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Pending') as NewRequests,
                (SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @Id) as TotalPatients,
                (SELECT ISNULL(SUM(Amount), 0) FROM Payments p JOIN Bills b ON p.BillId = b.BillId WHERE b.PatientId IN (SELECT PatientId FROM Appointments WHERE DoctorId = @Id) AND MONTH(p.PaymentDate) = MONTH(GETDATE())) as Revenue";

            return _db.ExecuteQuery(query, reader => new DoctorDashboardStats
            {
                AppointmentsToday = reader.GetInt32(0),
                PendingApprovals = reader.GetInt32(1),
                TotalPatientsServed = reader.GetInt32(2),
                MonthlyCommission = reader.GetDecimal(3)
            }, new[] { new SqlParameter("@Id", doctorId) }).FirstOrDefault() ?? new DoctorDashboardStats();
        }

        public async Task<DoctorDashboardStats> GetDoctorDashboardStatsAsync(int doctorId)
        {
            if (doctorId <= 0) return new DoctorDashboardStats();
            string query = @"SELECT 
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Scheduled' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)) as TodayAppointments,
                (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Pending') as NewRequests,
                (SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @Id) as TotalPatients,
                (SELECT ISNULL(SUM(Amount), 0) FROM Payments p JOIN Bills b ON p.BillId = b.BillId WHERE b.PatientId IN (SELECT PatientId FROM Appointments WHERE DoctorId = @Id) AND MONTH(p.PaymentDate) = MONTH(GETDATE())) as Revenue";

            var results = await _db.ExecuteQueryAsync(query, reader => new DoctorDashboardStats
            {
                AppointmentsToday = reader.GetInt32(0),
                PendingApprovals = reader.GetInt32(1),
                TotalPatientsServed = reader.GetInt32(2),
                MonthlyCommission = reader.GetDecimal(3)
            }, new[] { new SqlParameter("@Id", doctorId) });
            return results.FirstOrDefault() ?? new DoctorDashboardStats();
        }

        public List<Doctor> GetDoctors()
        {
            string query = "SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.IsActive = 1";
            return _db.ExecuteQuery(query, MapDoctor);
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
                CommissionRate = reader.GetDecimal(reader.GetOrdinal("CommissionRate"))
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "DepartmentName") d.DepartmentName = reader[i]?.ToString() ?? "";
            }

            return d;
        }
    }
}

