-- Seed Doctor Shifts for all doctors
USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'SEEDING DOCTOR SHIFTS';
PRINT '========================================';

-- Seed shifts for all doctors (Monday to Friday, 9 AM - 5 PM)
DECLARE @DoctorId INT;
DECLARE @DayOfWeek NVARCHAR(20);
DECLARE @ShiftType NVARCHAR(50);

DECLARE doctor_cursor CURSOR FOR 
SELECT DoctorId FROM Doctors WHERE Email != 'admin@hospital.com';

OPEN doctor_cursor;
FETCH NEXT FROM doctor_cursor INTO @DoctorId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Create shifts for weekdays
    DECLARE @DayCounter INT = 1;
    
    WHILE @DayCounter <= 5 -- Monday to Friday
    BEGIN
        SET @DayOfWeek = CASE @DayCounter
            WHEN 1 THEN 'Monday'
            WHEN 2 THEN 'Tuesday'
            WHEN 3 THEN 'Wednesday'
            WHEN 4 THEN 'Thursday'
            WHEN 5 THEN 'Friday'
        END;
        
        -- Vary shift types for realism
        SET @ShiftType = CASE (@DoctorId % 3)
            WHEN 0 THEN 'Morning'
            WHEN 1 THEN 'Full Day'
            ELSE 'Evening'
        END;
        
        -- Insert shift
        INSERT INTO DoctorShifts (
            DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt
        )
        VALUES (
            @DoctorId,
            @DayOfWeek,
            CASE @ShiftType
                WHEN 'Morning' THEN '09:00:00'
                WHEN 'Evening' THEN '14:00:00'
                ELSE '09:00:00'
            END,
            CASE @ShiftType
                WHEN 'Morning' THEN '13:00:00'
                WHEN 'Evening' THEN '20:00:00'
                ELSE '17:00:00'
            END,
            @ShiftType,
            1,
            'Regular shift schedule',
            GETDATE()
        );
        
        SET @DayCounter = @DayCounter + 1;
    END;
    
    -- Some doctors also work Saturdays
    IF @DoctorId % 3 = 0
    BEGIN
        INSERT INTO DoctorShifts (
            DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt
        )
        VALUES (
            @DoctorId,
            'Saturday',
            '09:00:00',
            '13:00:00',
            'Morning',
            1,
            'Weekend shift',
            GETDATE()
        );
    END;
    
    FETCH NEXT FROM doctor_cursor INTO @DoctorId;
END;

CLOSE doctor_cursor;
DEALLOCATE doctor_cursor;
GO

PRINT 'Seeded doctor shifts for all doctors';
PRINT 'Each doctor has 5-6 shifts per week';
GO

-- Display summary
SELECT 
    'Total Shifts Created' as Info,
    COUNT(*) as Count
FROM DoctorShifts;

SELECT 
    'Shifts by Day' as Info,
    DayOfWeek,
    COUNT(*) as ShiftCount
FROM DoctorShifts
GROUP BY DayOfWeek
ORDER BY 
    CASE DayOfWeek
        WHEN 'Monday' THEN 1
        WHEN 'Tuesday' THEN 2
        WHEN 'Wednesday' THEN 3
        WHEN 'Thursday' THEN 4
        WHEN 'Friday' THEN 5
        WHEN 'Saturday' THEN 6
        WHEN 'Sunday' THEN 7
    END;
GO

PRINT '========================================';
PRINT 'DOCTOR SHIFTS SEEDING COMPLETE!';
PRINT '========================================';
GO
