using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using HMS.Web.Data;
using HMS.Web.DAL;
using HMS.Web.Models;

namespace HMS.Web.Services
{
    public class DatabaseSeeder
    {
        private readonly DatabaseHelper _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Random _random = new Random();

        // Sample data arrays
        private readonly string[] _firstNames = { "Ahmed", "Ali", "Fatima", "Ayesha", "Muhammad", "Hassan", "Zainab", "Omar", "Sara", "Bilal", "Amina", "Usman", "Mariam", "Ibrahim", "Khadija", "Abdullah", "Aisha", "Hamza", "Noor", "Yusuf", "Hira", "Imran", "Sana", "Tariq", "Rabia", "Kamran", "Zara", "Adnan", "Hina", "Farhan" };
        private readonly string[] _lastNames = { "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Shah", "Malik", "Raza", "Iqbal", "Siddiqui", "Butt", "Chaudhry", "Mirza", "Naqvi", "Qureshi", "Abbasi", "Rizvi", "Javed", "Aziz", "Mahmood", "Rashid", "Saeed", "Tariq", "Umar", "Waheed", "Yousaf", "Zaman", "Akram", "Bashir", "Farooq" };
        private readonly string[] _cities = { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan", "Peshawar", "Quetta", "Sialkot", "Gujranwala" };
        private readonly string[] _bloodGroups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        private readonly string[] _genders = { "Male", "Female" };
        private readonly string[] _maritalStatuses = { "Single", "Married", "Divorced", "Widowed" };
        private readonly string[] _specializations = { "Cardiology", "Neurology", "Orthopedics", "Pediatrics", "Dermatology", "Gynecology", "ENT", "Ophthalmology", "Psychiatry", "General Surgery" };
        private readonly string[] _qualifications = { "MBBS", "MBBS, FCPS", "MBBS, MD", "MBBS, MS", "MBBS, FRCS", "MBBS, MRCP" };
        private readonly string[] _daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        private readonly string[] _appointmentModes = { "Physical", "Online" };
        private readonly string[] _appointmentStatuses = { "Pending", "Approved", "Completed", "Rejected" };
        private readonly string[] _billStatuses = { "Paid", "Pending", "Partial" };
        private readonly string[] _operationStatuses = { "Scheduled", "In Progress", "Completed" };
        private readonly string[] _urgencyLevels = { "Low", "Medium", "High", "Critical" };
        private readonly string[] _paymentMethods = { "Cash", "Card", "Bank Transfer" };
        private readonly string[] _roomTypes = { "General", "Private", "Deluxe", "ICU", "CCU" };
        private readonly string[] _wards = { "Medical Ward", "Surgical Ward", "Pediatric Ward", "Maternity Ward", "ICU", "CCU", "Emergency Ward" };

        public DatabaseSeeder(DatabaseHelper db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private int GetMaxId(string tableName, string idColumn)
        {
            return Convert.ToInt32(_db.ExecuteScalar($"SELECT ISNULL(MAX({idColumn}), 0) FROM {tableName}"));
        }

        private List<int> GetValidIds(string tableName, string idColumn)
        {
            var query = $"SELECT {idColumn} FROM {tableName}";
            var dt = _db.ExecuteDataTable(query);
            var ids = new List<int>();
            if (dt != null)
            {
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    ids.Add(Convert.ToInt32(row[idColumn]));
                }
            }
            return ids;
        }

        private int GetRandomValidId(List<int> validIds, int defaultValue = 1)
        {
            if (validIds == null || validIds.Count == 0) return defaultValue;
            return validIds[_random.Next(validIds.Count)];
        }


        public async Task SeedAllData()
        {
            Console.WriteLine("Starting database seeding...");

            // 1. Seed Identity Users and Roles
            await SeedIdentityData();

            // 2. Seed Core Tables (no dependencies)
            SeedDepartments();
            SeedWards();
            SeedRoomTypes();
            SeedOperationTheaters();
            SeedOperationPackages();

            // 3. Seed tables with dependencies
            SeedRooms();
            SeedBeds();

            // 4. Seed User Profile Tables
            await SeedPatients();
            await SeedDoctors();
            await SeedStaff();

            // 5. Seed Operational Data
            SeedDoctorShifts();
            SeedAppointments();
            SeedPrescriptions();
            SeedReports();
            SeedAdmissions();
            SeedPatientOperations();
            SeedBills();
            SeedBillItems();
            SeedPayments();
            SeedUserShifts();
            SeedNotifications();
            SeedSupportTickets();
            SeedDoctorPayments();

            Console.WriteLine("Database seeding completed successfully!");
        }

        private void SeedDepartments()
        {
            Console.WriteLine("Seeding Departments...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Departments"));
            if (existingCount >= 10) return;

            string[] departments = { "Cardiology", "Neurology", "Orthopedics", "Pediatrics", "Dermatology",
                                    "Gynecology", "ENT", "Ophthalmology", "Psychiatry", "General Surgery" };

            for (int i = 0; i < departments.Length; i++)
            {
                var query = @"IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentName = @DepartmentName)
                             INSERT INTO Departments (DepartmentName, Description) 
                             VALUES (@DepartmentName, @Description)";

                var parameters = new[]
                {
                    new SqlParameter("@DepartmentName", departments[i]),
                    new SqlParameter("@Description", $"{departments[i]} Department")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Departments seeded.");
        }

        private async Task SeedIdentityData()
        {
            Console.WriteLine("Seeding Identity Users and Roles...");

            // Ensure roles exist
            string[] roles = { "Admin", "Doctor", "Patient", "Teller", "OTStaff" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed 1 Admin
            await CreateUser("admin@antigravity.hospital", "Admin@123", "Admin");

            // Seed 10 Doctors
            for (int i = 1; i <= 10; i++)
            {
                await CreateUser($"doctor{i}@antigravity.hospital", "Doctor@123", "Doctor");
            }

            // Seed 10 Patients
            for (int i = 1; i <= 10; i++)
            {
                await CreateUser($"patient{i}@antigravity.hospital", "Patient@123", "Patient");
            }

            // Seed 10 Tellers
            for (int i = 1; i <= 10; i++)
            {
                await CreateUser($"teller{i}@antigravity.hospital", "Teller@123", "Teller");
            }

            // Seed 10 OT Staff
            for (int i = 1; i <= 10; i++)
            {
                await CreateUser($"otstaff{i}@antigravity.hospital", "OTStaff@123", "OTStaff");
            }

            Console.WriteLine("Identity seeding completed.");
        }

        private async Task CreateUser(string email, string password, string role)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
        }

        private void SeedWards()
        {
            Console.WriteLine("Seeding Wards...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Wards"));
            if (existingCount >= 1000) return;

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Wards (WardName, Floor, Wing, IsActive) 
                             VALUES (@WardName, @Floor, @Wing, @IsActive)";

                var parameters = new[]
                {
                    new SqlParameter("@WardName", _wards[_random.Next(_wards.Length)] + $" {i}"),
                    new SqlParameter("@Floor", $"Floor {_random.Next(1, 10)}"),
                    new SqlParameter("@Wing", _random.Next(2) == 0 ? "East Wing" : "West Wing"),
                    new SqlParameter("@IsActive", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Wards seeded.");
        }

        private void SeedRoomTypes()
        {
            Console.WriteLine("Seeding Room Types...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM RoomTypes"));
            if (existingCount >= 1000) return;

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO RoomTypes (TypeName, DailyRate, Description) 
                             VALUES (@TypeName, @DailyRate, @Description)";

                var typeName = _roomTypes[_random.Next(_roomTypes.Length)];
                var parameters = new[]
                {
                    new SqlParameter("@TypeName", $"{typeName} Type {i}"),
                    new SqlParameter("@DailyRate", _random.Next(1000, 10000)),
                    new SqlParameter("@Description", $"Description for {typeName}")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Room Types seeded.");
        }

        private void SeedRooms()
        {
            Console.WriteLine("Seeding Rooms...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Rooms"));
            if (existingCount >= 1000) return;

            // Get actual valid Ward and RoomType IDs
            var validWardIds = GetValidIds("Wards", "WardId");
            var validRoomTypeIds = GetValidIds("RoomTypes", "RoomTypeId");

            if (validWardIds.Count == 0 || validRoomTypeIds.Count == 0)
            {
                Console.WriteLine("Skipping Rooms: No Wards or RoomTypes found.");
                return;
            }

            Console.WriteLine($"Found {validWardIds.Count} wards and {validRoomTypeIds.Count} room types.");

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive) 
                             VALUES (@WardId, @RoomNumber, @RoomTypeId, @IsActive)";

                var parameters = new[]
                {
                    new SqlParameter("@WardId", GetRandomValidId(validWardIds)),
                    new SqlParameter("@RoomNumber", $"R-{i:D4}"),
                    new SqlParameter("@RoomTypeId", GetRandomValidId(validRoomTypeIds)),
                    new SqlParameter("@IsActive", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Rooms seeded.");
        }

        private void SeedBeds()
        {
            Console.WriteLine("Seeding Beds...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Beds"));
            if (existingCount >= 1000) return;

            var validRoomIds = GetValidIds("Rooms", "RoomId");
            if (validRoomIds.Count == 0)
            {
                Console.WriteLine("Skipping Beds: No Rooms found.");
                return;
            }

            string[] statuses = { "Available", "Occupied", "Maintenance", "Cleaning" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Beds (RoomId, BedNumber, Status, IsActive) 
                             VALUES (@RoomId, @BedNumber, @Status, @IsActive)";

                var parameters = new[]
                {
                    new SqlParameter("@RoomId", GetRandomValidId(validRoomIds)),
                    new SqlParameter("@BedNumber", $"BED-{i:D4}"),
                    new SqlParameter("@Status", statuses[_random.Next(statuses.Length)]),
                    new SqlParameter("@IsActive", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Beds seeded.");
        }

        private void SeedOperationTheaters()
        {
            Console.WriteLine("Seeding Operation Theaters...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM OperationTheaters"));
            if (existingCount >= 1000) return;

            string[] statuses = { "Available", "Maintenance", "InUse" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO OperationTheaters (TheaterName, Status, IsActive) 
                             VALUES (@TheaterName, @Status, @IsActive)";

                var parameters = new[]
                {
                    new SqlParameter("@TheaterName", $"OT-{i:D3}"),
                    new SqlParameter("@Status", statuses[_random.Next(statuses.Length)]),
                    new SqlParameter("@IsActive", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Operation Theaters seeded.");
        }

        private void SeedOperationPackages()
        {
            Console.WriteLine("Seeding Operation Packages...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM OperationPackages"));
            if (existingCount >= 1000) return;

            string[] packageNames = { "Appendectomy", "Cesarean Section", "Hernia Repair", "Cataract Surgery", "Knee Replacement", "Hip Replacement", "Gallbladder Removal", "Tonsillectomy", "Bypass Surgery", "Angioplasty" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO OperationPackages (PackageName, Description, Cost) 
                             VALUES (@PackageName, @Description, @Cost)";

                var packageName = packageNames[_random.Next(packageNames.Length)];
                var parameters = new[]
                {
                    new SqlParameter("@PackageName", $"{packageName} Package {i}"),
                    new SqlParameter("@Description", $"Complete package for {packageName}"),
                    new SqlParameter("@Cost", _random.Next(50000, 500000))
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Operation Packages seeded.");
        }

        private async Task SeedPatients()
        {
            Console.WriteLine("Seeding Patients...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Patients"));
            if (existingCount >= 1000) return;

            var patientUsers = await _userManager.GetUsersInRoleAsync("Patient");

            for (int i = 1; i <= 1000; i++)
            {
                var userId = i <= patientUsers.Count ? patientUsers[i - 1].Id : Guid.NewGuid().ToString();
                var fullName = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}";

                var query = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, CNIC, BloodGroup, MaritalStatus, 
                             EmergencyContactName, EmergencyContactNumber, RelationshipToEmergencyContact, Email, City, Country, RegistrationDate, IsActive)
                             VALUES (@UserId, @FullName, @DateOfBirth, @Gender, @ContactNumber, @Address, @CNIC, @BloodGroup, @MaritalStatus,
                             @EmergencyContactName, @EmergencyContactNumber, @RelationshipToEmergencyContact, @Email, @City, @Country, @RegistrationDate, @IsActive)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@FullName", fullName),
                    new SqlParameter("@DateOfBirth", DateTime.Now.AddYears(-_random.Next(18, 80))),
                    new SqlParameter("@Gender", _genders[_random.Next(_genders.Length)]),
                    new SqlParameter("@ContactNumber", $"03{_random.Next(100000000, 999999999)}"),
                    new SqlParameter("@Address", $"{_random.Next(1, 999)} Street, {_cities[_random.Next(_cities.Length)]}"),
                    new SqlParameter("@CNIC", $"{_random.Next(10000, 99999)}-{_random.Next(1000000, 9999999)}-{_random.Next(1, 9)}"),
                    new SqlParameter("@BloodGroup", _bloodGroups[_random.Next(_bloodGroups.Length)]),
                    new SqlParameter("@MaritalStatus", _maritalStatuses[_random.Next(_maritalStatuses.Length)]),
                    new SqlParameter("@EmergencyContactName", $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}"),
                    new SqlParameter("@EmergencyContactNumber", $"03{_random.Next(100000000, 999999999)}"),
                    new SqlParameter("@RelationshipToEmergencyContact", _random.Next(2) == 0 ? "Spouse" : "Parent"),
                    new SqlParameter("@Email", i <= 10 ? $"patient{i}@antigravity.hospital" : $"patient{i}@example.com"),
                    new SqlParameter("@City", _cities[_random.Next(_cities.Length)]),
                    new SqlParameter("@Country", "Pakistan"),
                    new SqlParameter("@RegistrationDate", DateTime.Now.AddDays(-_random.Next(1, 365))),
                    new SqlParameter("@IsActive", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Patients seeded.");
        }

        private async Task SeedDoctors()
        {
            Console.WriteLine("Seeding Doctors...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Doctors"));
            if (existingCount >= 1000) return;

            var doctorUsers = await _userManager.GetUsersInRoleAsync("Doctor");
            var validDepartmentIds = GetValidIds("Departments", "DepartmentId");

            if (validDepartmentIds.Count == 0)
            {
                Console.WriteLine("Skipping Doctors: No Departments found.");
                return;
            }

            for (int i = 1; i <= 1000; i++)
            {
                var userId = i <= doctorUsers.Count ? doctorUsers[i - 1].Id : Guid.NewGuid().ToString();
                var fullName = $"Dr. {_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}";

                var query = @"INSERT INTO Doctors (UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization, 
                             MedicalLicenseNumber, YearsOfExperience, DepartmentId, HospitalJoiningDate, ConsultationFee, FollowUpFee, 
                             AvailableDays, AvailableTimeSlots, RoomNumber, IsOnCall, IsActive, IsVerified, CommissionRate, IsAvailable)
                             VALUES (@UserId, @FullName, @Gender, @ContactNumber, @Email, @Qualification, @Specialization,
                             @MedicalLicenseNumber, @YearsOfExperience, @DepartmentId, @HospitalJoiningDate, @ConsultationFee, @FollowUpFee,
                             @AvailableDays, @AvailableTimeSlots, @RoomNumber, @IsOnCall, @IsActive, @IsVerified, @CommissionRate, @IsAvailable)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@FullName", fullName),
                    new SqlParameter("@Gender", _genders[_random.Next(_genders.Length)]),
                    new SqlParameter("@ContactNumber", $"03{_random.Next(100000000, 999999999)}"),
                    new SqlParameter("@Email", i <= 10 ? $"doctor{i}@antigravity.hospital" : $"doctor{i}@example.com"),
                    new SqlParameter("@Qualification", _qualifications[_random.Next(_qualifications.Length)]),
                    new SqlParameter("@Specialization", _specializations[_random.Next(_specializations.Length)]),
                    new SqlParameter("@MedicalLicenseNumber", $"PMC-{_random.Next(10000, 99999)}"),
                    new SqlParameter("@YearsOfExperience", _random.Next(1, 30)),
                    new SqlParameter("@DepartmentId", GetRandomValidId(validDepartmentIds)),
                    new SqlParameter("@HospitalJoiningDate", DateTime.Now.AddYears(-_random.Next(1, 20))),
                    new SqlParameter("@ConsultationFee", _random.Next(1000, 5000)),
                    new SqlParameter("@FollowUpFee", _random.Next(500, 2000)),
                    new SqlParameter("@AvailableDays", "Monday,Wednesday,Friday"),
                    new SqlParameter("@AvailableTimeSlots", "09:00-17:00"),
                    new SqlParameter("@RoomNumber", $"R-{_random.Next(100, 999)}"),
                    new SqlParameter("@IsOnCall", _random.Next(2) == 0),
                    new SqlParameter("@IsActive", true),
                    new SqlParameter("@IsVerified", true),
                    new SqlParameter("@CommissionRate", _random.Next(70, 100)),
                    new SqlParameter("@IsAvailable", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Doctors seeded.");
        }

        private async Task SeedStaff()
        {
            Console.WriteLine("Seeding Staff...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Staff"));
            if (existingCount >= 1000) return;

            var tellerUsers = await _userManager.GetUsersInRoleAsync("Teller");
            var otUsers = await _userManager.GetUsersInRoleAsync("OTStaff");
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            string[] roles = { "Teller", "OTStaff", "Admin", "Nurse", "Receptionist", "Pharmacist" };
            string[] shifts = { "Morning", "Evening", "Night" };

            for (int i = 1; i <= 1000; i++)
            {
                var role = roles[_random.Next(roles.Length)];
                string userId;

                if (role == "Teller" && i <= tellerUsers.Count)
                    userId = tellerUsers[i - 1].Id;
                else if (role == "OTStaff" && i <= otUsers.Count)
                    userId = otUsers[i - 1].Id;
                else if (role == "Admin" && i <= adminUsers.Count)
                    userId = adminUsers[i - 1].Id;
                else
                    userId = Guid.NewGuid().ToString();

                var fullName = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}";

                var query = @"INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber)
                             VALUES (@UserId, @FullName, @Role, @Department, @Shift, @Salary, @JoinDate, @IsActive, @Email, @PhoneNumber)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@FullName", fullName),
                    new SqlParameter("@Role", role),
                    new SqlParameter("@Department", _specializations[_random.Next(_specializations.Length)]),
                    new SqlParameter("@Shift", shifts[_random.Next(shifts.Length)]),
                    new SqlParameter("@Salary", _random.Next(30000, 150000)),
                    new SqlParameter("@JoinDate", DateTime.Now.AddYears(-_random.Next(1, 10))),
                    new SqlParameter("@IsActive", true),
                    new SqlParameter("@Email", $"staff{i}@antigravity.hospital"),
                    new SqlParameter("@PhoneNumber", $"03{_random.Next(100000000, 999999999)}")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Staff seeded.");
        }

        private void SeedDoctorShifts()
        {
            Console.WriteLine("Seeding Doctor Shifts...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM DoctorShifts"));
            if (existingCount >= 1000) return;

            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            if (validDoctorIds.Count == 0)
            {
                Console.WriteLine("Skipping Doctor Shifts: No Doctors found.");
                return;
            }

            string[] shiftTypes = { "Morning", "Evening", "Night", "Full Day" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO DoctorShifts (DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes)
                             VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @ShiftType, @IsActive, @Notes)";

                var startHour = _random.Next(6, 18);
                var shiftDuration = _random.Next(4, 8);
                var endHour = Math.Min(startHour + shiftDuration, 23); // Ensure it doesn't exceed 23:59

                var parameters = new[]
                {
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds)),
                    new SqlParameter("@DayOfWeek", _daysOfWeek[_random.Next(_daysOfWeek.Length)]),
                    new SqlParameter("@StartTime", new TimeSpan(startHour, 0, 0)),
                    new SqlParameter("@EndTime", new TimeSpan(endHour, 59, 0)),
                    new SqlParameter("@ShiftType", shiftTypes[_random.Next(shiftTypes.Length)]),
                    new SqlParameter("@IsActive", true),
                    new SqlParameter("@Notes", "Regular shift")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Doctor Shifts seeded.");
        }

        private void SeedAppointments()
        {
            Console.WriteLine("Seeding Appointments...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Appointments"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            if (validPatientIds.Count == 0 || validDoctorIds.Count == 0)
            {
                Console.WriteLine("Skipping Appointments: No Patients or Doctors found.");
                return;
            }

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentMode, Status, Reason)
                             VALUES (@PatientId, @DoctorId, @AppointmentDate, @AppointmentMode, @Status, @Reason)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds)),
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds)),
                    new SqlParameter("@AppointmentDate", DateTime.Now.AddDays(_random.Next(-30, 30))),
                    new SqlParameter("@AppointmentMode", _appointmentModes[_random.Next(_appointmentModes.Length)]),
                    new SqlParameter("@Status", _appointmentStatuses[_random.Next(_appointmentStatuses.Length)]),
                    new SqlParameter("@Reason", "Regular checkup and consultation")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Appointments seeded.");
        }

