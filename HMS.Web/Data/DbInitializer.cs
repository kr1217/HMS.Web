using HMS.Web.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HMS.Web.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();

            var createPrescriptions = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Prescriptions](
                        [PrescriptionId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PatientId] [int] NOT NULL,
                        [DoctorId] [int] NOT NULL,
                        [AppointmentId] [int] NULL,
                        [Details] [nvarchar](max) NULL,
                        [PrescribedDate] [datetime] NOT NULL,
                        [Medications] [nvarchar](max) NULL,
                        [IsLocked] [bit] NOT NULL DEFAULT 0,
                        [DigitalSignature] [nvarchar](max) NULL
                    )
                END";
            db.ExecuteNonQuery(createPrescriptions);

            var createNotifications = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Notifications](
                        [NotificationId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PatientId] [int] NULL,
                        [DoctorId] [int] NULL,
                        [Title] [nvarchar](250) NULL,
                        [Message] [nvarchar](max) NULL,
                        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
                        [IsRead] [bit] NOT NULL DEFAULT 0
                    )
                END";

            db.ExecuteNonQuery(createNotifications);

            var updatePatientOperations = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'DoctorId')
                BEGIN
                    ALTER TABLE [dbo].[PatientOperations] ADD [DoctorId] [int] NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'Urgency')
                BEGIN
                    ALTER TABLE [dbo].[PatientOperations] ADD [Urgency] [nvarchar](50) NULL;
                END

                -- Allow NULL PackageId for Custom Operations
                ALTER TABLE [dbo].[PatientOperations] ALTER COLUMN [PackageId] [int] NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'OperationName')
                BEGIN
                    ALTER TABLE [dbo].[PatientOperations] ADD [OperationName] [nvarchar](255) NULL;
                END
                ";
            db.ExecuteNonQuery(updatePatientOperations);

            var createStaff = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Staff]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Staff](
                        [StaffId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] [nvarchar](450) NULL,
                        [FullName] [nvarchar](100) NOT NULL,
                        [Role] [nvarchar](50) NOT NULL,
                        [Department] [nvarchar](100) NULL,
                        [Shift] [nvarchar](50) NULL,
                        [Salary] [decimal](18, 2) NOT NULL DEFAULT 0,
                        [JoinDate] [datetime] NOT NULL DEFAULT GETDATE(),
                        [IsActive] [bit] NOT NULL DEFAULT 1,
                        [Email] [nvarchar](256) NULL,
                        [PhoneNumber] [nvarchar](50) NULL
                    )
                END";
            db.ExecuteNonQuery(createStaff);

            var createShifts = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserShifts]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserShifts](
                        [ShiftId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] [nvarchar](450) NOT NULL,
                        [StartTime] [datetime] NOT NULL DEFAULT GETDATE(),
                        [EndTime] [datetime] NULL,
                        [StartingCash] [decimal](18, 2) NOT NULL DEFAULT 0,
                        [EndingCash] [decimal](18, 2) NULL,
                        [ActualCash] [decimal](18, 2) NULL,
                        [Status] [nvarchar](50) NOT NULL DEFAULT 'Open',
                        [Notes] [nvarchar](max) NULL
                    )
                END";
            db.ExecuteNonQuery(createShifts);

            var shiftIdCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'ShiftId'");
            if (shiftIdCol == null)
            {
                var addShiftId = "ALTER TABLE [dbo].[Bills] ADD [ShiftId] [int] NULL;";
                db.ExecuteNonQuery(addShiftId);
            }

            var createdByCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'CreatedBy'");
            if (createdByCol == null)
            {
                var addCreatedBy = "ALTER TABLE [dbo].[Bills] ADD [CreatedBy] [nvarchar](450) NULL;";
                db.ExecuteNonQuery(addCreatedBy);
            }

            var admissionIdCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'AdmissionId'");
            if (admissionIdCol == null)
            {
                var addAdmissionId = "ALTER TABLE [dbo].[Bills] ADD [AdmissionId] [int] NULL;";
                db.ExecuteNonQuery(addAdmissionId);
            }

            var dailyRateCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Admissions]') AND name = 'DailyRate'");
            if (dailyRateCol == null)
            {
                var addDailyRate = "ALTER TABLE [dbo].[Admissions] ADD [DailyRate] [decimal](18, 2) NOT NULL DEFAULT 500;";
                db.ExecuteNonQuery(addDailyRate);
            }

            var createBillItems = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillItems]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[BillItems](
                        [BillItemId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [BillId] [int] NOT NULL,
                        [Description] [nvarchar](255) NOT NULL,
                        [Amount] [decimal](18, 2) NOT NULL,
                        [Category] [nvarchar](50) NOT NULL DEFAULT 'General',
                        FOREIGN KEY (BillId) REFERENCES Bills(BillId)
                    )
                END";
            db.ExecuteNonQuery(createBillItems);

            // Add Payments Table
            var createPayments = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Payments] (
                        [PaymentId] [int] IDENTITY(1,1) PRIMARY KEY,
                        [BillId] [int] NOT NULL,
                        [Amount] [decimal](18,2) NOT NULL,
                        [PaymentMethod] [nvarchar](50) NOT NULL,
                        [PaymentDate] [datetime] DEFAULT GETDATE(),
                        [ReferenceNumber] [nvarchar](100) NULL,
                        [TellerId] [nvarchar](450) NOT NULL,
                        [ShiftId] [int] NOT NULL,
                        [Remarks] [nvarchar](MAX) NULL,
                        FOREIGN KEY (ShiftId) REFERENCES UserShifts(ShiftId)
                    );
                END";
            db.ExecuteNonQuery(createPayments);

            var doctorCommissionCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'CommissionRate'");
            if (doctorCommissionCol == null)
            {
                var addCommission = "ALTER TABLE [dbo].[Doctors] ADD [CommissionRate] [decimal](5, 2) NOT NULL DEFAULT 100.00;";
                db.ExecuteNonQuery(addCommission);
            }

            var doctorIdCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'DoctorId'");
            if (doctorIdCol == null)
            {
                var addDoctorId = @"ALTER TABLE [dbo].[PatientOperations] ADD 
                                    [DoctorId] [int] NOT NULL DEFAULT 0,
                                    [Urgency] [nvarchar](50) NULL,
                                    [OperationName] [nvarchar](255) NULL;";
                db.ExecuteNonQuery(addDoctorId);
            }

            var expStayCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'ExpectedStayDays'");
            if (expStayCol == null)
            {
                var addDetails = @"ALTER TABLE [dbo].[PatientOperations] ADD 
                                    [ExpectedStayDays] [int] NOT NULL DEFAULT 0,
                                    [RecommendedMedicines] [nvarchar](MAX) NULL,
                                    [RecommendedEquipment] [nvarchar](MAX) NULL;";
                db.ExecuteNonQuery(addDetails);
            }

            var costCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'AgreedOperationCost'");
            if (costCol == null)
            {
                var addCosts = @"ALTER TABLE [dbo].[PatientOperations] ADD 
                                    [AgreedOperationCost] [decimal](18, 2) NULL,
                                    [AgreedMedicineCost] [decimal](18, 2) NULL,
                                    [AgreedEquipmentCost] [decimal](18, 2) NULL;";
                db.ExecuteNonQuery(addCosts);
            }

            var createDoctorPayments = @"
                IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DoctorPayments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[DoctorPayments](
                        [PaymentId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [DoctorId] [int] NOT NULL,
                        [Amount] [decimal](18, 2) NOT NULL,
                        [PaymentDate] [datetime] NOT NULL DEFAULT GETDATE(),
                        [PeriodStart] [datetime] NOT NULL,
                        [PeriodEnd] [datetime] NOT NULL,
                        [Status] [nvarchar](50) NOT NULL DEFAULT 'Processed',
                        [Notes] [nvarchar](max) NULL
                    )
                END";
            db.ExecuteNonQuery(createDoctorPayments);

            var createTheaters = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationTheaters]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[OperationTheaters](
                        [TheaterId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [TheaterName] [nvarchar](100) NOT NULL,
                        [Status] [nvarchar](50) NOT NULL DEFAULT 'Available',
                        [IsActive] [bit] NOT NULL DEFAULT 1
                    );

                    INSERT INTO OperationTheaters (TheaterName, Status) VALUES 
                    ('OT-1 (General)', 'Available'),
                    ('OT-2 (Specialized)', 'Available'),
                    ('OT-3 (Emergency)', 'Available'),
                    ('OT-4 (Minor)', 'Available');
                END";
            db.ExecuteNonQuery(createTheaters);

            var theaterIdCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'TheaterId'");
            if (theaterIdCol == null)
            {
                var addTheaterId = @"ALTER TABLE [dbo].[PatientOperations] ADD [TheaterId] [int] NULL;
                                     ALTER TABLE [dbo].[PatientOperations] ADD CONSTRAINT FK_PatientOperations_Theaters FOREIGN KEY (TheaterId) REFERENCES OperationTheaters(TheaterId);";
                db.ExecuteNonQuery(addTheaterId);
            }

            var durationCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'DurationMinutes'");
            if (durationCol == null)
            {
                var addDuration = @"ALTER TABLE [dbo].[PatientOperations] ADD [DurationMinutes] [int] NOT NULL DEFAULT 60;";
                db.ExecuteNonQuery(addDuration);
            }

            var transferCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'IsTransferred'");
            if (transferCol == null)
            {
                db.ExecuteNonQuery("ALTER TABLE PatientOperations ADD IsTransferred BIT NOT NULL DEFAULT 0");
            }

            var actualStartCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND name = 'ActualStartTime'");
            if (actualStartCol == null)
            {
                var addActualStart = @"ALTER TABLE [dbo].[PatientOperations] ADD [ActualStartTime] [datetime] NULL;";
                db.ExecuteNonQuery(addActualStart);
            }

            var targetRoleCol = db.ExecuteScalar("SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'TargetRole'");
            if (targetRoleCol == null)
            {
                var addTargetRole = @"ALTER TABLE [dbo].[Notifications] ADD [TargetRole] [nvarchar](50) NULL;";
                db.ExecuteNonQuery(addTargetRole);
            }

            // Seed Admin User
            try
            {
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

                System.Threading.Tasks.Task.Run(async () =>
                            {
                                if (!await roleManager.RoleExistsAsync("Admin"))
                                {
                                    await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
                                }

                                var adminEmail = "admin@hospital.com";
                                if (await userManager.FindByEmailAsync(adminEmail) == null)
                                {
                                    var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                                    if (result.Succeeded)
                                    {
                                        await userManager.AddToRoleAsync(adminUser, "Admin");

                                        var insertAdminStaff = @"INSERT INTO Staff (UserId, FullName, Role, Department, Salary, IsActive, Email) 
                                                     VALUES (@UserId, 'System Administrator', 'Admin', 'Administration', 100000, 1, @Email)";
                                        db.ExecuteNonQuery(insertAdminStaff, new[] {
                                 new Microsoft.Data.SqlClient.SqlParameter("@UserId", adminUser.Id),
                                 new Microsoft.Data.SqlClient.SqlParameter("@Email", adminEmail)
                                         });
                                    }
                                }
                            }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Log error or ignore if seeding fails (e.g. DB connection issues)
                Console.WriteLine($"Seeding Failed: {ex.Message}");
            }

            // --- SEED DEFAULT PATIENT ---
            int demoPatientId = 0;
            try
            {
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

                System.Threading.Tasks.Task.Run(async () =>
                {
                    if (!await roleManager.RoleExistsAsync("Patient"))
                    {
                        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Patient"));
                    }

                    var patEmail = "patient@hospital.com";
                    var patUser = await userManager.FindByEmailAsync(patEmail);
                    if (patUser == null)
                    {
                        patUser = new ApplicationUser { UserName = patEmail, Email = patEmail, EmailConfirmed = true };
                        var result = await userManager.CreateAsync(patUser, "Patient@123");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(patUser, "Patient");

                            // Insert into Patients Table
                            var insertPat = @"INSERT INTO Patients (UserId, FullName, DateOfBirth, Gender, ContactNumber, Address, BloodGroup) 
                                               OUTPUT INSERTED.PatientId
                                               VALUES (@Uid, 'John Doe', '1985-01-01', 'Male', '555-0199', '123 Health St', 'O+')";

                            var pid = db.ExecuteScalar(insertPat, new[] { new Microsoft.Data.SqlClient.SqlParameter("@Uid", patUser.Id) });
                            demoPatientId = Convert.ToInt32(pid);
                        }
                    }
                    else
                    {
                        // Get existing ID
                        var getPid = "SELECT PatientId FROM Patients WHERE UserId = @Uid";
                        var pid = db.ExecuteScalar(getPid, new[] { new Microsoft.Data.SqlClient.SqlParameter("@Uid", patUser.Id) });
                        if (pid != null) demoPatientId = Convert.ToInt32(pid);
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex) { Console.WriteLine("Patient Seeding Error: " + ex.Message); }

            // --- SEED SIMULATION DATA FOR DEMO ---
            try
            {
                // 1. Ensure Operation Package
                var checkPkg = db.ExecuteScalar("SELECT Count(*) FROM OperationPackages WHERE PackageName = 'Laparoscopic Appendectomy'");
                if (Convert.ToInt32(checkPkg) == 0)
                {
                    db.ExecuteNonQuery("INSERT INTO OperationPackages (PackageName, Description, Cost) VALUES ('Laparoscopic Appendectomy', 'Minimally invasive appendix removal', 5000.00)");
                }

                // 2. Details
                var pidObj = (demoPatientId > 0) ? demoPatientId : db.ExecuteScalar("SELECT TOP 1 PatientId FROM Patients");
                var didObj = db.ExecuteScalar("SELECT TOP 1 DoctorId FROM Doctors");
                var pkgIdObj = db.ExecuteScalar("SELECT TOP 1 PackageId FROM OperationPackages WHERE PackageName = 'Laparoscopic Appendectomy'");

                if (pidObj != null && didObj != null && pkgIdObj != null)
                {
                    int pid = Convert.ToInt32(pidObj);
                    int did = (int)didObj;
                    int pkgId = (int)pkgIdObj;

                    // 3. Create Recommendation
                    var checkOp = db.ExecuteScalar("SELECT Count(*) FROM PatientOperations WHERE PatientId = @P AND PackageId = @Pkg AND Status IN ('Recommended', 'Completed')",
                        new[] { new Microsoft.Data.SqlClient.SqlParameter("@P", pid), new Microsoft.Data.SqlClient.SqlParameter("@Pkg", pkgId) });

                    if (Convert.ToInt32(checkOp) == 0)
                    {
                        // Insert as 'Completed' so it shows up ready for billing in discharge dialog, 
                        // or 'Recommended' if we want the doctor to complete it first. 
                        // Prompt implies "rejection" isn't the focus, so let's say it's ready.
                        string insertOp = @"INSERT INTO PatientOperations 
                            (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, OperationName, ExpectedStayDays, RecommendedMedicines, RecommendedEquipment)
                            VALUES (@Pid, @PkgId, 'Recommended', GETDATE()+1, 'Patient requires surgery.', @Did, 'High', 'Laparoscopic Appendectomy', 3, 'Antibiotics (IV), Painkillers', 'Laparoscopic Set, Sterile Drapes')";

                        db.ExecuteNonQuery(insertOp, new[] {
                            new Microsoft.Data.SqlClient.SqlParameter("@Pid", pid),
                            new Microsoft.Data.SqlClient.SqlParameter("@PkgId", pkgId),
                            new Microsoft.Data.SqlClient.SqlParameter("@Did", did)
                        });
                    }

                    // 4. Admit the patient
                    var checkAdm = db.ExecuteScalar("SELECT Count(*) FROM Admissions WHERE PatientId = @P AND Status = 'Admitted'",
                         new[] { new Microsoft.Data.SqlClient.SqlParameter("@P", pid) });

                    if (Convert.ToInt32(checkAdm) == 0)
                    {
                        var bedIdObj = db.ExecuteScalar("SELECT TOP 1 BedId FROM Beds WHERE Status = 'Available'");
                        if (bedIdObj != null)
                        {
                            int bedId = (int)bedIdObj;
                            db.ExecuteNonQuery("UPDATE Beds SET Status = 'Occupied' WHERE BedId = @B", new[] { new Microsoft.Data.SqlClient.SqlParameter("@B", bedId) });

                            string insertAdm = @"INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, DailyRate)
                                                 VALUES (@Pid, @BedId, GETDATE()-2, 'Admitted', 500.00)";
                            db.ExecuteNonQuery(insertAdm, new[] {
                                new Microsoft.Data.SqlClient.SqlParameter("@Pid", pid),
                                new Microsoft.Data.SqlClient.SqlParameter("@BedId", bedId)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Simulation Seeding Error: " + ex.Message); }
        }
    }
}
