CREATE TABLE Bills (
    BillId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    DueAmount DECIMAL(18,2),
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Paid, Partial
    BillDate DATETIME DEFAULT GETDATE(),
    -- FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) -- Loose coupling
);
