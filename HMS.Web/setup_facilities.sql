CREATE TABLE RoomTypes (
    RoomTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL,
    DailyRate DECIMAL(18, 2) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE Wards (
    WardId INT IDENTITY(1,1) PRIMARY KEY,
    WardName NVARCHAR(100) NOT NULL,
    Floor NVARCHAR(50),
    Wing NVARCHAR(50),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Rooms (
    RoomId INT IDENTITY(1,1) PRIMARY KEY,
    WardId INT,
    RoomNumber NVARCHAR(50) NOT NULL,
    RoomTypeId INT,
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (WardId) REFERENCES Wards(WardId),
    FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId)
);

CREATE TABLE Beds (
    BedId INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT,
    BedNumber NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Available', -- Available, Occupied, Maintenance
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);

-- Seed Data
INSERT INTO RoomTypes (TypeName, DailyRate, Description) VALUES 
('General Ward', 1500.00, 'Standard shared ward bed'),
('Private Room', 5000.00, 'Private room with attached bath'),
('ICU', 15000.00, 'Intensive Care Unit');

INSERT INTO Wards (WardName, Floor, Wing) VALUES 
('General Medicine', '1st Floor', 'East'),
('Cardiology', '2nd Floor', 'West'),
('Pediatrics', '1st Floor', 'North');
