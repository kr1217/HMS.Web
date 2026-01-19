IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PatientLossEvents]') AND type in (N'U'))
BEGIN
CREATE TABLE PatientLossEvents (
    LossId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NULL,
    AttemptedAt DATETIME NOT NULL DEFAULT GETDATE(),
    EntryPoint NVARCHAR(50) NOT NULL, -- Appointment, Surgery, Admission
    RequestedDepartmentId INT NULL,
    RequestedSpecialization NVARCHAR(100) NULL,
    RequestedProcedureId INT NULL,
    RequestedDoctorId INT NULL,
    LossReasonCode NVARCHAR(50) NOT NULL,
    LossReasonDetail NVARCHAR(255) NULL,
    EstimatedValue DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Lost', -- Lost, Recovered
    RecordedBy NVARCHAR(50) NOT NULL DEFAULT 'System'
);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HospitalOpportunities]') AND type in (N'U'))
BEGIN
CREATE TABLE HospitalOpportunities (
    OpportunityId INT IDENTITY(1,1) PRIMARY KEY,
    Type NVARCHAR(50) NOT NULL, -- Department, Specialist, Equipment
    Target NVARCHAR(100) NOT NULL,
    LossCount INT NOT NULL,
    EstimatedMonthlyRevenue DECIMAL(18,2) NOT NULL,
    EstimatedAnnualRevenue DECIMAL(18,2) NOT NULL,
    ConfidenceScore DECIMAL(5,2) NOT NULL, -- Percentage
    Status NVARCHAR(20) NOT NULL DEFAULT 'New', -- New, Reviewed, Approved, Rejected
    GeneratedAt DATETIME NOT NULL DEFAULT GETDATE()
);
END
