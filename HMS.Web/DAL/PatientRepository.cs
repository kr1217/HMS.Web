/*
 * FILE: PatientRepository.cs
 * PURPOSE: Manages patient demographics and profiles.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Dashboard.razor, Doctor/PatientProfile.razor
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
    /// Repository for managing patient-related database operations.
    /// OPTIMIZATION: [Explicit Columns] Uses a constant column list to avoid SELECT * performance hits.
    /// OPTIMIZATION: [Index Covering] Queries are designed to leverage indexes on CNIC and RegistrationDate.
    /// </summary>
    public class PatientRepository
    {
        private readonly DatabaseHelper _db;

        public PatientRepository(DatabaseHelper db)
        {
            _db = db;
        }

        private const string PatientColumns = "PatientId, UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Allergies, ChronicDiseases, CurrentMedications, DisabilityStatus, RegistrationDate, IsActive, PatientType, Email, City, Country, LastVisitDate, PrimaryDoctorId";

        /// <summary>
        /// Retrieves all patients.
        /// </summary>
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            try
            {
                string query = $"SELECT {PatientColumns} FROM Patients ORDER BY RegistrationDate DESC";
                return await _db.ExecuteQueryAsync(query, MapPatient);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all patients: {ex.Message}", ex);
            }
        }

        public List<Patient> GetAllPatients()
        {
            string query = $"SELECT {PatientColumns} FROM Patients ORDER BY RegistrationDate DESC";
            return _db.ExecuteQuery(query, MapPatient);
        }
        /// <summary>
        /// Deactivates a patient record (Soft Delete).
        /// OPTIMIZATION: [Audit Retention] We never physically delete patient data to maintain medical history integrity.
        /// </summary>
        public async Task DeletePatientAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return;
                string query = "UPDATE Patients SET IsActive = 0 WHERE PatientId = @Id";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deactivating patient {patientId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a single patient by their ID.
        /// </summary>
        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            try
            {
                if (id <= 0) return null;
                string query = $"SELECT {PatientColumns} FROM Patients WHERE PatientId = @Id";
                var parameters = new[] { new SqlParameter("@Id", id) };
                var list = await _db.ExecuteQueryAsync(query, MapPatient, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient {id}: {ex.Message}", ex);
            }
        }

        public Patient? GetPatientById(int id)
        {
            if (id <= 0) return null;
            string query = $"SELECT {PatientColumns} FROM Patients WHERE PatientId = @Id";
            var parameters = new[] { new SqlParameter("@Id", id) };
            return _db.ExecuteQuery(query, MapPatient, parameters).FirstOrDefault();
        }

        public async Task<Patient?> GetPatientByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT {PatientColumns} FROM Patients WHERE UserId = @UserId";
            var list = await _db.ExecuteQueryAsync(query, MapPatient, new[] { new SqlParameter("@UserId", userId) });
            return list.FirstOrDefault();
        }

        public Patient? GetPatientByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT {PatientColumns} FROM Patients WHERE UserId = @UserId";
            return _db.ExecuteQuery(query, MapPatient, new[] { new SqlParameter("@UserId", userId) }).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a paged list of patients with optional search functionality.
        /// </summary>
        /// <summary>
        /// Retrieves a paged list of patients with optional search functionality.
        /// OPTIMIZATION: [Server-Side Pagination] Uses OFFSET/FETCH for efficient retrieval of large datasets.
        /// HOW IT WORKS: Dynamically builds the ORDER BY and WHERE clauses based on search criteria and paging parameters.
        /// </summary>
        public async Task<List<Patient>> GetPatientsPagedAsync(int skip, int take, string orderBy, string? searchTerm = null)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "RegistrationDate DESC" : orderBy;
                string whereClause = string.IsNullOrEmpty(searchTerm) ? "" : "WHERE FullName LIKE @Search OR Email LIKE @Search OR ContactNumber LIKE @Search";

                string query = $@"SELECT {PatientColumns} FROM Patients 
                                 {whereClause}
                                 ORDER BY {orderClause} 
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new List<SqlParameter> {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };

                if (!string.IsNullOrEmpty(searchTerm))
                    parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));

                return await _db.ExecuteQueryAsync(query, MapPatient, parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged patients: {ex.Message}", ex);
            }
        }

        public List<Patient> GetPatientsPaged(int skip, int take, string orderBy, string? searchTerm = null)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "RegistrationDate DESC" : orderBy;
            string whereClause = string.IsNullOrEmpty(searchTerm) ? "" : "WHERE FullName LIKE @Search OR Email LIKE @Search OR ContactNumber LIKE @Search";
            string query = $@"SELECT {PatientColumns} FROM Patients {whereClause} ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new List<SqlParameter> { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            if (!string.IsNullOrEmpty(searchTerm)) parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));
            return _db.ExecuteQuery(query, MapPatient, parameters.ToArray());
        }

        /// <summary>
        /// Gets the total count of patients, with optional filtering.
        /// </summary>
        public async Task<int> GetPatientsCountAsync(string? searchTerm = null)
        {
            string whereClause = string.IsNullOrEmpty(searchTerm) ? "" : "WHERE FullName LIKE @Search OR Email LIKE @Search OR ContactNumber LIKE @Search";
            string query = $"SELECT COUNT(*) FROM Patients {whereClause}";

            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(searchTerm))
                parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));

            return await _db.ExecuteScalarAsync<int>(query, parameters.ToArray());
        }

        public int GetPatientsCount(string? searchTerm = null)
        {
            string whereClause = string.IsNullOrEmpty(searchTerm) ? "" : "WHERE FullName LIKE @Search OR Email LIKE @Search OR ContactNumber LIKE @Search";
            string query = $"SELECT COUNT(*) FROM Patients {whereClause}";
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(searchTerm)) parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));
            return _db.ExecuteScalar<int>(query, parameters.ToArray());
        }

        /// <summary>
        /// Asynchronously creates a new patient record.
        /// </summary>
        public async Task CreatePatientAsync(Patient p)
        {
            try
            {
                if (p == null) throw new ArgumentNullException(nameof(p));

                string query = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Allergies, ChronicDiseases, CurrentMedications, DisabilityStatus, RegistrationDate, IsActive, PatientType, Email, City, Country, LastVisitDate, PrimaryDoctorId) 
                                VALUES (@UserId, @FullName, @DOB, @Gender, @Phone, @Address, @CNIC, @Blood, @Marital, @EName, @ENumber, @ERel, @Allergies, @Chronic, @Meds, @Disability, @Created, @Active, @Type, @Email, @City, @Country, @LastVisit, @DoctorId)";
                var parameters = new[]
                {
                    new SqlParameter("@UserId", p.UserId ?? (object)DBNull.Value),
                    new SqlParameter("@FullName", p.FullName ?? ""),
                    new SqlParameter("@DOB", (object?)p.DateOfBirth ?? DBNull.Value),
                    new SqlParameter("@Gender", p.Gender ?? ""),
                    new SqlParameter("@Phone", p.ContactNumber ?? ""),
                    new SqlParameter("@Address", p.Address ?? ""),
                    new SqlParameter("@CNIC", p.CNIC ?? ""),
                    new SqlParameter("@Blood", p.BloodGroup ?? ""),
                    new SqlParameter("@Marital", p.MaritalStatus ?? ""),
                    new SqlParameter("@EName", p.EmergencyContactName ?? ""),
                    new SqlParameter("@ENumber", p.EmergencyContactNumber ?? ""),
                    new SqlParameter("@ERel", p.RelationshipToEmergencyContact ?? ""),
                    new SqlParameter("@Allergies", (object?)p.Allergies ?? DBNull.Value),
                    new SqlParameter("@Chronic", (object?)p.ChronicDiseases ?? DBNull.Value),
                    new SqlParameter("@Meds", (object?)p.CurrentMedications ?? DBNull.Value),
                    new SqlParameter("@Disability", (object?)p.DisabilityStatus ?? DBNull.Value),
                    new SqlParameter("@Created", p.RegistrationDate),
                    new SqlParameter("@Active", p.IsActive),
                    new SqlParameter("@Type", (object?)p.PatientType ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)p.Email ?? DBNull.Value),
                    new SqlParameter("@City", (object?)p.City ?? DBNull.Value),
                    new SqlParameter("@Country", (object?)p.Country ?? DBNull.Value),
                    new SqlParameter("@LastVisit", (object?)p.LastVisitDate ?? DBNull.Value),
                    new SqlParameter("@DoctorId", (object?)p.PrimaryDoctorId ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create patient record: {ex.Message}", ex);
            }
        }

        public void CreatePatient(Patient p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            string query = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Allergies, ChronicDiseases, CurrentMedications, DisabilityStatus, RegistrationDate, IsActive, PatientType, Email, City, Country, LastVisitDate, PrimaryDoctorId) VALUES (@UserId, @FullName, @DOB, @Gender, @Phone, @Address, @CNIC, @Blood, @Marital, @EName, @ENumber, @ERel, @Allergies, @Chronic, @Meds, @Disability, @Created, @Active, @Type, @Email, @City, @Country, @LastVisit, @DoctorId)";
            var parameters = new[] { new SqlParameter("@UserId", p.UserId ?? (object)DBNull.Value), new SqlParameter("@FullName", p.FullName ?? ""), new SqlParameter("@DOB", (object?)p.DateOfBirth ?? DBNull.Value), new SqlParameter("@Gender", p.Gender ?? ""), new SqlParameter("@Phone", p.ContactNumber ?? ""), new SqlParameter("@Address", p.Address ?? ""), new SqlParameter("@CNIC", p.CNIC ?? ""), new SqlParameter("@Blood", p.BloodGroup ?? ""), new SqlParameter("@Marital", p.MaritalStatus ?? ""), new SqlParameter("@EName", p.EmergencyContactName ?? ""), new SqlParameter("@ENumber", p.EmergencyContactNumber ?? ""), new SqlParameter("@ERel", p.RelationshipToEmergencyContact ?? ""), new SqlParameter("@Allergies", (object?)p.Allergies ?? DBNull.Value), new SqlParameter("@Chronic", (object?)p.ChronicDiseases ?? DBNull.Value), new SqlParameter("@Meds", (object?)p.CurrentMedications ?? DBNull.Value), new SqlParameter("@Disability", (object?)p.DisabilityStatus ?? DBNull.Value), new SqlParameter("@Created", p.RegistrationDate), new SqlParameter("@Active", p.IsActive), new SqlParameter("@Type", (object?)p.PatientType ?? DBNull.Value), new SqlParameter("@Email", (object?)p.Email ?? DBNull.Value), new SqlParameter("@City", (object?)p.City ?? DBNull.Value), new SqlParameter("@Country", (object?)p.Country ?? DBNull.Value), new SqlParameter("@LastVisit", (object?)p.LastVisitDate ?? DBNull.Value), new SqlParameter("@DoctorId", (object?)p.PrimaryDoctorId ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Asynchronously updates an existing patient record.
        /// </summary>
        public async Task UpdatePatientAsync(Patient p)
        {
            try
            {
                if (p == null || p.PatientId <= 0) throw new ArgumentException("Invalid patient data for update.");

                string query = @"UPDATE Patients 
                                SET FullName = @FullName, 
                                    DateOfBirth = @DOB, 
                                    Gender = @Gender, 
                                    ContactNumber = @Phone,
                                    Address = @Address, 
                                    CNIC = @CNIC,
                                    BloodGroup = @Blood,
                                    MaritalStatus = @Marital,
                                    EmergencyContactName = @EName,
                                    EmergencyContactNumber = @ENumber,
                                    RelationshipToEmergencyContact = @ERel,
                                    Allergies = @Allergies,
                                    ChronicDiseases = @Chronic,
                                    CurrentMedications = @Meds,
                                    DisabilityStatus = @Disability,
                                    IsActive = @Active,
                                    Email = @Email,
                                    City = @City,
                                    Country = @Country
                                WHERE PatientId = @Id";
                var parameters = new[]
                {
                    new SqlParameter("@FullName", p.FullName ?? ""),
                    new SqlParameter("@DOB", (object?)p.DateOfBirth ?? DBNull.Value),
                    new SqlParameter("@Gender", p.Gender ?? ""),
                    new SqlParameter("@Phone", p.ContactNumber ?? ""),
                    new SqlParameter("@Address", p.Address ?? ""),
                    new SqlParameter("@CNIC", p.CNIC ?? ""),
                    new SqlParameter("@Blood", p.BloodGroup ?? ""),
                    new SqlParameter("@Marital", p.MaritalStatus ?? ""),
                    new SqlParameter("@EName", p.EmergencyContactName ?? ""),
                    new SqlParameter("@ENumber", p.EmergencyContactNumber ?? ""),
                    new SqlParameter("@ERel", p.RelationshipToEmergencyContact ?? ""),
                    new SqlParameter("@Allergies", (object?)p.Allergies ?? DBNull.Value),
                    new SqlParameter("@Chronic", (object?)p.ChronicDiseases ?? DBNull.Value),
                    new SqlParameter("@Meds", (object?)p.CurrentMedications ?? DBNull.Value),
                    new SqlParameter("@Disability", (object?)p.DisabilityStatus ?? DBNull.Value),
                    new SqlParameter("@Active", p.IsActive),
                    new SqlParameter("@Email", (object?)p.Email ?? DBNull.Value),
                    new SqlParameter("@City", (object?)p.City ?? DBNull.Value),
                    new SqlParameter("@Country", (object?)p.Country ?? DBNull.Value),
                    new SqlParameter("@Id", p.PatientId)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update patient {p?.PatientId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates an existing patient record (Synchronous).
        /// </summary>
        public void UpdatePatient(Patient p)
        {
            if (p == null || p.PatientId <= 0) throw new ArgumentException("Invalid patient data for update.");
            string query = @"UPDATE Patients SET FullName = @FullName, DateOfBirth = @DOB, Gender = @Gender, ContactNumber = @Phone, Address = @Address, CNIC = @CNIC, BloodGroup = @Blood, MaritalStatus = @Marital, EmergencyContactName = @EName, EmergencyContactNumber = @ENumber, RelationshipToEmergencyContact = @ERel, Allergies = @Allergies, ChronicDiseases = @Chronic, CurrentMedications = @Meds, DisabilityStatus = @Disability, IsActive = @Active, Email = @Email, City = @City, Country = @Country WHERE PatientId = @Id";
            var parameters = new[] { new SqlParameter("@FullName", p.FullName ?? ""), new SqlParameter("@DOB", (object?)p.DateOfBirth ?? DBNull.Value), new SqlParameter("@Gender", p.Gender ?? ""), new SqlParameter("@Phone", p.ContactNumber ?? ""), new SqlParameter("@Address", p.Address ?? ""), new SqlParameter("@CNIC", p.CNIC ?? ""), new SqlParameter("@Blood", p.BloodGroup ?? ""), new SqlParameter("@Marital", p.MaritalStatus ?? ""), new SqlParameter("@EName", p.EmergencyContactName ?? ""), new SqlParameter("@ENumber", p.EmergencyContactNumber ?? ""), new SqlParameter("@ERel", p.RelationshipToEmergencyContact ?? ""), new SqlParameter("@Allergies", (object?)p.Allergies ?? DBNull.Value), new SqlParameter("@Chronic", (object?)p.ChronicDiseases ?? DBNull.Value), new SqlParameter("@Meds", (object?)p.CurrentMedications ?? DBNull.Value), new SqlParameter("@Disability", (object?)p.DisabilityStatus ?? DBNull.Value), new SqlParameter("@Active", p.IsActive), new SqlParameter("@Email", (object?)p.Email ?? DBNull.Value), new SqlParameter("@City", (object?)p.City ?? DBNull.Value), new SqlParameter("@Country", (object?)p.Country ?? DBNull.Value), new SqlParameter("@Id", p.PatientId) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Patient model.
        /// </summary>
        /// <summary>
        /// Mapping logic from SqlDataReader to Patient model.
        /// OPTIMIZATION: [Forward-Only Reading] Uses GetOrdinal and IsDBNull for high-performance data extraction.
        /// </summary>
        private Patient MapPatient(SqlDataReader reader)
        {
            return new Patient
            {
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                UserId = reader["UserId"]?.ToString() ?? "",
                FullName = reader["FullName"]?.ToString() ?? "",
                DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                Gender = reader["Gender"]?.ToString() ?? "",
                ContactNumber = reader["ContactNumber"]?.ToString() ?? "",
                Address = reader["Address"]?.ToString() ?? "",
                CNIC = reader["CNIC"]?.ToString() ?? "",
                BloodGroup = reader["BloodGroup"]?.ToString() ?? "",
                MaritalStatus = reader["MaritalStatus"]?.ToString() ?? "",
                EmergencyContactName = reader["EmergencyContactName"]?.ToString() ?? "",
                EmergencyContactNumber = reader["EmergencyContactNumber"]?.ToString() ?? "",
                RelationshipToEmergencyContact = reader["RelationshipToEmergencyContact"]?.ToString() ?? "",
                Allergies = reader.IsDBNull(reader.GetOrdinal("Allergies")) ? null : reader["Allergies"]?.ToString(),
                ChronicDiseases = reader.IsDBNull(reader.GetOrdinal("ChronicDiseases")) ? null : reader["ChronicDiseases"]?.ToString(),
                CurrentMedications = reader.IsDBNull(reader.GetOrdinal("CurrentMedications")) ? null : reader["CurrentMedications"]?.ToString(),
                DisabilityStatus = reader.IsDBNull(reader.GetOrdinal("DisabilityStatus")) ? null : reader["DisabilityStatus"]?.ToString(),
                RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                PatientType = reader.IsDBNull(reader.GetOrdinal("PatientType")) ? null : reader["PatientType"]?.ToString(),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"]?.ToString(),
                City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader["City"]?.ToString(),
                Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader["Country"]?.ToString(),
                LastVisitDate = reader.IsDBNull(reader.GetOrdinal("LastVisitDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("LastVisitDate")),
                PrimaryDoctorId = reader.IsDBNull(reader.GetOrdinal("PrimaryDoctorId")) ? null : reader["PrimaryDoctorId"]?.ToString()
            };
        }
    }
}

