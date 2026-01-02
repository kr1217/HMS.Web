CREATE TABLE Admissions (
    AdmissionId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    BedId INT NOT NULL,
    AdmissionDate DATETIME DEFAULT GETDATE(),
    DischargeDate DATETIME NULL,
    Status NVARCHAR(50) DEFAULT 'Admitted', -- Admitted, Discharged
    Notes NVARCHAR(MAX),
    FOREIGN KEY (BedId) REFERENCES Beds(BedId)
    -- FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) -- Assuming PatientId is trusted or add constraint if table exists
    -- Keeping it loose for now as Patient table might be created via EF or Identity mixed
);

-- Index for fast lookup
CREATE INDEX IX_Admissions_PatientId ON Admissions(PatientId);
CREATE INDEX IX_Admissions_BedId ON Admissions(BedId);
