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
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'ShiftId')
                BEGIN
                    ALTER TABLE [dbo].[Bills] ADD [ShiftId] [int] NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [dbo].[Bills] ADD [CreatedBy] [nvarchar](450) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'CommissionRate')
                BEGIN
                    ALTER TABLE [dbo].[Doctors] ADD [CommissionRate] [decimal](5, 2) NOT NULL DEFAULT 100.00;
                END

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DoctorPayments]') AND type in (N'U'))
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
            db.ExecuteNonQuery(createShifts);

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
        }
    }
}
