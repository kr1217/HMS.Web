-- Comprehensive Hospital Facilities Seeding
USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'SEEDING HOSPITAL FACILITIES';
PRINT '========================================';

-- Clear existing facility data
DELETE FROM Admissions;
DELETE FROM Beds;
DELETE FROM Rooms;
DELETE FROM Wards;
DELETE FROM RoomTypes;
GO

-- ============================================
-- SEED ROOM TYPES
-- ============================================
INSERT INTO RoomTypes (TypeName, DailyRate, Description)
VALUES 
    ('General Ward', 1500.00, 'Standard multi-bed ward with basic amenities'),
    ('Semi-Private', 3000.00, 'Two-bed room with attached bathroom'),
    ('Private Deluxe', 5000.00, 'Single occupancy with premium amenities'),
    ('ICU', 8000.00, 'Intensive Care Unit with 24/7 monitoring'),
    ('NICU', 10000.00, 'Neonatal Intensive Care Unit'),
    ('CCU', 9000.00, 'Cardiac Care Unit'),
    ('VIP Suite', 15000.00, 'Luxury suite with separate living area'),
    ('Isolation', 6000.00, 'Isolation room for infectious cases');
GO

PRINT 'Seeded 8 Room Types';
GO

-- ============================================
-- SEED WARDS (Realistic Hospital Structure)
-- ============================================
INSERT INTO Wards (WardName, Floor, Wing, IsActive)
VALUES 
    -- Ground Floor
    ('Emergency Ward', 'Ground Floor', 'East Wing', 1),
    ('Outpatient Department', 'Ground Floor', 'West Wing', 1),
    
    -- First Floor
    ('General Medicine', '1st Floor', 'North Wing', 1),
    ('General Surgery', '1st Floor', 'South Wing', 1),
    ('Pediatrics', '1st Floor', 'East Wing', 1),
    
    -- Second Floor
    ('Cardiology', '2nd Floor', 'North Wing', 1),
    ('Orthopedics', '2nd Floor', 'South Wing', 1),
    ('Neurology', '2nd Floor', 'East Wing', 1),
    ('Dermatology', '2nd Floor', 'West Wing', 1),
    
    -- Third Floor
    ('ICU', '3rd Floor', 'North Wing', 1),
    ('CCU', '3rd Floor', 'South Wing', 1),
    ('NICU', '3rd Floor', 'East Wing', 1),
    
    -- Fourth Floor
    ('Maternity', '4th Floor', 'North Wing', 1),
    ('Gynecology', '4th Floor', 'South Wing', 1),
    
    -- Fifth Floor
    ('VIP Suites', '5th Floor', 'Entire Floor', 1);
GO

PRINT 'Seeded 15 Wards across 6 floors';
GO

-- ============================================
-- SEED ROOMS (10 rooms per ward = 150 rooms)
-- ============================================
DECLARE @WardId INT;
DECLARE @WardName NVARCHAR(100);
DECLARE @RoomCounter INT;
DECLARE @RoomTypeId INT;

DECLARE ward_cursor CURSOR FOR 
SELECT WardId, WardName FROM Wards;

OPEN ward_cursor;
FETCH NEXT FROM ward_cursor INTO @WardId, @WardName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoomCounter = 1;
    
    -- Determine room type based on ward
    SET @RoomTypeId = CASE 
        WHEN @WardName LIKE '%ICU%' THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'ICU')
        WHEN @WardName LIKE '%CCU%' THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'CCU')
        WHEN @WardName LIKE '%NICU%' THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'NICU')
        WHEN @WardName LIKE '%VIP%' THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'VIP Suite')
        WHEN @WardName LIKE '%Emergency%' THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'General Ward')
        WHEN @WardName IN ('Maternity', 'Gynecology') THEN (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'Semi-Private')
        ELSE (SELECT RoomTypeId FROM RoomTypes WHERE TypeName = 'General Ward')
    END;
    
    WHILE @RoomCounter <= 10
    BEGIN
        -- Create room number based on ward ID and counter
        DECLARE @RoomNumber NVARCHAR(50) = CAST(@WardId AS NVARCHAR) + RIGHT('00' + CAST(@RoomCounter AS NVARCHAR), 2);
        
        INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive)
        VALUES (@WardId, @RoomNumber, @RoomTypeId, 1);
        
        SET @RoomCounter = @RoomCounter + 1;
    END;
    
    FETCH NEXT FROM ward_cursor INTO @WardId, @WardName;
END;

CLOSE ward_cursor;
DEALLOCATE ward_cursor;
GO

PRINT 'Seeded 150 Rooms (10 per ward)';
GO

-- ============================================
-- SEED BEDS (4 beds per room = 600 beds)
-- ============================================
DECLARE @RoomId INT;
DECLARE @RoomNum NVARCHAR(50);
DECLARE @BedCounter INT;

DECLARE room_cursor CURSOR FOR 
SELECT RoomId, RoomNumber FROM Rooms;

OPEN room_cursor;
FETCH NEXT FROM room_cursor INTO @RoomId, @RoomNum;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @BedCounter = 1;
    
    WHILE @BedCounter <= 4
    BEGIN
        DECLARE @BedNumber NVARCHAR(50) = @RoomNum + '-' + CHAR(64 + @BedCounter); -- A, B, C, D
        
        INSERT INTO Beds (RoomId, BedNumber, Status, IsActive)
        VALUES (
            @RoomId, 
            @BedNumber, 
            'Available', 
            1
        );
        
        SET @BedCounter = @BedCounter + 1;
    END;
    
    FETCH NEXT FROM room_cursor INTO @RoomId, @RoomNum;
END;

CLOSE room_cursor;
DEALLOCATE room_cursor;
GO

PRINT 'Seeded 600 Beds (4 per room)';
GO

-- ============================================
-- MARK SOME BEDS AS OCCUPIED (50% occupancy)
-- ============================================
UPDATE TOP (300) Beds 
SET Status = 'Occupied' 
WHERE Status = 'Available';

-- Mark some beds as under maintenance
UPDATE TOP (20) Beds 
SET Status = 'Maintenance' 
WHERE Status = 'Available';

-- Mark some beds as cleaning
UPDATE TOP (10) Beds 
SET Status = 'Cleaning' 
WHERE Status = 'Available';
GO

PRINT 'Updated Bed Statuses: 300 Occupied, 20 Maintenance, 10 Cleaning, 270 Available';
GO

PRINT '========================================';
PRINT 'FACILITIES SEEDING COMPLETE!';
PRINT '========================================';
PRINT 'Summary:';
PRINT '- Wards: 15 (across 6 floors)';
PRINT '- Rooms: 150 (10 per ward)';
PRINT '- Beds: 600 (4 per room)';
PRINT '- Bed Occupancy: 50% (300/600)';
PRINT '========================================';
GO
