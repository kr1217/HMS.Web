
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OperationTheaters')
BEGIN
    CREATE TABLE OperationTheaters (
        TheaterId INT PRIMARY KEY IDENTITY(1,1),
        TheaterName NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) DEFAULT 'Available',
        IsActive BIT DEFAULT 1
    );

    -- Seed Data
    INSERT INTO OperationTheaters (TheaterName, Status) VALUES 
    ('OT-1 (General)', 'Available'),
    ('OT-2 (Specialized)', 'Available'),
    ('OT-3 (Emergency)', 'Available'),
    ('OT-4 (Minor)', 'Available');
END

-- Add TheaterId to PatientOperations if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PatientOperations') AND name = 'TheaterId')
BEGIN
    ALTER TABLE PatientOperations ADD TheaterId INT NULL;
    ALTER TABLE PatientOperations ADD CONSTRAINT FK_PatientOperations_Theaters FOREIGN KEY (TheaterId) REFERENCES OperationTheaters(TheaterId);
END
