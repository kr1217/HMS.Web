-- Create DoctorShifts table for managing doctor schedules
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DoctorShifts')
BEGIN
    CREATE TABLE DoctorShifts (
        ShiftId INT PRIMARY KEY IDENTITY(1,1),
        DoctorId INT NOT NULL,
        DayOfWeek NVARCHAR(20) NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        ShiftType NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId) ON DELETE CASCADE
    );

    PRINT 'DoctorShifts table created successfully.';
END
ELSE
BEGIN
    PRINT 'DoctorShifts table already exists.';
END
GO
