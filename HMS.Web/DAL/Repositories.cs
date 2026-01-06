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
            // ... (existing implementation)
            string query = "SELECT * FROM Patients WHERE UserId = @UserId";
            var parameters = new[] { new SqlParameter("@UserId", userId) };
            var table = _db.ExecuteDataTable(query, parameters);

            if (table != null && table.Rows.Count > 0)
            {
                var row = table.Rows[0];
                return MapPatient(row);
            }
            return null;
        }

        public List<Patient> GetAllPatients()
        {
            string query = "SELECT * FROM Patients WHERE IsActive = 1 ORDER BY FullName";
            var table = _db.ExecuteDataTable(query);
            var list = new List<Patient>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapPatient(row));
                }
            }
            return list;
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
            string query = "SELECT * FROM Patients WHERE PatientId = @PatientId";
            var parameters = new[] { new SqlParameter("@PatientId", patientId) };
            var table = _db.ExecuteDataTable(query, parameters);

            if (table != null && table.Rows.Count > 0)
            {
                var row = table.Rows[0];
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
            return null;
        }

        public void CreatePatient(Patient patient)
        {
            string query = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, 
                            EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Allergies, ChronicDiseases, 
                            CurrentMedications, DisabilityStatus, RegistrationDate, IsActive, PatientType, Email, City, Country, LastVisitDate, PrimaryDoctorId) 
                            VALUES (@UserId, @FullName, @DateOfBirth, @Gender, @ContactNumber, @Address, @CNIC, @BloodGroup, @MaritalStatus, 
                            @EmergencyContactName, @EmergencyContactNumber, @RelationshipToEmergencyContact, @Allergies, @ChronicDiseases, 
                            @CurrentMedications, @DisabilityStatus, @RegistrationDate, @IsActive, @PatientType, @Email, @City, @Country, @LastVisitDate, @PrimaryDoctorId)";
            var parameters = new[]
            {
                new SqlParameter("@UserId", patient.UserId),
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

        public void UpdatePatient(Patient patient)
        {
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
    }

    public class DoctorRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorRepository(DatabaseHelper db) { _db = db; }

        public Doctor? GetDoctorByUserId(string userId)
        {
            string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE UserId = @UserId";
            var parameters = new[] { new SqlParameter("@UserId", userId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapDoctor(table.Rows[0]);
            }
            return null;
        }

        public Doctor? GetDoctorById(int doctorId)
        {
            string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE DoctorId = @DoctorId";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapDoctor(table.Rows[0]);
            }
            return null;
        }

        public List<Doctor> GetDoctors()
        {
            string query = "SELECT d.*, dep.DepartmentName FROM Doctors d LEFT JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE d.IsActive = 1 ORDER BY d.FullName";
            var table = _db.ExecuteDataTable(query);
            var list = new List<Doctor>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapDoctor(row));
                }
            }
            return list;
        }

        public void CreateDoctor(Doctor doc)
        {
            string query = @"INSERT INTO Doctors (UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization, 
                            MedicalLicenseNumber, LicenseIssuingAuthority, YearsOfExperience, DepartmentId, HospitalJoiningDate, 
                            ConsultationFee, FollowUpFee, AvailableDays, AvailableTimeSlots, RoomNumber, IsOnCall, IsActive, IsVerified, CreatedAt, IsAvailable) 
                            VALUES (@UserId, @FullName, @Gender, @ContactNumber, @Email, @Qualification, @Specialization, 
                            @MedicalLicenseNumber, @LicenseIssuingAuthority, @YearsOfExperience, @DepartmentId, @HospitalJoiningDate, 
                            @ConsultationFee, @FollowUpFee, @AvailableDays, @AvailableTimeSlots, @RoomNumber, @IsOnCall, @IsActive, @IsVerified, @CreatedAt, @IsAvailable)";
            var parameters = new[]
            {
                new SqlParameter("@UserId", (object?)doc.UserId ?? DBNull.Value),
                new SqlParameter("@FullName", (object?)doc.FullName ?? DBNull.Value),
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
                new SqlParameter("@CreatedAt", doc.CreatedAt),
                new SqlParameter("@IsAvailable", doc.IsAvailable)
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public void UpdateDoctor(Doctor doc)
        {
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
                new SqlParameter("@FullName", doc.FullName),
                new SqlParameter("@Gender", doc.Gender),
                new SqlParameter("@ContactNumber", doc.ContactNumber),
                new SqlParameter("@Email", doc.Email),
                new SqlParameter("@Qualification", doc.Qualification),
                new SqlParameter("@Specialization", doc.Specialization),
                new SqlParameter("@MedicalLicenseNumber", doc.MedicalLicenseNumber),
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
            string query = @"
                SELECT a.*, d.FullName as DoctorName, dep.DepartmentName, p.FullName as PatientName
                FROM Appointments a
                JOIN Doctors d ON a.DoctorId = d.DoctorId
                JOIN Departments dep ON d.DepartmentId = dep.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                WHERE a.PatientId = @PatientId
                ORDER BY a.AppointmentDate DESC";

            var parameters = new[] { new SqlParameter("@PatientId", patientId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Appointment>();

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapAppointment(row));
                }
            }
            return list;
        }

        public List<Appointment> GetAppointmentsByDoctorId(int doctorId)
        {
            string query = @"
                SELECT a.*, p.FullName as PatientName
                FROM Appointments a
                JOIN Patients p ON a.PatientId = p.PatientId
                WHERE a.DoctorId = @DoctorId
                ORDER BY a.AppointmentDate DESC";

            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Appointment>();

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapAppointment(row));
                }
            }
            return list;
        }

        public void CreateAppointment(Appointment appt)
        {
            string query = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentMode, Status, Reason) VALUES (@PatientId, @DoctorId, @AppointmentDate, @AppointmentMode, @Status, @Reason)";
            var parameters = new[]
            {
                new SqlParameter("@PatientId", appt.PatientId),
                new SqlParameter("@DoctorId", appt.DoctorId),
                new SqlParameter("@AppointmentDate", appt.AppointmentDate),
                new SqlParameter("@AppointmentMode", appt.AppointmentMode),
                new SqlParameter("@Status", "Pending"),
                new SqlParameter("@Reason", appt.Reason ?? "")
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public void UpdateAppointmentStatus(int appointmentId, string status, string? notes = null, string? rejectionReason = null, DateTime? rescheduledDate = null)
        {
            string query = @"UPDATE Appointments SET Status=@Status, DoctorNotes=@Notes, RejectionReason=@RejectionReason, 
                             RescheduledDate=@RescheduledDate WHERE AppointmentId=@AppointmentId";
            var parameters = new[]
            {
                new SqlParameter("@AppointmentId", appointmentId),
                new SqlParameter("@Status", status),
                new SqlParameter("@Notes", (object?)notes ?? DBNull.Value),
                new SqlParameter("@RejectionReason", (object?)rejectionReason ?? DBNull.Value),
                new SqlParameter("@RescheduledDate", (object?)rescheduledDate ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public List<Doctor> GetDoctors()
        {
            string query = "SELECT d.*, dep.DepartmentName FROM Doctors d JOIN Departments dep ON d.DepartmentId = dep.DepartmentId WHERE d.IsAvailable = 1";
            var table = _db.ExecuteDataTable(query);
            var list = new List<Doctor>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new Doctor
                    {
                        DoctorId = (int)row["DoctorId"],
                        FullName = row["FullName"]?.ToString() ?? "",
                        DepartmentId = (int)row["DepartmentId"],
                        DepartmentName = row["DepartmentName"]?.ToString() ?? "",
                        ConsultationFee = row["ConsultationFee"] != DBNull.Value ? (decimal)row["ConsultationFee"] : 0,
                        IsAvailable = (bool)row["IsAvailable"]
                    });
                }
            }
            return list;
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
            string query = "SELECT * FROM Reports WHERE PatientId = @PatientId ORDER BY ReportDate DESC";
            var parameters = new[] { new SqlParameter("@PatientId", patientId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Report>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapReport(row));
                }
            }
            return list;
        }

        public List<Report> GetReportsByDoctorId(int doctorId)
        {
            string query = "SELECT * FROM Reports WHERE DoctorId = @DoctorId ORDER BY ReportDate DESC";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Report>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapReport(row));
                }
            }
            return list;
        }

        public void CreateReport(Report report)
        {
            string query = @"INSERT INTO Reports (PatientId, DoctorId, AppointmentId, ReportName, ReportType, ReportDate, FilePath, Status, Observations) 
                            VALUES (@PatientId, @DoctorId, @AppointmentId, @ReportName, @ReportType, @ReportDate, @FilePath, @Status, @Observations)";
            var parameters = new[]
            {
                new SqlParameter("@PatientId", report.PatientId),
                new SqlParameter("@DoctorId", report.DoctorId),
                new SqlParameter("@AppointmentId", (object?)report.AppointmentId ?? DBNull.Value),
                new SqlParameter("@ReportName", report.ReportName),
                new SqlParameter("@ReportType", report.ReportType),
                new SqlParameter("@ReportDate", report.ReportDate),
                new SqlParameter("@FilePath", report.FilePath ?? ""),
                new SqlParameter("@Status", report.Status),
                new SqlParameter("@Observations", (object?)report.Observations ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
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
            string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE PatientId = @PatientId ORDER BY PrescribedDate DESC";
            var parameters = new[] { new SqlParameter("@PatientId", patientId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Prescription>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapPrescription(row));
                }
            }
            return list;
        }

        public List<Prescription> GetPrescriptionsByDoctorId(int doctorId)
        {
            string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE p.DoctorId = @DoctorId ORDER BY PrescribedDate DESC";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Prescription>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapPrescription(row));
                }
            }
            return list;
        }

        public Prescription? GetPrescriptionById(int prescriptionId)
        {
            string query = "SELECT p.*, d.FullName as DoctorName FROM Prescriptions p JOIN Doctors d ON p.DoctorId = d.DoctorId WHERE PrescriptionId = @PrescriptionId";
            var parameters = new[] { new SqlParameter("@PrescriptionId", prescriptionId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapPrescription(table.Rows[0]);
            }
            return null;
        }

        public void CreatePrescription(Prescription p)
        {
            string query = @"INSERT INTO Prescriptions (PatientId, DoctorId, AppointmentId, Details, PrescribedDate, Medications, IsLocked, DigitalSignature) 
                            VALUES (@PatientId, @DoctorId, @AppointmentId, @Details, @PrescribedDate, @Medications, @IsLocked, @DigitalSignature)";
            var parameters = new[]
            {
                new SqlParameter("@PatientId", p.PatientId),
                new SqlParameter("@DoctorId", p.DoctorId),
                new SqlParameter("@AppointmentId", (object?)p.AppointmentId ?? DBNull.Value),
                new SqlParameter("@Details", p.Details ?? ""),
                new SqlParameter("@PrescribedDate", p.PrescribedDate),
                new SqlParameter("@Medications", p.Medications ?? ""),
                new SqlParameter("@IsLocked", p.IsLocked),
                new SqlParameter("@DigitalSignature", (object?)p.DigitalSignature ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
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
            // Assumes Bills table exists
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
                        Status = row["Status"].ToString(),
                        BillDate = (DateTime)row["BillDate"]
                    });
                }
            }
            return list;
        }

        public int CreateBill(Bill bill)
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
            });

            // Insert Items
            if (bill.Items != null && bill.Items.Any())
            {
                foreach (var item in bill.Items)
                {
                    const string itemSql = @"INSERT INTO BillItems (BillId, Description, Amount, Category) 
                                           VALUES (@BillId, @Description, @Amount, @Category)";
                    _db.ExecuteNonQuery(itemSql, new[] {
                        new SqlParameter("@BillId", billId),
                        new SqlParameter("@Description", item.Description),
                        new SqlParameter("@Amount", item.Amount),
                        new SqlParameter("@Category", item.Category)
                    });
                }
            }

            return billId;
        }

        public List<BillItem> GetBillItems(int billId)
        {
            const string sql = "SELECT * FROM BillItems WHERE BillId = @BillId";
            return _db.ExecuteQuery(sql, r => new BillItem
            {
                BillItemId = (int)r["BillItemId"],
                BillId = (int)r["BillId"],
                Description = r["Description"].ToString()!,
                Amount = (decimal)r["Amount"],
                Category = r["Category"].ToString()!
            }, new[] { new SqlParameter("@BillId", billId) });
        }

        public void CreateComprehensiveBill(Bill bill)
        {
            // This method assumes the Bill object already has its items populated and TotalAmount calculated.
            // It creates the bill and its associated items in a single transaction.
            int billId = CreateBill(bill); // Re-use the CreateBill logic which now handles items
            bill.BillId = billId; // Assign the newly created BillId back to the object
        }
    }

    public class OperationRepository
    {
        private readonly DatabaseHelper _db;
        public OperationRepository(DatabaseHelper db) { _db = db; }

        public List<OperationPackage> GetOperationPackages()
        {
            string query = "SELECT * FROM OperationPackages";
            var table = _db.ExecuteDataTable(query);
            var list = new List<OperationPackage>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new OperationPackage
                    {
                        PackageId = (int)row["PackageId"],
                        PackageName = row["PackageName"].ToString(),
                        Description = row["Description"].ToString(),
                        Cost = (decimal)row["Cost"]
                    });
                }
            }
            return list;
        }

        public List<PatientOperation> GetPatientOperations(int patientId)
        {
            // Join Patients and Doctors for names if needed, or just handle at view level
            // For Admin view, we often need all operations, not just by patientId. 
            // So we might need a "GetAllPendingOperations" too.
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

        public List<PatientOperation> GetPendingOperations()
        {
            string query = @"SELECT po.*, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                             FROM PatientOperations po 
                             LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                             LEFT JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status IN ('Recommended', 'Pending Deposit')";
            var table = _db.ExecuteDataTable(query);
            return MapOperations(table);
        }

        private List<PatientOperation> MapOperations(DataTable? table)
        {
            var list = new List<PatientOperation>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PatientOperation
                    {
                        OperationId = (int)row["OperationId"],
                        PatientId = (int)row["PatientId"],
                        PackageId = row["PackageId"] != DBNull.Value ? (int)row["PackageId"] : null,
                        PackageName = row["PackageName"] != DBNull.Value ? row["PackageName"].ToString() : (row["OperationName"]?.ToString() ?? ""),
                        Status = row["Status"].ToString(),
                        ScheduledDate = (DateTime)row["ScheduledDate"],
                        Notes = row["Notes"].ToString(),
                        DoctorId = row.Table.Columns.Contains("DoctorId") ? ((row["DoctorId"] != DBNull.Value) ? (int)row["DoctorId"] : 0) : 0,
                        Urgency = row.Table.Columns.Contains("Urgency") ? row["Urgency"]?.ToString() : null,
                        ExpectedStayDays = row.Table.Columns.Contains("ExpectedStayDays") && row["ExpectedStayDays"] != DBNull.Value ? (int)row["ExpectedStayDays"] : 0,
                        RecommendedMedicines = row.Table.Columns.Contains("RecommendedMedicines") ? row["RecommendedMedicines"]?.ToString() : null,
                        RecommendedEquipment = row.Table.Columns.Contains("RecommendedEquipment") ? row["RecommendedEquipment"]?.ToString() : null,
                        DurationMinutes = row.Table.Columns.Contains("DurationMinutes") && row["DurationMinutes"] != DBNull.Value ? (int)row["DurationMinutes"] : 60,
                        ActualStartTime = row.Table.Columns.Contains("ActualStartTime") && row["ActualStartTime"] != DBNull.Value ? (DateTime?)row["ActualStartTime"] : null,


                        // Costs
                        AgreedOperationCost = row.Table.Columns.Contains("AgreedOperationCost") && row["AgreedOperationCost"] != DBNull.Value ? (decimal)row["AgreedOperationCost"] : null,
                        AgreedMedicineCost = row.Table.Columns.Contains("AgreedMedicineCost") && row["AgreedMedicineCost"] != DBNull.Value ? (decimal)row["AgreedMedicineCost"] : null,
                        AgreedEquipmentCost = row.Table.Columns.Contains("AgreedEquipmentCost") && row["AgreedEquipmentCost"] != DBNull.Value ? (decimal)row["AgreedEquipmentCost"] : null,

                        // Joined Names
                        PatientName = row.Table.Columns.Contains("PatientName") ? row["PatientName"]?.ToString() : null,
                        DoctorName = row.Table.Columns.Contains("DoctorName") ? row["DoctorName"]?.ToString() : null,
                        TheaterId = row.Table.Columns.Contains("TheaterId") && row["TheaterId"] != DBNull.Value ? (int)row["TheaterId"] : null,
                        TheaterName = row.Table.Columns.Contains("TheaterName") ? row["TheaterName"]?.ToString() : null,
                        IsTransferred = row.Table.Columns.Contains("IsTransferred") && row["IsTransferred"] != DBNull.Value ? (bool)row["IsTransferred"] : false,
                    });
                }
            }
            return list;
        }

        public void UpdateOperationStatusAndCosts(int opId, string status, decimal? opCost, decimal? medCost, decimal? eqCost, int? theaterId, DateTime? scheduledDate = null, int? duration = null, DateTime? actualStartTime = null, int? doctorId = null)
        {
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

        public void CreatePatientOperation(PatientOperation op)
        {
            string query = @"INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, OperationName, ExpectedStayDays, RecommendedMedicines, RecommendedEquipment) 
                             VALUES (@PatientId, @PackageId, @Status, @ScheduledDate, @Notes, @DoctorId, @Urgency, @OperationName, @ExpectedStayDays, @RecMeds, @RecEq)";
            var parameters = new[]
            {
                new SqlParameter("@PatientId", op.PatientId),
                new SqlParameter("@PackageId", (object?)op.PackageId ?? DBNull.Value),
                new SqlParameter("@Status", op.Status),
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
            string query = @"SELECT po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status IN ('Scheduled', 'Running')";
            return MapOperations(_db.ExecuteDataTable(query));
        }

        public List<PatientOperation> GetOperationsByStatus(string status)
        {
            string query = @"SELECT po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status = @Status";
            var parameters = new[] { new SqlParameter("@Status", status) };
            return MapOperations(_db.ExecuteDataTable(query, parameters));
        }

        public List<PatientOperation> GetOperationsReadyForTransfer()
        {
            // Completed operations where the patient has not been transferred to a bed yet
            string query = @"SELECT po.*, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                             FROM PatientOperations po 
                             JOIN Patients p ON po.PatientId = p.PatientId
                             LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                             LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                             LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                             WHERE po.Status = 'Completed' AND po.IsTransferred = 0";
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
                        Subject = row["Subject"].ToString(),
                        Message = row["Message"].ToString(),
                        Status = row["Status"].ToString(),
                        CreatedDate = (DateTime)row["CreatedDate"],
                        Response = row["Response"] == DBNull.Value ? null : row["Response"].ToString()
                    });
                }
            }
            return list;
        }

        public void CreateTicket(SupportTicket ticket)
        {
            string query = "INSERT INTO SupportTickets (PatientId, Subject, Message, Status, CreatedDate) VALUES (@PatientId, @Subject, @Message, 'Open', @CreatedDate)";
            var parameters = new[]
            {
                new SqlParameter("@PatientId", ticket.PatientId),
                new SqlParameter("@Subject", ticket.Subject),
                new SqlParameter("@Message", ticket.Message),
                new SqlParameter("@CreatedDate", DateTime.Now)
            };
            _db.ExecuteNonQuery(query, parameters);
        }
    }

    public class DoctorShiftRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorShiftRepository(DatabaseHelper db) { _db = db; }

        public List<DoctorShift> GetShiftsByDoctorId(int doctorId)
        {
            string query = "SELECT * FROM DoctorShifts WHERE DoctorId = @DoctorId AND IsActive = 1 ORDER BY CASE DayOfWeek WHEN 'Monday' THEN 1 WHEN 'Tuesday' THEN 2 WHEN 'Wednesday' THEN 3 WHEN 'Thursday' THEN 4 WHEN 'Friday' THEN 5 WHEN 'Saturday' THEN 6 WHEN 'Sunday' THEN 7 END, StartTime";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<DoctorShift>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapShift(row));
                }
            }
            return list;
        }

        public DoctorShift? GetShiftById(int shiftId)
        {
            string query = "SELECT * FROM DoctorShifts WHERE ShiftId = @ShiftId";
            var parameters = new[] { new SqlParameter("@ShiftId", shiftId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapShift(table.Rows[0]);
            }
            return null;
        }

        public void CreateShift(DoctorShift shift)
        {
            string query = @"INSERT INTO DoctorShifts (DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt) 
                            VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @ShiftType, @IsActive, @Notes, @CreatedAt)";
            var parameters = new[]
            {
                new SqlParameter("@DoctorId", shift.DoctorId),
                new SqlParameter("@DayOfWeek", shift.DayOfWeek),
                new SqlParameter("@StartTime", shift.StartTime),
                new SqlParameter("@EndTime", shift.EndTime),
                new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                new SqlParameter("@IsActive", shift.IsActive),
                new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value),
                new SqlParameter("@CreatedAt", shift.CreatedAt)
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public void UpdateShift(DoctorShift shift)
        {
            string query = @"UPDATE DoctorShifts SET DayOfWeek=@DayOfWeek, StartTime=@StartTime, EndTime=@EndTime, 
                            ShiftType=@ShiftType, IsActive=@IsActive, Notes=@Notes WHERE ShiftId=@ShiftId";
            var parameters = new[]
            {
                new SqlParameter("@ShiftId", shift.ShiftId),
                new SqlParameter("@DayOfWeek", shift.DayOfWeek),
                new SqlParameter("@StartTime", shift.StartTime),
                new SqlParameter("@EndTime", shift.EndTime),
                new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                new SqlParameter("@IsActive", shift.IsActive),
                new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public void DeleteShift(int shiftId)
        {
            string query = "UPDATE DoctorShifts SET IsActive = 0 WHERE ShiftId = @ShiftId";
            var parameters = new[] { new SqlParameter("@ShiftId", shiftId) };
            _db.ExecuteNonQuery(query, parameters);
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
            string query = "SELECT * FROM Notifications WHERE PatientId = @PatientId ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@PatientId", patientId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Notification>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapNotification(row));
                }
            }
            return list;
        }

        public List<Notification> GetNotificationsByDoctorId(int doctorId)
        {
            string query = "SELECT * FROM Notifications WHERE DoctorId = @DoctorId ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Notification>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapNotification(row));
                }
            }
            return list;
        }

        public List<Notification> GetAdminNotifications()
        {
            string query = "SELECT * FROM Notifications WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin' ORDER BY CreatedDate DESC";
            var table = _db.ExecuteDataTable(query);
            var list = new List<Notification>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapNotification(row));
                }
            }
            return list;
        }

        public void MarkAdminNotificationsAsRead()
        {
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin'";
            _db.ExecuteNonQuery(sql);
        }

        public List<Notification> GetNotificationsByRole(string role)
        {
            string query = "SELECT * FROM Notifications WHERE TargetRole = @Role ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@Role", role) };
            var table = _db.ExecuteDataTable(query, parameters);
            var list = new List<Notification>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapNotification(row));
                }
            }
            return list;
        }

        public void MarkRoleNotificationsAsRead(string role)
        {
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE TargetRole = @Role";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Role", role) });
        }

        public void CreateNotification(Notification n)
        {
            string query = "INSERT INTO Notifications (PatientId, DoctorId, TargetRole, Title, Message, CreatedDate, IsRead) VALUES (@PatientId, @DoctorId, @TargetRole, @Title, @Message, @CreatedDate, @IsRead)";
            _db.ExecuteNonQuery(query, new[] {
                new SqlParameter("@PatientId", (object?)n.PatientId ?? DBNull.Value),
                new SqlParameter("@DoctorId", (object?)n.DoctorId ?? DBNull.Value),
                new SqlParameter("@TargetRole", (object?)n.TargetRole ?? DBNull.Value),
                new SqlParameter("@Title", n.Title),
                new SqlParameter("@Message", n.Message),
                new SqlParameter("@CreatedDate", n.CreatedDate),
                new SqlParameter("@IsRead", n.IsRead)
            });
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
