/*
 * FILE: AppointmentRepository.cs
 * PURPOSE: Manages appointment scheduling, rescheduling, and cancellation logic.
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
    /// Repository for managing appointment-related database operations.
    /// OPTIMIZATION: [Snapshotting] Implements transactional fee freezing to protect historical billing.
    /// OPTIMIZATION: [Join Optimization] Uses indexed JOINs between Patients and Appointments for faster retrieval.
    /// </summary>
    public class AppointmentRepository
    {
        private readonly DatabaseHelper _db;
        public AppointmentRepository(DatabaseHelper db) { _db = db; }

        private const string AppointmentColumns = "a.AppointmentId, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status, a.Reason, a.DoctorNotes, a.AppointmentMode, a.RejectionReason, a.RescheduledDate";

        /// <summary>
        /// Retrieves all appointments for a specific patient.
        /// </summary>
        public List<Appointment> GetAppointmentsByDoctorIdPaged(int doctorId, int skip, int take, string? filter = null)
        {
            if (doctorId <= 0) return new List<Appointment>();
            string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName FROM Appointments a JOIN Patients p ON a.PatientId = p.PatientId WHERE a.DoctorId = @Id AND (p.FullName LIKE @Filter OR a.Status LIKE @Filter) ORDER BY a.AppointmentDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Id", doctorId), new SqlParameter("@Skip", skip), new SqlParameter("@Take", take), new SqlParameter("@Filter", $"%{(filter ?? "")}%") };
            return _db.ExecuteQuery(query, MapAppointment, parameters);
        }

        public int GetAppointmentsByDoctorIdCount(int doctorId, string? filter = null)
        {
            if (doctorId <= 0) return 0;
            string query = "SELECT COUNT(*) FROM Appointments a JOIN Patients p ON a.PatientId = p.PatientId WHERE a.DoctorId = @Id AND (p.FullName LIKE @Filter OR a.Status LIKE @Filter)";
            return _db.ExecuteScalar<int>(query, new[] { new SqlParameter("@Id", doctorId), new SqlParameter("@Filter", $"%{(filter ?? "")}%") });
        }


        public async Task<List<Appointment>> GetAppointmentsByDoctorIdPagedAsync(int doctorId, int skip, int take, string? filter = null)
        {
            if (doctorId <= 0) return new List<Appointment>();
            string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName 
                             FROM Appointments a 
                             JOIN Patients p ON a.PatientId = p.PatientId 
                             WHERE a.DoctorId = @Id 
                             AND (p.FullName LIKE @Filter OR a.Status LIKE @Filter)
                             ORDER BY a.AppointmentDate DESC 
                             OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] {
                new SqlParameter("@Id", doctorId),
                new SqlParameter("@Skip", skip),
                new SqlParameter("@Take", take),
                new SqlParameter("@Filter", $"%{(filter ?? "")}%")
            };
            return await _db.ExecuteQueryAsync(query, MapAppointment, parameters);
        }

        public async Task<int> GetAppointmentsByDoctorIdCountAsync(int doctorId, string? filter = null)
        {
            if (doctorId <= 0) return 0;
            string query = "SELECT COUNT(*) FROM Appointments a JOIN Patients p ON a.PatientId = p.PatientId WHERE a.DoctorId = @Id AND (p.FullName LIKE @Filter OR a.Status LIKE @Filter)";
            return await _db.ExecuteScalarAsync<int>(query, new[] { new SqlParameter("@Id", doctorId), new SqlParameter("@Filter", $"%{(filter ?? "")}%") });
        }

        public async Task<List<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Appointment>();
                string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName, d.FullName as DoctorName, dept.DepartmentName 
                                 FROM Appointments a 
                                 JOIN Patients p ON a.PatientId = p.PatientId 
                                 JOIN Doctors d ON a.DoctorId = d.DoctorId 
                                 LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId
                                 WHERE a.PatientId = @Id 
                                 ORDER BY a.AppointmentDate DESC";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapAppointment, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointments for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Appointment> GetAppointmentsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<Appointment>();
            string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName, d.FullName as DoctorName, dept.DepartmentName FROM Appointments a JOIN Patients p ON a.PatientId = p.PatientId JOIN Doctors d ON a.DoctorId = d.DoctorId LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE a.PatientId = @Id ORDER BY a.AppointmentDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapAppointment, parameters);
        }

        /// <summary>
        /// Retrieves appointments for a specific doctor.
        /// </summary>
        public async Task<List<Appointment>> GetAppointmentsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Appointment>();
                string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName, d.FullName as DoctorName, dept.DepartmentName 
                                 FROM Appointments a 
                                 JOIN Patients p ON a.PatientId = p.PatientId 
                                 JOIN Doctors d ON a.DoctorId = d.DoctorId 
                                 LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId
                                 WHERE a.DoctorId = @Id 
                                 ORDER BY a.AppointmentDate DESC";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapAppointment, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointments for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Appointment> GetAppointmentsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<Appointment>();
            string query = $@"SELECT {AppointmentColumns}, p.FullName as PatientName, d.FullName as DoctorName, dept.DepartmentName FROM Appointments a JOIN Patients p ON a.PatientId = p.PatientId JOIN Doctors d ON a.DoctorId = d.DoctorId LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE a.DoctorId = @Id ORDER BY a.AppointmentDate DESC";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapAppointment, parameters);
        }

        /// <summary>
        /// Asynchronously creates a new appointment record.
        /// OPTIMIZATION: [Temporal Snapshot] We fetch the doctor's current fee and freeze it in the appointment.
        /// </summary>
        /// <summary>
        /// Asynchronously creates a new appointment record.
        /// OPTIMIZATION: [Temporal Snapshot] Fetches the doctor's current ConsultationFee and stores it in the Appointment.
        /// WHY: This ensures that if a doctor's fee changes in the future, past appointments still show the rate that was actually charged/quoted.
        /// </summary>
        public async Task CreateAppointmentAsync(Appointment appointment)
        {
            try
            {
                if (appointment == null || appointment.PatientId <= 0 || appointment.DoctorId <= 0) throw new ArgumentException("Invalid appointment data.");

                // Fetch current fee to snapshot it
                decimal currentFee = await _db.ExecuteScalarAsync<decimal>("SELECT ConsultationFee FROM Doctors WHERE DoctorId = @Id", new[] { new SqlParameter("@Id", appointment.DoctorId) });

                const string query = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentMode, Status, Reason, ConsultationFeeAtBooking) VALUES (@PatientId, @DoctorId, @AppointmentDate, @AppointmentMode, @Status, @Reason, @Fee)";
                var parameters = new[] {
                    new SqlParameter("@PatientId", appointment.PatientId),
                    new SqlParameter("@DoctorId", appointment.DoctorId),
                    new SqlParameter("@AppointmentDate", appointment.AppointmentDate),
                    new SqlParameter("@AppointmentMode", appointment.AppointmentMode ?? "Physical"),
                    new SqlParameter("@Status", appointment.Status ?? "Pending"),
                    new SqlParameter("@Reason", appointment.Reason ?? ""),
                    new SqlParameter("@Fee", currentFee)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create appointment: {ex.Message}", ex);
            }
        }

        public void CreateAppointment(Appointment a)
        {
            if (a == null || a.PatientId <= 0 || a.DoctorId <= 0) throw new ArgumentException("Invalid appointment data.");
            string query = @"INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, Status, Reason, AppointmentMode) VALUES (@PatientId, @DoctorId, @Date, @Status, @Reason, @Mode)";
            var parameters = new[] { new SqlParameter("@PatientId", a.PatientId), new SqlParameter("@DoctorId", a.DoctorId), new SqlParameter("@Date", a.AppointmentDate), new SqlParameter("@Status", a.Status ?? "Pending"), new SqlParameter("@Reason", a.Reason ?? ""), new SqlParameter("@Mode", a.AppointmentMode ?? "Physical") };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the status or details of an existing appointment.
        /// </summary>
        public async Task UpdateAppointmentStatusAsync(int id, string status, string? notes = null, string? rejectionReason = null, DateTime? rescheduledDate = null)
        {
            try
            {
                if (id <= 0) throw new ArgumentException("Invalid appointment ID.");
                string query = @"UPDATE Appointments 
                                 SET Status = @Status, 
                                     DoctorNotes = COALESCE(@Notes, DoctorNotes),
                                     RejectionReason = @RejectionReason,
                                     RescheduledDate = @RescheduledDate
                                 WHERE AppointmentId = @Id";
                await _db.ExecuteNonQueryAsync(query, new[] {
                    new SqlParameter("@Status", status),
                    new SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                    new SqlParameter("@RejectionReason", (object?)rejectionReason ?? DBNull.Value),
                    new SqlParameter("@RescheduledDate", (object?)rescheduledDate ?? DBNull.Value),
                    new SqlParameter("@Id", id)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update appointment {id}: {ex.Message}", ex);
            }
        }

        public void UpdateAppointmentStatus(int id, string status, string? notes = null, string? rejectionReason = null, DateTime? rescheduledDate = null)
        {
            if (id <= 0) throw new ArgumentException("Invalid appointment ID.");
            string query = @"UPDATE Appointments SET Status = @Status, DoctorNotes = COALESCE(@Notes, DoctorNotes), RejectionReason = @RejectionReason, RescheduledDate = @RescheduledDate WHERE AppointmentId = @Id";
            _db.ExecuteNonQuery(query, new[] { new SqlParameter("@Status", status), new SqlParameter("@Notes", (object?)notes ?? DBNull.Value), new SqlParameter("@RejectionReason", (object?)rejectionReason ?? DBNull.Value), new SqlParameter("@RescheduledDate", (object?)rescheduledDate ?? DBNull.Value), new SqlParameter("@Id", id) });
        }

        /// <summary>
        /// Retrieves a list of available doctors for appointment booking.
        /// </summary>
        public async Task<List<Doctor>> GetDoctorsAsync()
        {
            string query = "SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.IsActive = 1";
            return await _db.ExecuteQueryAsync(query, reader => new Doctor
            {
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                FullName = reader["FullName"]?.ToString() ?? "",
                Specialization = reader["Specialization"]?.ToString() ?? "",
                DepartmentName = reader.IsDBNull(reader.GetOrdinal("DepartmentName")) ? "" : reader["DepartmentName"].ToString() ?? "",
                ConsultationFee = reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
            });
        }

        public List<Doctor> GetDoctors()
        {
            string query = "SELECT d.*, dept.DepartmentName FROM Doctors d LEFT JOIN Departments dept ON d.DepartmentId = dept.DepartmentId WHERE d.IsActive = 1";
            return _db.ExecuteQuery(query, reader => new Doctor
            {
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                FullName = reader["FullName"]?.ToString() ?? "",
                Specialization = reader["Specialization"]?.ToString() ?? "",
                DepartmentName = reader.IsDBNull(reader.GetOrdinal("DepartmentName")) ? "" : reader["DepartmentName"].ToString() ?? "",
                ConsultationFee = reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
            });
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Appointment model.
        /// </summary>
        private Appointment MapAppointment(SqlDataReader reader)
        {
            var app = new Appointment
            {
                AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader["Status"]?.ToString() ?? "Pending",
                Reason = reader["Reason"]?.ToString() ?? "",
                DoctorNotes = reader.IsDBNull(reader.GetOrdinal("DoctorNotes")) ? null : reader["DoctorNotes"]?.ToString(),
                AppointmentMode = reader["AppointmentMode"]?.ToString() ?? "Physical",
                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader["RejectionReason"]?.ToString(),
                RescheduledDate = reader.IsDBNull(reader.GetOrdinal("RescheduledDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("RescheduledDate"))
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string col = reader.GetName(i);
                if (col == "PatientName") app.PatientName = reader[i]?.ToString() ?? "";
                else if (col == "DoctorName") app.DoctorName = reader[i]?.ToString() ?? "";
                else if (col == "DepartmentName") app.DepartmentName = reader[i]?.ToString() ?? "";
            }

            return app;
        }
    }
}
