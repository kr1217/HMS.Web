CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    BillId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentDate DATETIME DEFAULT GETDATE(),
    ReferenceNumber NVARCHAR(100) NULL,
    TellerId NVARCHAR(450) NOT NULL,
    ShiftId INT NOT NULL,
    Remarks NVARCHAR(MAX) NULL,
    FOREIGN KEY (ShiftId) REFERENCES UserShifts(ShiftId)
);
