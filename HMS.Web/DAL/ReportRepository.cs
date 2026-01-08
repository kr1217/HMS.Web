/*
 * FILE: ReportRepository.cs
 * PURPOSE: Manages medical reports/files.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Reports.razor, Doctor/UploadReport.razor
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
    /// Repository for managing medical report-related database operations.
    /// OPTIMIZATION: [Lazy Loading] File content is NOT loaded by default (only metadata like FilePath) to keep listing queries lightweight.
    /// </summary>
    public class ReportRepository
    {
        private readonly DatabaseHelper _db;
        public ReportRepository(DatabaseHelper db) { _db = db; }

        private const string ReportColumns = "ReportId, PatientId, DoctorId, AppointmentId, ReportName, ReportType, ReportDate, FilePath, Status, Observations";

        /// <summary>
        /// Retrieves all reports for a specific patient.
        /// </summary>
        public async Task<List<Report>> GetReportsByPatientIdAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Report>();
                string query = $"SELECT {ReportColumns} FROM Reports WHERE PatientId = @Id ORDER BY ReportDate DESC";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapReport, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving reports for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Report> GetReportsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<Report>();
            string query = $"SELECT {ReportColumns} FROM Reports WHERE PatientId = @Id ORDER BY ReportDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapReport, parameters);
        }

        /// <summary>
        /// Retrieves recent reports for a specific doctor.
        /// </summary>
        public async Task<List<Report>> GetReportsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Report>();
                string query = $"SELECT TOP 50 {ReportColumns} FROM Reports WHERE DoctorId = @Id ORDER BY ReportDate DESC";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapReport, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving reports for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Report> GetReportsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<Report>();
            string query = $"SELECT TOP 50 {ReportColumns} FROM Reports WHERE DoctorId = @Id ORDER BY ReportDate DESC";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapReport, parameters);
        }

        /// <summary>
        /// Retrieves a paged list of reports for a doctor.
        /// </summary>
        public async Task<List<Report>> GetReportsByDoctorIdPagedAsync(int doctorId, int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ReportDate DESC" : orderBy;
                string query = $@"SELECT {ReportColumns} FROM Reports WHERE DoctorId = @Id 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                var parameters = new[] {
                    new SqlParameter("@Id", doctorId),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return await _db.ExecuteQueryAsync(query, MapReport, parameters);
            }
            catch { return new List<Report>(); }
        }

        public List<Report> GetReportsByDoctorIdPaged(int doctorId, int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "ReportDate DESC" : orderBy;
            string query = $@"SELECT {ReportColumns} FROM Reports WHERE DoctorId = @Id ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Id", doctorId), new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(query, MapReport, parameters);
        }

        /// <summary>
        /// Gets the total count of reports for a doctor.
        /// </summary>
        public async Task<int> GetReportsByDoctorIdCountAsync(int doctorId)
        {
            return await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Reports WHERE DoctorId = @Id",
                new[] { new SqlParameter("@Id", doctorId) });
        }

        public int GetReportsByDoctorIdCount(int doctorId)
        {
            return _db.ExecuteScalar<int>("SELECT COUNT(*) FROM Reports WHERE DoctorId = @Id",
                new[] { new SqlParameter("@Id", doctorId) });
        }

        /// <summary>
        /// Asynchronously creates a new medical report record.
        /// </summary>
        public async Task CreateReportAsync(Report report)
        {
            try
            {
                if (report == null || report.PatientId <= 0 || report.DoctorId <= 0)
                    throw new ArgumentException("Valid Report, Patient and Doctor are required.");

                string query = @"INSERT INTO Reports (PatientId, DoctorId, AppointmentId, ReportName, ReportType, ReportDate, FilePath, Status, Observations) 
                                VALUES (@PatientId, @DoctorId, @AppointmentId, @ReportName, @ReportType, @ReportDate, @FilePath, @Status, @Observations)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", report.PatientId),
                    new SqlParameter("@DoctorId", report.DoctorId),
                    new SqlParameter("@AppointmentId", (object?)report.AppointmentId ?? DBNull.Value),
                    new SqlParameter("@ReportName", report.ReportName ?? "Unnamed Report"),
                    new SqlParameter("@ReportType", report.ReportType ?? "General"),
                    new SqlParameter("@ReportDate", report.ReportDate == default ? DateTime.Now : report.ReportDate),
                    new SqlParameter("@FilePath", report.FilePath ?? ""),
                    new SqlParameter("@Status", report.Status ?? "Final"),
                    new SqlParameter("@Observations", (object?)report.Observations ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create report: {ex.Message}", ex);
            }
        }

        public void CreateReport(Report report)
        {
            if (report == null || report.PatientId <= 0 || report.DoctorId <= 0) throw new ArgumentException("Valid Report, Patient and Doctor are required.");
            string query = @"INSERT INTO Reports (PatientId, DoctorId, AppointmentId, ReportName, ReportType, ReportDate, FilePath, Status, Observations) VALUES (@PatientId, @DoctorId, @AppointmentId, @ReportName, @ReportType, @ReportDate, @FilePath, @Status, @Observations)";
            var parameters = new[] { new SqlParameter("@PatientId", report.PatientId), new SqlParameter("@DoctorId", report.DoctorId), new SqlParameter("@AppointmentId", (object?)report.AppointmentId ?? DBNull.Value), new SqlParameter("@ReportName", report.ReportName ?? "Unnamed Report"), new SqlParameter("@ReportType", report.ReportType ?? "General"), new SqlParameter("@ReportDate", report.ReportDate == default ? DateTime.Now : report.ReportDate), new SqlParameter("@FilePath", report.FilePath ?? ""), new SqlParameter("@Status", report.Status ?? "Final"), new SqlParameter("@Observations", (object?)report.Observations ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Report model.
        /// </summary>
        private Report MapReport(SqlDataReader reader)
        {
            return new Report
            {
                ReportId = reader.GetInt32(reader.GetOrdinal("ReportId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                ReportName = reader["ReportName"]?.ToString() ?? "",
                ReportType = reader["ReportType"]?.ToString() ?? "",
                ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate")),
                FilePath = reader["FilePath"]?.ToString() ?? "",
                Status = reader["Status"]?.ToString() ?? "",
                Observations = reader.IsDBNull(reader.GetOrdinal("Observations")) ? null : reader["Observations"]?.ToString()
            };
        }
    }
}