        private void SeedPrescriptions()
        {
            Console.WriteLine("Seeding Prescriptions...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Prescriptions"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            var validAppointmentIds = GetValidIds("Appointments", "AppointmentId");

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Prescriptions (PatientId, DoctorId, AppointmentId, Details, PrescribedDate, Medications, IsLocked)
                             VALUES (@PatientId, @DoctorId, @AppointmentId, @Details, @PrescribedDate, @Medications, @IsLocked)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds, 1)),
                    new SqlParameter("@AppointmentId", validAppointmentIds.Count > 0 ? GetRandomValidId(validAppointmentIds) : (object)DBNull.Value),
                    new SqlParameter("@Details", "Prescription details and instructions"),
                    new SqlParameter("@PrescribedDate", DateTime.Now.AddDays(-_random.Next(1, 90))),
                    new SqlParameter("@Medications", "[{\"Name\":\"Paracetamol\",\"Dosage\":\"500mg\",\"Frequency\":\"Twice daily\"}]"),
                    new SqlParameter("@IsLocked", true)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Prescriptions seeded.");
        }

        private void SeedReports()
        {
            Console.WriteLine("Seeding Reports...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Reports"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            var validAppointmentIds = GetValidIds("Appointments", "AppointmentId");

            string[] reportTypes = { "Blood Test", "X-Ray", "MRI", "CT Scan", "Ultrasound", "ECG" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Reports (PatientId, DoctorId, AppointmentId, ReportName, ReportType, ReportDate, FilePath, Status)
                             VALUES (@PatientId, @DoctorId, @AppointmentId, @ReportName, @ReportType, @ReportDate, @FilePath, @Status)";

                var reportType = reportTypes[_random.Next(reportTypes.Length)];
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds, 1)),
                    new SqlParameter("@AppointmentId", validAppointmentIds.Count > 0 ? GetRandomValidId(validAppointmentIds) : (object)DBNull.Value),
                    new SqlParameter("@ReportName", $"{reportType} Report {i}"),
                    new SqlParameter("@ReportType", reportType),
                    new SqlParameter("@ReportDate", DateTime.Now.AddDays(-_random.Next(1, 180))),
                    new SqlParameter("@FilePath", $"/reports/report_{i}.pdf"),
                    new SqlParameter("@Status", "Finalized")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Reports seeded.");
        }

