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
        }
    }
}
