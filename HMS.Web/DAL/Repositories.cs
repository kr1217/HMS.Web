using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace HMS.Web.DAL
{
    public class PatientRepository
    {
        private readonly DatabaseHelper _db;

        public PatientRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public Patient? GetPatientByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;

                string query = "SELECT * FROM Patients WHERE UserId = @UserId";
                var parameters = new[] { new SqlParameter("@UserId", userId) };
                var table = _db.ExecuteDataTable(query, parameters);

                if (table != null && table.Rows.Count > 0)
                {
                    return MapPatient(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient by UserId {userId}: {ex.Message}", ex);
            }
        }

        public List<Patient> GetAllPatients()
        {
            try
            {
                // Top 100 for safety
                string query = "SELECT TOP 100 * FROM Patients WHERE IsActive = 1 ORDER BY FullName";
                return _db.ExecuteQuery(query, MapPatient);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all patients: {ex.Message}", ex);
            }
        }

        public List<Patient> GetPatientsPaged(int skip, int take, string orderBy, string? searchTerm = null)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "FullName" : orderBy;
                string whereClause = "WHERE IsActive = 1";
                var parameters = new List<SqlParameter> {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    whereClause += " AND (FullName LIKE @Search OR ContactNumber LIKE @Search OR CNIC LIKE @Search)";
                    parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));
                }

                string sql = $@"SELECT * FROM Patients {whereClause} 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                return _db.ExecuteQuery(sql, MapPatient, parameters.ToArray());
            }
            catch { return new List<Patient>(); }
        }

        public int GetPatientsCount(string? searchTerm = null)
        {
            string whereClause = "WHERE IsActive = 1";
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClause += " AND (FullName LIKE @Search OR ContactNumber LIKE @Search OR CNIC LIKE @Search)";
                parameters.Add(new SqlParameter("@Search", $"%{searchTerm}%"));
            }
            return Convert.ToInt32(_db.ExecuteScalar($"SELECT COUNT(*) FROM Patients {whereClause}", parameters.ToArray()) ?? 0);
        }

        private Patient MapPatient(DataRow row)
        {
            return new Patient
            {
                PatientId = (int)row["PatientId"],
                UserId = row["UserId"]?.ToString() ?? "",
                FullName = row["FullName"]?.ToString() ?? "",
                DateOfBirth = row["DateOfBirth"] == DBNull.Value ? null : (DateTime?)row["DateOfBirth"],
                Gender = row["Gender"]?.ToString() ?? "",
                ContactNumber = row["ContactNumber"]?.ToString() ?? "",
                Address = row["Address"]?.ToString() ?? "",
                CNIC = row["CNIC"]?.ToString() ?? "",
                BloodGroup = row["BloodGroup"]?.ToString() ?? "",
                MaritalStatus = row["MaritalStatus"]?.ToString() ?? "",
                EmergencyContactName = row["EmergencyContactName"]?.ToString() ?? "",
                EmergencyContactNumber = row["EmergencyContactNumber"]?.ToString() ?? "",
                RelationshipToEmergencyContact = row["RelationshipToEmergencyContact"]?.ToString() ?? "",
                Allergies = row["Allergies"] == DBNull.Value ? null : row["Allergies"]?.ToString(),
                ChronicDiseases = row["ChronicDiseases"] == DBNull.Value ? null : row["ChronicDiseases"]?.ToString(),
                CurrentMedications = row["CurrentMedications"] == DBNull.Value ? null : row["CurrentMedications"]?.ToString(),
                DisabilityStatus = row["DisabilityStatus"] == DBNull.Value ? null : row["DisabilityStatus"]?.ToString(),
                RegistrationDate = row["RegistrationDate"] == DBNull.Value ? DateTime.Now : (DateTime)row["RegistrationDate"],
                IsActive = row["IsActive"] == DBNull.Value ? true : (bool)row["IsActive"],
                PatientType = row["PatientType"] == DBNull.Value ? null : row["PatientType"]?.ToString(),
                Email = row["Email"] == DBNull.Value ? null : row["Email"]?.ToString(),
                City = row["City"] == DBNull.Value ? null : row["City"]?.ToString(),
                Country = row["Country"] == DBNull.Value ? null : row["Country"]?.ToString(),
                LastVisitDate = row["LastVisitDate"] == DBNull.Value ? null : (DateTime?)row["LastVisitDate"],
                PrimaryDoctorId = row["PrimaryDoctorId"] == DBNull.Value ? null : row["PrimaryDoctorId"]?.ToString()
            };
        }

        public Patient? GetPatientById(int patientId)
        {
            try
            {
                if (patientId <= 0) return null;
                string query = "SELECT * FROM Patients WHERE PatientId = @PatientId";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                var table = _db.ExecuteDataTable(query, parameters);

                if (table != null && table.Rows.Count > 0)
                {
                    return MapPatient(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient by ID {patientId}: {ex.Message}", ex);
            }
        }

        public void CreatePatient(Patient patient)
        {
            try
            {
                if (patient == null) throw new ArgumentNullException(nameof(patient));
                if (string.IsNullOrEmpty(patient.FullName)) throw new ArgumentException("Patient Full Name is required.");

                string query = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, 
                                EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Allergies, ChronicDiseases, 
                                CurrentMedications, DisabilityStatus, RegistrationDate, IsActive, PatientType, Email, City, Country, LastVisitDate, PrimaryDoctorId) 
                                VALUES (@UserId, @FullName, @DateOfBirth, @Gender, @ContactNumber, @Address, @CNIC, @BloodGroup, @MaritalStatus, 
                                @EmergencyContactName, @EmergencyContactNumber, @RelationshipToEmergencyContact, @Allergies, @ChronicDiseases, 
                                @CurrentMedications, @DisabilityStatus, @RegistrationDate, @IsActive, @PatientType, @Email, @City, @Country, @LastVisitDate, @PrimaryDoctorId)";
                var parameters = new[]
                {
                    new SqlParameter("@UserId", patient.UserId ?? ""),
                    new SqlParameter("@FullName", patient.FullName),
                    new SqlParameter("@DateOfBirth", (object?)patient.DateOfBirth ?? DBNull.Value),
                    new SqlParameter("@Gender", patient.Gender ?? ""),
                    new SqlParameter("@ContactNumber", patient.ContactNumber ?? ""),
                    new SqlParameter("@Address", patient.Address ?? ""),
                    new SqlParameter("@CNIC", patient.CNIC ?? ""),
                    new SqlParameter("@BloodGroup", patient.BloodGroup ?? ""),
                    new SqlParameter("@MaritalStatus", patient.MaritalStatus ?? ""),
                    new SqlParameter("@EmergencyContactName", patient.EmergencyContactName ?? ""),
                    new SqlParameter("@EmergencyContactNumber", patient.EmergencyContactNumber ?? ""),
                    new SqlParameter("@RelationshipToEmergencyContact", patient.RelationshipToEmergencyContact ?? ""),
                    new SqlParameter("@Allergies", (object?)patient.Allergies ?? DBNull.Value),
                    new SqlParameter("@ChronicDiseases", (object?)patient.ChronicDiseases ?? DBNull.Value),
                    new SqlParameter("@CurrentMedications", (object?)patient.CurrentMedications ?? DBNull.Value),
                    new SqlParameter("@DisabilityStatus", (object?)patient.DisabilityStatus ?? DBNull.Value),
                    new SqlParameter("@RegistrationDate", patient.RegistrationDate == default ? DateTime.Now : patient.RegistrationDate),
                    new SqlParameter("@IsActive", patient.IsActive),
                    new SqlParameter("@PatientType", (object?)patient.PatientType ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)patient.Email ?? DBNull.Value),
                    new SqlParameter("@City", (object?)patient.City ?? DBNull.Value),
                    new SqlParameter("@Country", (object?)patient.Country ?? DBNull.Value),
                    new SqlParameter("@LastVisitDate", (object?)patient.LastVisitDate ?? DBNull.Value),
                    new SqlParameter("@PrimaryDoctorId", (object?)patient.PrimaryDoctorId ?? DBNull.Value)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create patient record: {ex.Message}", ex);
            }
        }

        public void UpdatePatient(Patient patient)
        {
            try
            {
                if (patient == null || patient.PatientId <= 0) throw new ArgumentException("Invalid patient record for update.");

                string query = @"UPDATE Patients SET FullName=@FullName, DateOfBirth=@DateOfBirth, Gender=@Gender, ContactNumber=@ContactNumber, Address=@Address, 
                                 CNIC=@CNIC, BloodGroup=@BloodGroup, MaritalStatus=@MaritalStatus, EmergencyContactName=@EmergencyContactName, 
                                 EmergencyContactNumber=@EmergencyContactNumber, RelationshipToEmergencyContact=@RelationshipToEmergencyContact, 
                                 Allergies=@Allergies, ChronicDiseases=@ChronicDiseases, CurrentMedications=@CurrentMedications, 
                                 DisabilityStatus=@DisabilityStatus, RegistrationDate=@RegistrationDate, IsActive=@IsActive, 
                                 PatientType=@PatientType, Email=@Email, City=@City, Country=@Country, LastVisitDate=@LastVisitDate, 
                                 PrimaryDoctorId=@PrimaryDoctorId WHERE PatientId=@PatientId";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", patient.PatientId),
                    new SqlParameter("@FullName", patient.FullName ?? ""),
                    new SqlParameter("@DateOfBirth", (object?)patient.DateOfBirth ?? DBNull.Value),
                    new SqlParameter("@Gender", patient.Gender ?? ""),
                    new SqlParameter("@ContactNumber", patient.ContactNumber ?? ""),
                    new SqlParameter("@Address", patient.Address ?? ""),
                    new SqlParameter("@CNIC", patient.CNIC ?? ""),
                    new SqlParameter("@BloodGroup", patient.BloodGroup ?? ""),
                    new SqlParameter("@MaritalStatus", patient.MaritalStatus ?? ""),
                    new SqlParameter("@EmergencyContactName", patient.EmergencyContactName ?? ""),
                    new SqlParameter("@EmergencyContactNumber", patient.EmergencyContactNumber ?? ""),
                    new SqlParameter("@RelationshipToEmergencyContact", patient.RelationshipToEmergencyContact ?? ""),
                    new SqlParameter("@Allergies", (object?)patient.Allergies ?? DBNull.Value),
                    new SqlParameter("@ChronicDiseases", (object?)patient.ChronicDiseases ?? DBNull.Value),
                    new SqlParameter("@CurrentMedications", (object?)patient.CurrentMedications ?? DBNull.Value),
                    new SqlParameter("@DisabilityStatus", (object?)patient.DisabilityStatus ?? DBNull.Value),
                    new SqlParameter("@RegistrationDate", patient.RegistrationDate),
                    new SqlParameter("@IsActive", patient.IsActive),
                    new SqlParameter("@PatientType", (object?)patient.PatientType ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)patient.Email ?? DBNull.Value),
                    new SqlParameter("@City", (object?)patient.City ?? DBNull.Value),
                    new SqlParameter("@Country", (object?)patient.Country ?? DBNull.Value),
                    new SqlParameter("@LastVisitDate", (object?)patient.LastVisitDate ?? DBNull.Value),
                    new SqlParameter("@PrimaryDoctorId", (object?)patient.PrimaryDoctorId ?? DBNull.Value)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update patient {patient?.PatientId}: {ex.Message}", ex);
            }
        }
    }

    public class DoctorRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorRepository(DatabaseHelper db) { _db = db; }

        public Doctor? GetDoctorByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;
                string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE UserId = @UserId";
                var parameters = new[] { new SqlParameter("@UserId", userId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapDoctor(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor by UserId {userId}: {ex.Message}", ex);
            }
        }

        public Doctor? GetDoctorById(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return null;
                string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE DoctorId = @DoctorId";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapDoctor(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor by ID {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Doctor> GetDoctors()
        {
            try
            {
                string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE d.IsActive = 1 ORDER BY d.FullName";
                return _db.ExecuteQuery(query, MapDoctor);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctors list: {ex.Message}", ex);
            }
        }

        public DoctorDashboardStats GetDoctorDashboardStats(int doctorId)
        {
            try
            {
                var stats = new DoctorDashboardStats();

                // Today's Appointments
                stats.AppointmentsToday = Convert.ToInt32(_db.ExecuteScalar(@"
                    SELECT COUNT(*) FROM Appointments 
                    WHERE DoctorId = @Id AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)",
                    new[] { new SqlParameter("@Id", doctorId) }) ?? 0);

                // Pending Approvals
                stats.PendingApprovals = Convert.ToInt32(_db.ExecuteScalar(@"
                    SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND Status = 'Pending'",
                    new[] { new SqlParameter("@Id", doctorId) }) ?? 0);

                // Total Patients Served (Unique patients with completed appointments)
                stats.TotalPatientsServed = Convert.ToInt32(_db.ExecuteScalar(@"
                    SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @Id",
                    new[] { new SqlParameter("@Id", doctorId) }) ?? 0);

                // Pending Reports
                stats.PendingReports = Convert.ToInt32(_db.ExecuteScalar(@"
                    SELECT COUNT(*) FROM Reports WHERE DoctorId = @Id AND Status = 'Draft'",
                    new[] { new SqlParameter("@Id", doctorId) }) ?? 0);

                // Monthly Commission (Sum of consultation fees from completed appointments this month * commission rate)
                // Note: Assuming commission calculation logic here for the dashboard
                stats.MonthlyCommission = Convert.ToDecimal(_db.ExecuteScalar(@"
                    SELECT ISNULL(SUM(ConsultationFee * (CommissionRate/100.0)), 0)
                    FROM Appointments a
                    JOIN Doctors d ON a.DoctorId = d.DoctorId
                    WHERE a.DoctorId = @Id AND a.Status = 'Completed' 
                    AND MONTH(a.AppointmentDate) = MONTH(GETDATE()) 
                    AND YEAR(a.AppointmentDate) = YEAR(GETDATE())",
                    new[] { new SqlParameter("@Id", doctorId) }) ?? 0);

                return stats;
            }
            catch { return new DoctorDashboardStats(); }
        }

        public void CreateDoctor(Doctor doc)
        {
            try
            {
                if (doc == null) throw new ArgumentNullException(nameof(doc));
                if (string.IsNullOrEmpty(doc.FullName)) throw new ArgumentException("Doctor Full Name is required.");

                string query = @"INSERT INTO Doctors (UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization, 
                                MedicalLicenseNumber, LicenseIssuingAuthority, YearsOfExperience, DepartmentId, HospitalJoiningDate, 
                                ConsultationFee, FollowUpFee, AvailableDays, AvailableTimeSlots, RoomNumber, IsOnCall, IsActive, IsVerified, CreatedAt, IsAvailable) 
                                VALUES (@UserId, @FullName, @Gender, @ContactNumber, @Email, @Qualification, @Specialization, 
                                @MedicalLicenseNumber, @LicenseIssuingAuthority, @YearsOfExperience, @DepartmentId, @HospitalJoiningDate, 
                                @ConsultationFee, @FollowUpFee, @AvailableDays, @AvailableTimeSlots, @RoomNumber, @IsOnCall, @IsActive, @IsVerified, @CreatedAt, @IsAvailable)";
                var parameters = new[]
                {
                    new SqlParameter("@UserId", (object?)doc.UserId ?? DBNull.Value),
                    new SqlParameter("@FullName", doc.FullName),
                    new SqlParameter("@Gender", (object?)doc.Gender ?? DBNull.Value),
                    new SqlParameter("@ContactNumber", (object?)doc.ContactNumber ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)doc.Email ?? DBNull.Value),
                    new SqlParameter("@Qualification", (object?)doc.Qualification ?? DBNull.Value),
                    new SqlParameter("@Specialization", (object?)doc.Specialization ?? DBNull.Value),
                    new SqlParameter("@MedicalLicenseNumber", (object?)doc.MedicalLicenseNumber ?? DBNull.Value),
                    new SqlParameter("@LicenseIssuingAuthority", (object?)doc.LicenseIssuingAuthority ?? DBNull.Value),
                    new SqlParameter("@YearsOfExperience", doc.YearsOfExperience),
                    new SqlParameter("@DepartmentId", doc.DepartmentId),
                    new SqlParameter("@HospitalJoiningDate", doc.HospitalJoiningDate),
                    new SqlParameter("@ConsultationFee", doc.ConsultationFee),
                    new SqlParameter("@FollowUpFee", doc.FollowUpFee),
                    new SqlParameter("@AvailableDays", (object?)doc.AvailableDays ?? DBNull.Value),
                    new SqlParameter("@AvailableTimeSlots", (object?)doc.AvailableTimeSlots ?? DBNull.Value),
                    new SqlParameter("@RoomNumber", (object?)doc.RoomNumber ?? DBNull.Value),
                    new SqlParameter("@IsOnCall", doc.IsOnCall),
                    new SqlParameter("@IsActive", doc.IsActive),
                    new SqlParameter("@IsVerified", doc.IsVerified),
                    new SqlParameter("@CreatedAt", doc.CreatedAt == default ? DateTime.Now : doc.CreatedAt),
                    new SqlParameter("@IsAvailable", doc.IsAvailable)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create doctor record: {ex.Message}", ex);
            }
        }

        public void UpdateDoctor(Doctor doc)
        {
            try
            {
                if (doc == null || doc.DoctorId <= 0) throw new ArgumentException("Invalid doctor record for update.");

                string query = @"UPDATE Doctors SET FullName=@FullName, Gender=@Gender, ContactNumber=@ContactNumber, Email=@Email, 
                                Qualification=@Qualification, Specialization=@Specialization, MedicalLicenseNumber=@MedicalLicenseNumber, 
                                LicenseIssuingAuthority=@LicenseIssuingAuthority, YearsOfExperience=@YearsOfExperience, 
                                DepartmentId=@DepartmentId, HospitalJoiningDate=@HospitalJoiningDate, ConsultationFee=@ConsultationFee, 
                                FollowUpFee=@FollowUpFee, AvailableDays=@AvailableDays, AvailableTimeSlots=@AvailableTimeSlots, 
                                RoomNumber=@RoomNumber, IsOnCall=@IsOnCall, IsActive=@IsActive, IsVerified=@IsVerified, IsAvailable=@IsAvailable 
                                WHERE DoctorId=@DoctorId";
                var parameters = new[]
                {
                    new SqlParameter("@DoctorId", doc.DoctorId),
                    new SqlParameter("@FullName", doc.FullName ?? ""),
                    new SqlParameter("@Gender", doc.Gender ?? ""),
                    new SqlParameter("@ContactNumber", doc.ContactNumber ?? ""),
                    new SqlParameter("@Email", doc.Email ?? ""),
                    new SqlParameter("@Qualification", doc.Qualification ?? ""),
                    new SqlParameter("@Specialization", doc.Specialization ?? ""),
                    new SqlParameter("@MedicalLicenseNumber", doc.MedicalLicenseNumber ?? ""),
                    new SqlParameter("@LicenseIssuingAuthority", doc.LicenseIssuingAuthority ?? ""),
                    new SqlParameter("@YearsOfExperience", doc.YearsOfExperience),
                    new SqlParameter("@DepartmentId", doc.DepartmentId),
                    new SqlParameter("@HospitalJoiningDate", doc.HospitalJoiningDate),
                    new SqlParameter("@ConsultationFee", doc.ConsultationFee),
                    new SqlParameter("@FollowUpFee", doc.FollowUpFee),
                    new SqlParameter("@AvailableDays", doc.AvailableDays ?? ""),
                    new SqlParameter("@AvailableTimeSlots", doc.AvailableTimeSlots ?? ""),
                    new SqlParameter("@RoomNumber", doc.RoomNumber ?? ""),
                    new SqlParameter("@IsOnCall", doc.IsOnCall),
                    new SqlParameter("@IsActive", doc.IsActive),
                    new SqlParameter("@IsVerified", doc.IsVerified),
                    new SqlParameter("@IsAvailable", doc.IsAvailable)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update doctor {doc?.DoctorId}: {ex.Message}", ex);
            }
        }

        private Doctor MapDoctor(DataRow row)
        {
            return new Doctor
            {
                DoctorId = (int)row["DoctorId"],
                UserId = row["UserId"]?.ToString() ?? "",
                FullName = row["FullName"]?.ToString() ?? "",
                Gender = row["Gender"]?.ToString() ?? "",
                ContactNumber = row["ContactNumber"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? "",
                Qualification = row["Qualification"]?.ToString() ?? "",
                Specialization = row["Specialization"]?.ToString() ?? "",
                MedicalLicenseNumber = row["MedicalLicenseNumber"]?.ToString() ?? "",
                LicenseIssuingAuthority = row["LicenseIssuingAuthority"]?.ToString() ?? "",
                YearsOfExperience = (int)row["YearsOfExperience"],
                DepartmentId = (int)row["DepartmentId"],
                DepartmentName = row["DepartmentName"]?.ToString() ?? "",
                HospitalJoiningDate = (DateTime)row["HospitalJoiningDate"],
                ConsultationFee = row["ConsultationFee"] != DBNull.Value ? (decimal)row["ConsultationFee"] : 0,
                FollowUpFee = row["FollowUpFee"] != DBNull.Value ? (decimal)row["FollowUpFee"] : 0,
                AvailableDays = row["AvailableDays"]?.ToString() ?? "",
                AvailableTimeSlots = row["AvailableTimeSlots"]?.ToString() ?? "",
                RoomNumber = row["RoomNumber"]?.ToString() ?? "",
                IsOnCall = (bool)row["IsOnCall"],
                IsActive = (bool)row["IsActive"],
                IsVerified = (bool)row["IsVerified"],
                CreatedAt = (DateTime)row["CreatedAt"],
                IsAvailable = (bool)row["IsAvailable"]
            };
        }
    }

    public class AppointmentRepository
    {
        private readonly DatabaseHelper _db;
        public AppointmentRepository(DatabaseHelper db) { _db = db; }

        public List<Appointment> GetAppointmentsByPatientId(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Appointment>();
                string query = @"
                    SELECT a.*, d.FullName as DoctorName, dep.DepartmentName, p.FullName as PatientName
                    FROM Appointments a
                    JOIN Doctors d ON a.DoctorId = d.DoctorId
                    JOIN Departments dep ON d.DepartmentId = dep.DepartmentId
                    JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.PatientId = @PatientId
                    ORDER BY a.AppointmentDate DESC";

                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                return _db.ExecuteQuery(query, MapAppointment, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointments for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Appointment> GetAppointmentsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Appointment>();
                // Top 20 by default for quick view
                string query = @"
                    SELECT TOP 20 a.*, p.FullName as PatientName
                    FROM Appointments a
                    JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId
                    ORDER BY a.AppointmentDate DESC";

                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                return _db.ExecuteQuery(query, MapAppointment, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving appointments for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Appointment> GetAppointmentsByDoctorIdPaged(int doctorId, int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "AppointmentDate DESC" : orderBy;
                string query = $@"
                    SELECT a.*, p.FullName as PatientName
                    FROM Appointments a
                    JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId
                    ORDER BY {orderClause}
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@DoctorId", doctorId),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return _db.ExecuteQuery(query, MapAppointment, parameters);
            }
            catch { return new List<Appointment>(); }
        }

        public int GetAppointmentsByDoctorIdCount(int doctorId)
        {
            return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id",
                new[] { new SqlParameter("@Id", doctorId) }) ?? 0);
        }

        public void CreateAppointment(Appointment appt)
        {
            try
            {
                if (appt == null) throw new ArgumentNullException(nameof(appt));
                if (appt.PatientId <= 0 || appt.DoctorId <= 0) throw new ArgumentException("Valid Patient and Doctor are required for appointment.");

                string query = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentMode, Status, Reason) VALUES (@PatientId, @DoctorId, @AppointmentDate, @AppointmentMode, @Status, @Reason)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", appt.PatientId),
                    new SqlParameter("@DoctorId", appt.DoctorId),
                    new SqlParameter("@AppointmentDate", appt.AppointmentDate),
                    new SqlParameter("@AppointmentMode", appt.AppointmentMode ?? "Clinic"),
                    new SqlParameter("@Status", "Pending"),
                    new SqlParameter("@Reason", appt.Reason ?? "")
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create appointment: {ex.Message}", ex);
            }
        }

        public void UpdateAppointmentStatus(int appointmentId, string status, string? notes = null, string? rejectionReason = null, DateTime? rescheduledDate = null)
        {
            try
            {
                if (appointmentId <= 0) throw new ArgumentException("Invalid Appointment ID.");
                string query = @"UPDATE Appointments SET Status=@Status, DoctorNotes=@Notes, RejectionReason=@RejectionReason, 
                                 RescheduledDate=@RescheduledDate WHERE AppointmentId=@AppointmentId";
                var parameters = new[]
                {
                    new SqlParameter("@AppointmentId", appointmentId),
                    new SqlParameter("@Status", status ?? "Pending"),
                    new SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                    new SqlParameter("@RejectionReason", (object?)rejectionReason ?? DBNull.Value),
                    new SqlParameter("@RescheduledDate", (object?)rescheduledDate ?? DBNull.Value)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update appointment status for {appointmentId}: {ex.Message}", ex);
            }
        }

        public List<Doctor> GetDoctors()
        {
            try
            {
                string query = "SELECT d.*, dep.DepartmentName FROM Doctors d JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE d.IsAvailable = 1";
                return _db.ExecuteQuery(query, r => new Doctor
                {
                    DoctorId = (int)r["DoctorId"],
                    FullName = r["FullName"]?.ToString() ?? "",
                    DepartmentId = (int)r["DepartmentId"],
                    DepartmentName = r["DepartmentName"]?.ToString() ?? "",
                    ConsultationFee = r["ConsultationFee"] != DBNull.Value ? (decimal)r["ConsultationFee"] : 0,
                    IsAvailable = (bool)r["IsAvailable"]
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving available doctors: {ex.Message}", ex);
            }
        }

        private Appointment MapAppointment(DataRow row)
        {
            var appt = new Appointment
            {
                AppointmentId = (int)row["AppointmentId"],
                PatientId = (int)row["PatientId"],
                DoctorId = (int)row["DoctorId"],
                AppointmentDate = (DateTime)row["AppointmentDate"],
                AppointmentMode = row["AppointmentMode"]?.ToString() ?? "",
                Status = row["Status"]?.ToString() ?? "",
                Reason = row["Reason"]?.ToString() ?? "",
                DoctorNotes = row["DoctorNotes"] == DBNull.Value ? null : row["DoctorNotes"]?.ToString(),
                RejectionReason = row["RejectionReason"] == DBNull.Value ? null : row["RejectionReason"]?.ToString(),
                RescheduledDate = row["RescheduledDate"] == DBNull.Value ? null : (DateTime?)row["RescheduledDate"]
            };

            if (row.Table.Columns.Contains("DoctorName")) appt.DoctorName = row["DoctorName"]?.ToString() ?? "";
            if (row.Table.Columns.Contains("PatientName")) appt.PatientName = row["PatientName"]?.ToString() ?? "";
            if (row.Table.Columns.Contains("DepartmentName")) appt.DepartmentName = row["DepartmentName"]?.ToString() ?? "";

            return appt;
        }
    }

    public class ReportRepository
    {
        private readonly DatabaseHelper _db;
        public ReportRepository(DatabaseHelper db) { _db = db; }

        public List<Report> GetReportsByPatientId(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Report>();
                string query = "SELECT * FROM Reports WHERE PatientId = @PatientId ORDER BY ReportDate DESC";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                return _db.ExecuteQuery(query, MapReport, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving reports for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Report> GetReportsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Report>();
                string query = "SELECT TOP 50 * FROM Reports WHERE DoctorId = @DoctorId ORDER BY ReportDate DESC";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                return _db.ExecuteQuery(query, MapReport, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving reports for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Report> GetReportsByDoctorIdPaged(int doctorId, int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ReportDate DESC" : orderBy;
                string query = $@"SELECT * FROM Reports WHERE DoctorId = @DoctorId 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                var parameters = new[] {
                    new SqlParameter("@DoctorId", doctorId),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return _db.ExecuteQuery(query, MapReport, parameters);
            }
            catch { return new List<Report>(); }
        }

        public int GetReportsByDoctorIdCount(int doctorId)
        {
            return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Reports WHERE DoctorId = @Id",
                new[] { new SqlParameter("@Id", doctorId) }) ?? 0);
        }

        public void CreateReport(Report report)
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
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create report: {ex.Message}", ex);
            }
        }

        private Report MapReport(DataRow row)
        {
            return new Report
            {
                ReportId = (int)row["ReportId"],
                PatientId = (int)row["PatientId"],
                DoctorId = (int)row["DoctorId"],
                AppointmentId = row["AppointmentId"] == DBNull.Value ? null : (int?)row["AppointmentId"],
                ReportName = row["ReportName"]?.ToString() ?? "",
                ReportType = row["ReportType"]?.ToString() ?? "",
                ReportDate = (DateTime)row["ReportDate"],
                FilePath = row["FilePath"]?.ToString() ?? "",
                Status = row["Status"]?.ToString() ?? "",
                Observations = row["Observations"] == DBNull.Value ? null : row["Observations"]?.ToString()
            };
        }
    }

    public class PrescriptionRepository
    {
        private readonly DatabaseHelper _db;
        public PrescriptionRepository(DatabaseHelper db) { _db = db; }

        public List<Prescription> GetPrescriptionsByPatientId(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Prescription>();
                string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE PatientId = @PatientId ORDER BY PrescribedDate DESC";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                return _db.ExecuteQuery(query, MapPrescription, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescriptions for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Prescription> GetPrescriptionsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Prescription>();
                string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.DoctorId = @DoctorId ORDER BY PrescribedDate DESC";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                return _db.ExecuteQuery(query, MapPrescription, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescriptions for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public Prescription? GetPrescriptionById(int prescriptionId)
        {
            try
            {
                if (prescriptionId <= 0) return null;
                string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE PrescriptionId = @PrescriptionId";
                var parameters = new[] { new SqlParameter("@PrescriptionId", prescriptionId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapPrescription(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving prescription {prescriptionId}: {ex.Message}", ex);
            }
        }

        public void CreatePrescription(Prescription p)
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
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create prescription: {ex.Message}", ex);
            }
        }

        private Prescription MapPrescription(DataRow row)
        {
            return new Prescription
            {
                PrescriptionId = (int)row["PrescriptionId"],
                PatientId = (int)row["PatientId"],
                DoctorId = (int)row["DoctorId"],
                AppointmentId = row["AppointmentId"] == DBNull.Value ? null : (int?)row["AppointmentId"],
                DoctorName = row["DoctorName"]?.ToString() ?? "",
                Details = row["Details"]?.ToString() ?? "",
                PrescribedDate = (DateTime)row["PrescribedDate"],
                Medications = row["Medications"]?.ToString() ?? "",
                IsLocked = (bool)row["IsLocked"],
                DigitalSignature = row["DigitalSignature"] == DBNull.Value ? null : row["DigitalSignature"]?.ToString()
            };
        }
    }

    public class BillingRepository
    {
        private readonly DatabaseHelper _db;
        public BillingRepository(DatabaseHelper db) { _db = db; }

        public List<Bill> GetBillsByPatientId(int patientId)
        {
            try
            {
                string query = "SELECT * FROM Bills WHERE PatientId = @PatientId ORDER BY BillDate DESC";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                var table = _db.ExecuteDataTable(query, parameters);
                var list = new List<Bill>();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        list.Add(new Bill
                        {
                            BillId = (int)row["BillId"],
                            PatientId = (int)row["PatientId"],
                            TotalAmount = (decimal)row["TotalAmount"],
                            PaidAmount = (decimal)row["PaidAmount"],
                            DueAmount = (decimal)row["DueAmount"],
                            Status = row["Status"].ToString()!,
                            BillDate = (DateTime)row["BillDate"]
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bills for patient {patientId}: {ex.Message}", ex);
            }
        }

        public int CreateBill(Bill bill)
        {
            if (bill.PatientId <= 0) throw new ArgumentException("Invalid Patient ID for bill.");
            if (bill.TotalAmount < 0) throw new ArgumentException("Bill Total Amount cannot be negative.");

            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"INSERT INTO Bills (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, ShiftId, CreatedBy, AdmissionId) 
                                           OUTPUT INSERTED.BillId
                                           VALUES (@PatientId, @TotalAmount, @PaidAmount, @DueAmount, @Status, @BillDate, @ShiftId, @CreatedBy, @AdmissionId)";

                        var billId = (int)_db.ExecuteScalar(sql, new[] {
                            new SqlParameter("@PatientId", bill.PatientId),
                            new SqlParameter("@TotalAmount", bill.TotalAmount),
                            new SqlParameter("@PaidAmount", bill.PaidAmount),
                            new SqlParameter("@DueAmount", bill.TotalAmount), // Initial Due = Total
                            new SqlParameter("@Status", bill.Status),
                            new SqlParameter("@BillDate", bill.BillDate),
                            new SqlParameter("@ShiftId", (object?)bill.ShiftId ?? DBNull.Value),
                            new SqlParameter("@CreatedBy", (object?)bill.CreatedBy ?? DBNull.Value),
                            new SqlParameter("@AdmissionId", (object?)bill.AdmissionId ?? DBNull.Value)
                        }, transaction);

                        // Insert Items
                        if (bill.Items != null && bill.Items.Any())
                        {
                            foreach (var item in bill.Items)
                            {
                                const string itemSql = @"INSERT INTO BillItems (BillId, Description, Amount, Category) 
                                                       VALUES (@BillId, @Description, @Amount, @Category)";
                                _db.ExecuteNonQuery(itemSql, new[] {
                                    new SqlParameter("@BillId", billId),
                                    new SqlParameter("@Description", item.Description ?? "No description"),
                                    new SqlParameter("@Amount", item.Amount),
                                    new SqlParameter("@Category", item.Category ?? "General")
                                }, transaction);
                            }
                        }

                        transaction.Commit();
                        return billId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Failed to create bill: {ex.Message}", ex);
                    }
                }
            }
        }

        public List<BillItem> GetBillItems(int billId)
        {
            try
            {
                const string sql = "SELECT * FROM BillItems WHERE BillId = @BillId";
                return _db.ExecuteQuery(sql, r => new BillItem
                {
                    BillItemId = (int)r["BillItemId"],
                    BillId = (int)r["BillId"],
                    Description = r["Description"]?.ToString() ?? "",
                    Amount = (decimal)r["Amount"],
                    Category = r["Category"]?.ToString() ?? "General"
                }, new[] { new SqlParameter("@BillId", billId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bill items for bill {billId}: {ex.Message}", ex);
            }
        }

        public void CreateComprehensiveBill(Bill bill)
        {
            bill.BillId = CreateBill(bill);
        }
    }

    public class OperationRepository
    {
        private readonly DatabaseHelper _db;
        public OperationRepository(DatabaseHelper db) { _db = db; }

        public List<OperationPackage> GetOperationPackages()
        {
            try
            {
                string query = "SELECT * FROM OperationPackages";
                return _db.ExecuteQuery(query, row => new OperationPackage
                {
                    PackageId = (int)row["PackageId"],
                    PackageName = row["PackageName"]?.ToString() ?? "Basic Package",
                    Description = row["Description"]?.ToString() ?? "",
                    Cost = (decimal)row["Cost"]
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operation packages: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPatientOperations(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<PatientOperation>();
                string query = @"SELECT po.*, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.PatientId = @PatientId";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                var table = _db.ExecuteDataTable(query, parameters);
                return MapOperations(table);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<PatientOperation>();
                string query = @"SELECT po.*, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName, ot.TheaterName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 WHERE po.DoctorId = @DoctorId
                                 ORDER BY po.ScheduledDate DESC";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                var table = _db.ExecuteDataTable(query, parameters);
                return MapOperations(table);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPendingOperations()
        {
            try
            {
                // OPTIMIZATION: Default to top 50 pending to keep UI snappy
                string query = @"SELECT TOP 50 po.*, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')
                                 ORDER BY po.ScheduledDate DESC";
                return MapOperations(_db.ExecuteDataTable(query));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving pending operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPendingOperationsPaged(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
                string query = $@"SELECT po.*, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')
                                 ORDER BY {orderClause}
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return MapOperations(_db.ExecuteDataTable(query, parameters));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged pending operations: {ex.Message}", ex);
            }
        }

        public int GetPendingOperationsCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM PatientOperations WHERE Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')";
                return Convert.ToInt32(_db.ExecuteScalar(query));
            }
            catch { return 0; }
        }

        private List<PatientOperation> MapOperations(DataTable? table)
        {
            var list = new List<PatientOperation>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    var op = new PatientOperation
                    {
                        OperationId = (int)row["OperationId"],
                        PatientId = (int)row["PatientId"],
                        PackageId = row["PackageId"] != DBNull.Value ? (int)row["PackageId"] : null,
                        PackageName = row["PackageName"] != DBNull.Value ? row["PackageName"].ToString() : (row["OperationName"]?.ToString() ?? ""),
                        Status = row["Status"]?.ToString() ?? "Proposed",
                        ScheduledDate = (DateTime)row["ScheduledDate"],
                        Notes = row["Notes"]?.ToString() ?? "",
                        DoctorId = row.Table.Columns.Contains("DoctorId") && row["DoctorId"] != DBNull.Value ? (int)row["DoctorId"] : 0,
                    };

                    if (row.Table.Columns.Contains("PatientName")) op.PatientName = row["PatientName"]?.ToString() ?? "Unknown";
                    if (row.Table.Columns.Contains("DoctorName")) op.DoctorName = row["DoctorName"]?.ToString() ?? "Unknown";

                    list.Add(op);
                }
            }
            return list;
        }

        public void UpdateOperationStatusAndCosts(int opId, string status, decimal? opCost, decimal? medCost, decimal? eqCost, int? theaterId, DateTime? scheduledDate = null, int? duration = null, DateTime? actualStartTime = null, int? doctorId = null)
        {
            try
            {
                if (opId <= 0) throw new ArgumentException("Invalid Operation ID.");
                string query = @"UPDATE PatientOperations 
                                 SET Status = @Status, 
                                     AgreedOperationCost = @OpCost, 
                                     AgreedMedicineCost = @MedCost, 
                                     AgreedEquipmentCost = @EqCost,
                                     TheaterId = @TheaterId,
                                     ScheduledDate = COALESCE(@ScheduledDate, ScheduledDate),
                                     DurationMinutes = COALESCE(@Duration, DurationMinutes),
                                     ActualStartTime = COALESCE(@ActualStart, ActualStartTime),
                                     DoctorId = COALESCE(@DoctorId, DoctorId)
                                 WHERE OperationId = @OpId";
                _db.ExecuteNonQuery(query, new[] {
                    new SqlParameter("@Status", status),
                    new SqlParameter("@OpCost", (object?)opCost ?? DBNull.Value),
                    new SqlParameter("@MedCost", (object?)medCost ?? DBNull.Value),
                    new SqlParameter("@EqCost", (object?)eqCost ?? DBNull.Value),
                    new SqlParameter("@TheaterId", (object?)theaterId ?? DBNull.Value),
                    new SqlParameter("@ScheduledDate", (object?)scheduledDate ?? DBNull.Value),
                    new SqlParameter("@Duration", (object?)duration ?? DBNull.Value),
                    new SqlParameter("@ActualStart", (object?)actualStartTime ?? DBNull.Value),
                    new SqlParameter("@DoctorId", (object?)doctorId ?? DBNull.Value),
                    new SqlParameter("@OpId", opId)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update operation {opId}: {ex.Message}", ex);
            }
        }

        public void CreatePatientOperation(PatientOperation op)
        {
            try
            {
                if (op == null || op.PatientId <= 0) throw new ArgumentException("Invalid operation data.");

                string query = @"INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, OperationName, ExpectedStayDays, RecommendedMedicines, RecommendedEquipment) 
                                 VALUES (@PatientId, @PackageId, @Status, @ScheduledDate, @Notes, @DoctorId, @Urgency, @OperationName, @ExpectedStayDays, @RecMeds, @RecEq)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", op.PatientId),
                    new SqlParameter("@PackageId", (object?)op.PackageId ?? DBNull.Value),
                    new SqlParameter("@Status", op.Status ?? "Proposed"),
                    new SqlParameter("@ScheduledDate", op.ScheduledDate),
                    new SqlParameter("@Notes", (object?)op.Notes ?? DBNull.Value),
                    new SqlParameter("@DoctorId", op.DoctorId),
                    new SqlParameter("@Urgency", (object?)op.Urgency ?? DBNull.Value),
                    new SqlParameter("@OperationName", (object?)op.PackageName ?? DBNull.Value),
                    new SqlParameter("@ExpectedStayDays", op.ExpectedStayDays),
                    new SqlParameter("@RecMeds", (object?)op.RecommendedMedicines ?? DBNull.Value),
                    new SqlParameter("@RecEq", (object?)op.RecommendedEquipment ?? DBNull.Value)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create patient operation: {ex.Message}", ex);
            }
        }


        public List<PatientOperation> GetOperationsByTheaterAndDate(int theaterId, DateTime date)
        {
            string query = @"SELECT po.*, p.FullName as PatientName, opkg.PackageName 
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             WHERE po.TheaterId = @TheaterId 
                             AND CAST(po.ScheduledDate AS DATE) = CAST(@Date AS DATE)
                             AND po.Status IN ('Scheduled', 'Running')";
            var parameters = new[] {
                new SqlParameter("@TheaterId", theaterId),
                new SqlParameter("@Date", date)
            };
            return MapOperations(_db.ExecuteDataTable(query, parameters));
        }

        public List<PatientOperation> GetAllScheduledOperations()
        {
            try
            {
                // OPTIMIZATION: Limit to top 200 recent/upcoming scheduled operations to keep the scheduler from lagging
                string query = @"SELECT TOP 200 po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Scheduled', 'Running')
                                 ORDER BY po.ScheduledDate DESC";
                return MapOperations(_db.ExecuteDataTable(query));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving scheduled operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetScheduledOperationsByRange(DateTime start, DateTime end)
        {
            try
            {
                string query = @"SELECT po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.ScheduledDate >= @Start AND po.ScheduledDate <= @End
                                 AND po.Status NOT IN ('Cancelled')";

                var parameters = new[] {
                    new SqlParameter("@Start", start),
                    new SqlParameter("@End", end)
                };
                return MapOperations(_db.ExecuteDataTable(query, parameters));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations by range: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsByStatus(string status)
        {
            // TOP 100 for safety
            string query = @"SELECT TOP 100 po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status = @Status";
            var parameters = new[] { new SqlParameter("@Status", status) };
            return MapOperations(_db.ExecuteDataTable(query, parameters));
        }

        public List<PatientOperation> GetOperationsByStatusPaged(string status, int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
                string query = $@"SELECT po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status = @Status
                                 ORDER BY {orderClause}
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Status", status),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return MapOperations(_db.ExecuteDataTable(query, parameters));
            }
            catch { return new List<PatientOperation>(); }
        }

        public int GetOperationsByStatusCount(string status)
        {
            return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM PatientOperations WHERE Status = @Status",
                new[] { new SqlParameter("@Status", status) }) ?? 0);
        }

        public List<PatientOperation> GetOperationsReadyForTransfer()
        {
            // Completed operations where the patient has not been transferred to a bed yet
            // TOP 50 for dashboard performance
            string query = @"SELECT TOP 50 po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status = 'Completed' AND po.IsTransferred = 0
                             ORDER BY po.ScheduledDate DESC";
            return MapOperations(_db.ExecuteDataTable(query));
        }

        public void MarkOperationAsTransferred(int operationId)
        {
            string sql = "UPDATE PatientOperations SET IsTransferred = 1 WHERE OperationId = @Id";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Id", operationId) });
        }
    }


    public class SupportRepository
    {
        private readonly DatabaseHelper _db;
        public SupportRepository(DatabaseHelper db) { _db = db; }

        public List<SupportTicket> GetTicketsByPatientId(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<SupportTicket>();
                string query = "SELECT * FROM SupportTickets WHERE PatientId = @PatientId ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                var table = _db.ExecuteDataTable(query, parameters);
                var list = new List<SupportTicket>();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        list.Add(new SupportTicket
                        {
                            TicketId = (int)row["TicketId"],
                            PatientId = (int)row["PatientId"],
                            Subject = row["Subject"]?.ToString() ?? "No Subject",
                            Message = row["Message"]?.ToString() ?? "",
                            Status = row["Status"]?.ToString() ?? "Open",
                            CreatedDate = (DateTime)row["CreatedDate"],
                            Response = row["Response"] == DBNull.Value ? null : row["Response"]?.ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving support tickets for patient {patientId}: {ex.Message}", ex);
            }
        }

        public void CreateTicket(SupportTicket ticket)
        {
            try
            {
                if (ticket == null || ticket.PatientId <= 0) throw new ArgumentException("Invalid ticket data.");

                string query = "INSERT INTO SupportTickets (PatientId, Subject, Message, Status, CreatedDate) VALUES (@PatientId, @Subject, @Message, 'Open', @CreatedDate)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", ticket.PatientId),
                    new SqlParameter("@Subject", ticket.Subject ?? "No Subject"),
                    new SqlParameter("@Message", ticket.Message ?? ""),
                    new SqlParameter("@CreatedDate", DateTime.Now)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create support ticket: {ex.Message}", ex);
            }
        }
    }

    public class DoctorShiftRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorShiftRepository(DatabaseHelper db) { _db = db; }

        public List<DoctorShift> GetShiftsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<DoctorShift>();
                string query = "SELECT * FROM DoctorShifts WHERE DoctorId = @DoctorId AND IsActive = 1 ORDER BY CASE DayOfWeek WHEN 'Monday' THEN 1 WHEN 'Tuesday' THEN 2 WHEN 'Wednesday' THEN 3 WHEN 'Thursday' THEN 4 WHEN 'Friday' THEN 5 WHEN 'Saturday' THEN 6 WHEN 'Sunday' THEN 7 END, StartTime";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                return _db.ExecuteQuery(query, MapShift, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving shifts for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public DoctorShift? GetShiftById(int shiftId)
        {
            try
            {
                if (shiftId <= 0) return null;
                string query = "SELECT * FROM DoctorShifts WHERE ShiftId = @ShiftId";
                var parameters = new[] { new SqlParameter("@ShiftId", shiftId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapShift(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving shift {shiftId}: {ex.Message}", ex);
            }
        }

        public void CreateShift(DoctorShift shift)
        {
            try
            {
                if (shift == null || shift.DoctorId <= 0) throw new ArgumentException("Invalid shift data.");

                string query = @"INSERT INTO DoctorShifts (DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt) 
                                VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @ShiftType, @IsActive, @Notes, @CreatedAt)";
                var parameters = new[]
                {
                    new SqlParameter("@DoctorId", shift.DoctorId),
                    new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"),
                    new SqlParameter("@StartTime", shift.StartTime),
                    new SqlParameter("@EndTime", shift.EndTime),
                    new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                    new SqlParameter("@IsActive", shift.IsActive),
                    new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value),
                    new SqlParameter("@CreatedAt", shift.CreatedAt == default ? DateTime.Now : shift.CreatedAt)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create shift: {ex.Message}", ex);
            }
        }

        public void UpdateShift(DoctorShift shift)
        {
            try
            {
                if (shift == null || shift.ShiftId <= 0) throw new ArgumentException("Invalid shift ID for update.");

                string query = @"UPDATE DoctorShifts SET DayOfWeek=@DayOfWeek, StartTime=@StartTime, EndTime=@EndTime, 
                                ShiftType=@ShiftType, IsActive=@IsActive, Notes=@Notes WHERE ShiftId=@ShiftId";
                var parameters = new[]
                {
                    new SqlParameter("@ShiftId", shift.ShiftId),
                    new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"),
                    new SqlParameter("@StartTime", shift.StartTime),
                    new SqlParameter("@EndTime", shift.EndTime),
                    new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                    new SqlParameter("@IsActive", shift.IsActive),
                    new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value)
                };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update shift {shift?.ShiftId}: {ex.Message}", ex);
            }
        }

        public void DeleteShift(int shiftId)
        {
            try
            {
                if (shiftId <= 0) return;
                string query = "UPDATE DoctorShifts SET IsActive = 0 WHERE ShiftId = @ShiftId";
                var parameters = new[] { new SqlParameter("@ShiftId", shiftId) };
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete shift {shiftId}: {ex.Message}", ex);
            }
        }

        public bool IsAvailableAtTime(int doctorId, DateTime appointmentDateTime)
        {
            string dayOfWeek = appointmentDateTime.DayOfWeek.ToString();
            TimeSpan appointmentTime = appointmentDateTime.TimeOfDay;

            string query = @"SELECT COUNT(*) FROM DoctorShifts 
                            WHERE DoctorId = @DoctorId 
                            AND DayOfWeek = @DayOfWeek 
                            AND StartTime <= @AppointmentTime 
                            AND EndTime >= @AppointmentTime 
                            AND IsActive = 1";

            var parameters = new[]
            {
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@DayOfWeek", dayOfWeek),
                new SqlParameter("@AppointmentTime", appointmentTime)
            };

            var result = _db.ExecuteScalar(query, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        private DoctorShift MapShift(DataRow row)
        {
            return new DoctorShift
            {
                ShiftId = (int)row["ShiftId"],
                DoctorId = (int)row["DoctorId"],
                DayOfWeek = row["DayOfWeek"]?.ToString() ?? "",
                StartTime = (TimeSpan)row["StartTime"],
                EndTime = (TimeSpan)row["EndTime"],
                ShiftType = row["ShiftType"] == DBNull.Value ? null : row["ShiftType"]?.ToString(),
                IsActive = (bool)row["IsActive"],
                Notes = row["Notes"] == DBNull.Value ? null : row["Notes"]?.ToString(),
                CreatedAt = (DateTime)row["CreatedAt"]
            };
        }
    }

    public class NotificationRepository
    {
        private readonly DatabaseHelper _db;
        public NotificationRepository(DatabaseHelper db) { _db = db; }

        public List<Notification> GetNotificationsByPatientId(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Notification>();
                string query = "SELECT * FROM Notifications WHERE PatientId = @PatientId ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@PatientId", patientId) };
                return _db.ExecuteQuery(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Notification> GetNotificationsByDoctorId(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Notification>();
                string query = "SELECT * FROM Notifications WHERE DoctorId = @DoctorId ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
                return _db.ExecuteQuery(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Notification> GetAdminNotifications()
        {
            try
            {
                // OPTIMIZATION: Only get top 50 recent notifications
                string query = "SELECT TOP 50 * FROM Notifications WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin' ORDER BY CreatedDate DESC";
                return _db.ExecuteQuery(query, MapNotification);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving admin notifications: {ex.Message}", ex);
            }
        }

        public int GetAdminUnreadCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM Notifications WHERE ((PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin') AND IsRead = 0";
                var result = _db.ExecuteScalar(query);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public void MarkAdminNotificationsAsRead()
        {
            try
            {
                const string sql = "UPDATE Notifications SET IsRead = 1 WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin'";
                _db.ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to mark admin notifications as read: {ex.Message}", ex);
            }
        }

        public List<Notification> GetNotificationsByRole(string role)
        {
            try
            {
                if (string.IsNullOrEmpty(role)) return new List<Notification>();
                // OPTIMIZATION: Only get top 50 recent notifications
                string query = "SELECT TOP 50 * FROM Notifications WHERE TargetRole = @Role ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@Role", role) };
                return _db.ExecuteQuery(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for role {role}: {ex.Message}", ex);
            }
        }

        public int GetRoleUnreadCount(string role)
        {
            try
            {
                if (string.IsNullOrEmpty(role)) return 0;
                string query = "SELECT COUNT(*) FROM Notifications WHERE TargetRole = @Role AND IsRead = 0";
                var parameters = new[] { new SqlParameter("@Role", role) };
                var result = _db.ExecuteScalar(query, parameters);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public int GetDoctorUnreadCount(int doctorId)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM Notifications WHERE DoctorId = @Id AND IsRead = 0";
                var result = _db.ExecuteScalar(query, new[] { new SqlParameter("@Id", doctorId) });
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public int GetPatientUnreadCount(int patientId)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM Notifications WHERE PatientId = @Id AND IsRead = 0";
                var result = _db.ExecuteScalar(query, new[] { new SqlParameter("@Id", patientId) });
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public void MarkNotificationsAsRead(int? doctorId, int? patientId)
        {
            try
            {
                string sql = "UPDATE Notifications SET IsRead = 1 WHERE ";
                if (doctorId.HasValue)
                    _db.ExecuteNonQuery(sql + "DoctorId = @Id", new[] { new SqlParameter("@Id", doctorId.Value) });
                else if (patientId.HasValue)
                    _db.ExecuteNonQuery(sql + "PatientId = @Id", new[] { new SqlParameter("@Id", patientId.Value) });
            }
            catch { }
        }

        public void MarkRoleNotificationsAsRead(string role)
        {
            try
            {
                if (string.IsNullOrEmpty(role)) return;
                const string sql = "UPDATE Notifications SET IsRead = 1 WHERE TargetRole = @Role";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Role", role) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to mark notifications as read for role {role}: {ex.Message}", ex);
            }
        }

        public void CreateNotification(Notification n)
        {
            try
            {
                if (n == null) throw new ArgumentException("Notification data is required.");

                string query = "INSERT INTO Notifications (PatientId, DoctorId, TargetRole, Title, Message, CreatedDate, IsRead) VALUES (@PatientId, @DoctorId, @TargetRole, @Title, @Message, @CreatedDate, @IsRead)";
                _db.ExecuteNonQuery(query, new[] {
                    new SqlParameter("@PatientId", (object?)n.PatientId ?? DBNull.Value),
                    new SqlParameter("@DoctorId", (object?)n.DoctorId ?? DBNull.Value),
                    new SqlParameter("@TargetRole", (object?)n.TargetRole ?? DBNull.Value),
                    new SqlParameter("@Title", n.Title ?? "System Notification"),
                    new SqlParameter("@Message", n.Message ?? ""),
                    new SqlParameter("@CreatedDate", n.CreatedDate == default ? DateTime.Now : n.CreatedDate),
                    new SqlParameter("@IsRead", n.IsRead)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create notification: {ex.Message}", ex);
            }
        }

        private Notification MapNotification(DataRow row)
        {
            return new Notification
            {
                NotificationId = (int)row["NotificationId"],
                PatientId = row["PatientId"] != DBNull.Value ? (int)row["PatientId"] : null,
                DoctorId = row["DoctorId"] != DBNull.Value ? (int)row["DoctorId"] : null,
                TargetRole = row.Table.Columns.Contains("TargetRole") && row["TargetRole"] != DBNull.Value ? row["TargetRole"].ToString() : null,
                Title = row["Title"].ToString() ?? string.Empty,
                Message = row["Message"]?.ToString() ?? "",
                CreatedDate = (DateTime)row["CreatedDate"],
                IsRead = (bool)row["IsRead"]
            };
        }
    }
}