        private void SeedAdmissions()
        {
            Console.WriteLine("Seeding Admissions...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Admissions"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validBedIds = GetValidIds("Beds", "BedId");

            for (int i = 1; i <= 1000; i++)
            {
                var admissionDate = DateTime.Now.AddDays(-_random.Next(1, 365));
                var discharged = _random.Next(2) == 0;

                var query = @"INSERT INTO Admissions (PatientId, BedId, AdmissionDate, DischargeDate, Status, Notes)
                             VALUES (@PatientId, @BedId, @AdmissionDate, @DischargeDate, @Status, @Notes)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@BedId", GetRandomValidId(validBedIds, 1)),
                    new SqlParameter("@AdmissionDate", admissionDate),
                    new SqlParameter("@DischargeDate", discharged ? (object)admissionDate.AddDays(_random.Next(1, 30)) : DBNull.Value),
                    new SqlParameter("@Status", discharged ? "Discharged" : "Admitted"),
                    new SqlParameter("@Notes", "Admission notes and observations")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Admissions seeded.");
        }

        private void SeedPatientOperations()
        {
            Console.WriteLine("Seeding Patient Operations...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM PatientOperations"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validPackageIds = GetValidIds("OperationPackages", "PackageId");
            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            var validTheaterIds = GetValidIds("OperationTheaters", "TheaterId");

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, 
                             ExpectedStayDays, TheaterId, DurationMinutes, AgreedOperationCost, IsTransferred)
                             VALUES (@PatientId, @PackageId, @Status, @ScheduledDate, @Notes, @DoctorId, @Urgency,
                             @ExpectedStayDays, @TheaterId, @DurationMinutes, @AgreedOperationCost, @IsTransferred)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@PackageId", GetRandomValidId(validPackageIds, 1)),
                    new SqlParameter("@Status", _operationStatuses[_random.Next(_operationStatuses.Length)]),
                    new SqlParameter("@ScheduledDate", DateTime.Now.AddDays(_random.Next(-30, 60))),
                    new SqlParameter("@Notes", "Operation notes and requirements"),
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds, 1)),
                    new SqlParameter("@Urgency", _urgencyLevels[_random.Next(_urgencyLevels.Length)]),
                    new SqlParameter("@ExpectedStayDays", _random.Next(1, 14)),
                    new SqlParameter("@TheaterId", GetRandomValidId(validTheaterIds, 1)),
                    new SqlParameter("@DurationMinutes", _random.Next(60, 360)),
                    new SqlParameter("@AgreedOperationCost", _random.Next(50000, 500000)),
                    new SqlParameter("@IsTransferred", false)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Patient Operations seeded.");
        }

