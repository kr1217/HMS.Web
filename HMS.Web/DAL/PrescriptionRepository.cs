/*
 * FILE: PrescriptionRepository.cs
 * PURPOSE: Manages digital prescriptions.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Prescriptions.razor, Doctor/Prescribe.razor
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
    /// Repository for managing digital prescriptions.
    /// OPTIMIZATION: [Immutability] Supports 'IsLocked' flag to prevent tampering with issued prescriptions (compliance requirement).
    /// </summary>
    public class PrescriptionRepository
    {
        private readonly DatabaseHelper _db;
        public PrescriptionRepository(DatabaseHelper db) { _db = db; }

        private const string PrescriptionColumns = "p.PrescriptionId, p.PatientId, p.DoctorId, p.AppointmentId, p.Details, p.PrescribedDate, p.Medications, p.IsLocked, p.DigitalSignature, d.FullName as DoctorName";

        /// <summary>
        /// Retrieves all prescriptions for a specific patient.
        /// </summary>
        public async Task<List<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Prescription>();
                string query = $@"SELECT {PrescriptionColumns} 
                                 FROM Prescriptions p 
                                 JOIN Doctors d ON p.DoctorId = d.DoctorId 
                                 WHERE p.PatientId = @Id 
                                 ORDER BY p.PrescribedDate DESC";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapPrescription, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescriptions for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Prescription> GetPrescriptionsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<Prescription>();
            string query = $@"SELECT {PrescriptionColumns} FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.PatientId = @Id ORDER BY p.PrescribedDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapPrescription, parameters);
        }

        /// <summary>
        /// Retrieves all prescriptions written by a specific doctor.
        /// </summary>
        public async Task<List<Prescription>> GetPrescriptionsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Prescription>();
                string query = $@"SELECT {PrescriptionColumns} 
                                 FROM Prescriptions p 
                                 JOIN Doctors d ON p.DoctorId = d.DoctorId 
                                 WHERE p.DoctorId = @Id 
                                 ORDER BY p.PrescribedDate DESC";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapPrescription, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescriptions for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Prescription> GetPrescriptionsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<Prescription>();
            string query = $@"SELECT {PrescriptionColumns} FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.DoctorId = @Id ORDER BY p.PrescribedDate DESC";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapPrescription, parameters);
        }

        /// <summary>
        /// Retrieves a single prescription by its ID.
        /// </summary>
        public async Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId)
        {
            try
            {
                if (prescriptionId <= 0) return null;
                string query = $@"SELECT {PrescriptionColumns} FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.PrescriptionId = @Id";
                var parameters = new[] { new SqlParameter("@Id", prescriptionId) };
                var list = await _db.ExecuteQueryAsync(query, MapPrescription, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescription {prescriptionId}: {ex.Message}", ex);
            }
        }

        public Prescription? GetPrescriptionById(int prescriptionId)
        {
            if (prescriptionId <= 0) return null;
            string query = $@"SELECT {PrescriptionColumns} FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.PrescriptionId = @Id";
            var parameters = new[] { new SqlParameter("@Id", prescriptionId) };
            return _db.ExecuteQuery(query, MapPrescription, parameters).FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously creates a new prescription record.
        /// </summary>
        public async Task CreatePrescriptionAsync(Prescription p)
        {
            try
            {
                if (p == null || p.PatientId <= 0 || p.DoctorId <= 0) throw new ArgumentException("Invalid prescription data.");

                string query = @"INSERT INTO Prescriptions (PatientId, DoctorId, AppointmentId, Details, PrescribedDate, Medications, IsLocked, DigitalSignature) 
                                VALUES (@PatientId, @DoctorId, @AppointmentId, @Details, @PrescribedDate, @Medications, @IsLocked, @DigitalSignature)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", p.PatientId),
                    new SqlParameter("@DoctorId", p.DoctorId),
                    new SqlParameter("@AppointmentId", (object?)p.AppointmentId ?? DBNull.Value),
                    new SqlParameter("@Details", p.Details ?? ""),
                    new SqlParameter("@PrescribedDate", p.PrescribedDate == default ? DateTime.Now : p.PrescribedDate),
                    new SqlParameter("@Medications", p.Medications ?? ""),
                    new SqlParameter("@IsLocked", p.IsLocked),
                    new SqlParameter("@DigitalSignature", (object?)p.DigitalSignature ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create prescription: {ex.Message}", ex);
            }
        }

        public void CreatePrescription(Prescription p)
        {
            if (p == null || p.PatientId <= 0 || p.DoctorId <= 0) throw new ArgumentException("Invalid prescription data.");
            string query = @"INSERT INTO Prescriptions (PatientId, DoctorId, AppointmentId, Details, PrescribedDate, Medications, IsLocked, DigitalSignature) VALUES (@PatientId, @DoctorId, @AppointmentId, @Details, @PrescribedDate, @Medications, @IsLocked, @DigitalSignature)";
            var parameters = new[] { new SqlParameter("@PatientId", p.PatientId), new SqlParameter("@DoctorId", p.DoctorId), new SqlParameter("@AppointmentId", (object?)p.AppointmentId ?? DBNull.Value), new SqlParameter("@Details", p.Details ?? ""), new SqlParameter("@PrescribedDate", p.PrescribedDate == default ? DateTime.Now : p.PrescribedDate), new SqlParameter("@Medications", p.Medications ?? ""), new SqlParameter("@IsLocked", p.IsLocked), new SqlParameter("@DigitalSignature", (object?)p.DigitalSignature ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Prescription model.
        /// </summary>
        private Prescription MapPrescription(SqlDataReader reader)
        {
            var p = new Prescription
            {
                PrescriptionId = reader.GetInt32(reader.GetOrdinal("PrescriptionId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                Details = reader["Details"]?.ToString() ?? "",
                PrescribedDate = reader.GetDateTime(reader.GetOrdinal("PrescribedDate")),
                Medications = reader["Medications"]?.ToString() ?? "",
                IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                DigitalSignature = reader.IsDBNull(reader.GetOrdinal("DigitalSignature")) ? null : reader["DigitalSignature"]?.ToString()
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "DoctorName") p.DoctorName = reader[i]?.ToString() ?? "";
            }

            return p;
        }
    }
}