        private void SeedBills()
        {
            Console.WriteLine("Seeding Bills...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Bills"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validAdmissionIds = GetValidIds("Admissions", "AdmissionId");

            for (int i = 1; i <= 1000; i++)
            {
                var totalAmount = _random.Next(5000, 100000);
                var paidAmount = _random.Next(0, totalAmount);

                var query = @"INSERT INTO Bills (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, AdmissionId)
                             VALUES (@PatientId, @TotalAmount, @PaidAmount, @DueAmount, @Status, @BillDate, @AdmissionId)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@TotalAmount", totalAmount),
                    new SqlParameter("@PaidAmount", paidAmount),
                    new SqlParameter("@DueAmount", totalAmount - paidAmount),
                    new SqlParameter("@Status", paidAmount >= totalAmount ? "Paid" : (paidAmount > 0 ? "Partial" : "Pending")),
                    new SqlParameter("@BillDate", DateTime.Now.AddDays(-_random.Next(1, 180))),
                    new SqlParameter("@AdmissionId", validAdmissionIds.Count > 0 ? GetRandomValidId(validAdmissionIds) : (object)DBNull.Value)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Bills seeded.");
        }

        private void SeedBillItems()
        {
            Console.WriteLine("Seeding Bill Items...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM BillItems"));
            if (existingCount >= 1000) return;

            var validBillIds = GetValidIds("Bills", "BillId");
            string[] categories = { "Room", "Doctor", "Medicine", "Lab", "Surgery", "Equipment" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO BillItems (BillId, Description, Amount, Category)
                             VALUES (@BillId, @Description, @Amount, @Category)";

                var category = categories[_random.Next(categories.Length)];
                var parameters = new[]
                {
                    new SqlParameter("@BillId", GetRandomValidId(validBillIds, 1)),
                    new SqlParameter("@Description", $"{category} charges"),
                    new SqlParameter("@Amount", _random.Next(500, 50000)),
                    new SqlParameter("@Category", category)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Bill Items seeded.");
        }

        private void SeedPayments()
        {
            Console.WriteLine("Seeding Payments...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Payments"));
            if (existingCount >= 1000) return;

            var validBillIds = GetValidIds("Bills", "BillId");
            var validShiftIds = GetValidIds("UserShifts", "ShiftId");

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Payments (BillId, Amount, PaymentMethod, PaymentDate, TellerId, ShiftId, ReferenceNumber)
                             VALUES (@BillId, @Amount, @PaymentMethod, @PaymentDate, @TellerId, @ShiftId, @ReferenceNumber)";

                var parameters = new[]
                {
                    new SqlParameter("@BillId", GetRandomValidId(validBillIds, 1)),
                    new SqlParameter("@Amount", _random.Next(1000, 50000)),
                    new SqlParameter("@PaymentMethod", _paymentMethods[_random.Next(_paymentMethods.Length)]),
                    new SqlParameter("@PaymentDate", DateTime.Now.AddDays(-_random.Next(1, 180))),
                    new SqlParameter("@TellerId", Guid.NewGuid().ToString()),
                    new SqlParameter("@ShiftId", validShiftIds.Count > 0 ? GetRandomValidId(validShiftIds) : 1),
                    new SqlParameter("@ReferenceNumber", $"REF-{_random.Next(100000, 999999)}")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Payments seeded.");
        }

        private void SeedUserShifts()
        {
            Console.WriteLine("Seeding User Shifts...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM UserShifts"));
            if (existingCount >= 1000) return;

            for (int i = 1; i <= 1000; i++)
            {
                var startTime = DateTime.Now.AddDays(-_random.Next(1, 365));
                var ended = _random.Next(2) == 0;

                var query = @"INSERT INTO UserShifts (UserId, StartTime, EndTime, StartingCash, EndingCash, ActualCash, Status, Notes)
                             VALUES (@UserId, @StartTime, @EndTime, @StartingCash, @EndingCash, @ActualCash, @Status, @Notes)";

                var startingCash = _random.Next(10000, 50000);
                var endingCash = ended ? startingCash + _random.Next(0, 100000) : 0;

                var parameters = new[]
                {
                    new SqlParameter("@UserId", Guid.NewGuid().ToString()),
                    new SqlParameter("@StartTime", startTime),
                    new SqlParameter("@EndTime", ended ? (object)startTime.AddHours(8) : DBNull.Value),
                    new SqlParameter("@StartingCash", startingCash),
                    new SqlParameter("@EndingCash", ended ? (object)endingCash : DBNull.Value),
                    new SqlParameter("@ActualCash", ended ? (object)endingCash : DBNull.Value),
                    new SqlParameter("@Status", ended ? "Closed" : "Open"),
                    new SqlParameter("@Notes", "Shift notes")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("User Shifts seeded.");
        }

        private void SeedNotifications()
        {
            Console.WriteLine("Seeding Notifications...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Notifications"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            var validDoctorIds = GetValidIds("Doctors", "DoctorId");
            string[] titles = { "Appointment Reminder", "Bill Payment Due", "Test Results Ready", "Prescription Ready", "Operation Scheduled" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO Notifications (PatientId, DoctorId, Title, Message, CreatedDate, IsRead)
                             VALUES (@PatientId, @DoctorId, @Title, @Message, @CreatedDate, @IsRead)";

                var parameters = new[]
                {
                    new SqlParameter("@PatientId", _random.Next(2) == 0 && validPatientIds.Count > 0 ? (object)GetRandomValidId(validPatientIds) : DBNull.Value),
                    new SqlParameter("@DoctorId", _random.Next(2) == 0 && validDoctorIds.Count > 0 ? (object)GetRandomValidId(validDoctorIds) : DBNull.Value),
                    new SqlParameter("@Title", titles[_random.Next(titles.Length)]),
                    new SqlParameter("@Message", "This is a notification message with important information."),
                    new SqlParameter("@CreatedDate", DateTime.Now.AddDays(-_random.Next(1, 30))),
                    new SqlParameter("@IsRead", _random.Next(2) == 0)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Notifications seeded.");
        }

        private void SeedSupportTickets()
        {
            Console.WriteLine("Seeding Support Tickets...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM SupportTickets"));
            if (existingCount >= 1000) return;

            var validPatientIds = GetValidIds("Patients", "PatientId");
            string[] subjects = { "Billing Issue", "Appointment Problem", "Report Access", "Prescription Query", "General Inquiry" };
            string[] statuses = { "Open", "Closed", "In Progress" };

            for (int i = 1; i <= 1000; i++)
            {
                var query = @"INSERT INTO SupportTickets (PatientId, Subject, Message, Status, CreatedDate, Response)
                             VALUES (@PatientId, @Subject, @Message, @Status, @CreatedDate, @Response)";

                var status = statuses[_random.Next(statuses.Length)];
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", GetRandomValidId(validPatientIds, 1)),
                    new SqlParameter("@Subject", subjects[_random.Next(subjects.Length)]),
                    new SqlParameter("@Message", "This is a support ticket message describing the issue."),
                    new SqlParameter("@Status", status),
                    new SqlParameter("@CreatedDate", DateTime.Now.AddDays(-_random.Next(1, 60))),
                    new SqlParameter("@Response", status == "Closed" ? "Issue has been resolved." : DBNull.Value)
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Support Tickets seeded.");
        }

        private void SeedDoctorPayments()
        {
            Console.WriteLine("Seeding Doctor Payments...");
            var existingCount = Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM DoctorPayments"));
            if (existingCount >= 1000) return;

            var validDoctorIds = GetValidIds("Doctors", "DoctorId");

            for (int i = 1; i <= 1000; i++)
            {
                var periodStart = DateTime.Now.AddMonths(-_random.Next(1, 12));
                var periodEnd = periodStart.AddMonths(1);

                var query = @"INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes)
                             VALUES (@DoctorId, @Amount, @PaymentDate, @PeriodStart, @PeriodEnd, @Status, @Notes)";

                var parameters = new[]
                {
                    new SqlParameter("@DoctorId", GetRandomValidId(validDoctorIds, 1)),
                    new SqlParameter("@Amount", _random.Next(50000, 500000)),
                    new SqlParameter("@PaymentDate", periodEnd),
                    new SqlParameter("@PeriodStart", periodStart),
                    new SqlParameter("@PeriodEnd", periodEnd),
                    new SqlParameter("@Status", "Processed"),
                    new SqlParameter("@Notes", "Monthly payment processed")
                };

                _db.ExecuteNonQuery(query, parameters);
            }
            Console.WriteLine("Doctor Payments seeded.");
        }
    }
}
