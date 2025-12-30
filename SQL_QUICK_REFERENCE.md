# Quick Reference: SQL Queries for HMS Setup

## 1. Clean Up Users Without Roles (CRITICAL - Run This First!)

**Purpose**: Removes all users created before the doctor module who don't have assigned roles. This fixes the routing issues.

**File**: `cleanup_users_without_roles.sql`

**Quick Query** (if you want to run it directly):
```sql
USE [HospitalManagement];
GO

SELECT Id INTO #UsersToDelete FROM AspNetUsers 
WHERE Id NOT IN (SELECT UserId FROM AspNetUserRoles);

DELETE FROM [dbo].[Appointments] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
   OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
DELETE FROM [dbo].[Bills] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
DELETE FROM [dbo].[Notifications] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
   OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
DELETE FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUsers WHERE Id IN (SELECT Id FROM #UsersToDelete);

DROP TABLE #UsersToDelete;
GO
```

---

## 2. Create Doctor Shifts Table

**Purpose**: Creates the DoctorShifts table for managing doctor working hours.

**File**: `setup_doctor_shifts.sql`

**Quick Query**:
```sql
USE [HospitalManagement];
GO

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
GO
```

---

## 3. Verify Users and Roles

**Purpose**: Check which users have roles and which don't.

```sql
-- See all users and their roles
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    r.Name AS RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY r.Name, u.UserName;

-- Count users without roles
SELECT COUNT(*) AS UsersWithoutRoles
FROM AspNetUsers 
WHERE Id NOT IN (SELECT UserId FROM AspNetUserRoles);
```

---

## 4. Check Doctor Shifts

**Purpose**: View all doctor shifts in the system.

```sql
-- View all shifts with doctor names
SELECT 
    ds.ShiftId,
    d.FullName AS DoctorName,
    ds.DayOfWeek,
    ds.StartTime,
    ds.EndTime,
    ds.ShiftType,
    ds.IsActive,
    ds.Notes
FROM DoctorShifts ds
JOIN Doctors d ON ds.DoctorId = d.DoctorId
WHERE ds.IsActive = 1
ORDER BY d.FullName, 
    CASE ds.DayOfWeek 
        WHEN 'Monday' THEN 1 
        WHEN 'Tuesday' THEN 2 
        WHEN 'Wednesday' THEN 3 
        WHEN 'Thursday' THEN 4 
        WHEN 'Friday' THEN 5 
        WHEN 'Saturday' THEN 6 
        WHEN 'Sunday' THEN 7 
    END,
    ds.StartTime;
```

---

## 5. Check Appointments vs Shift Availability

**Purpose**: See which appointments fall outside doctor shift hours.

```sql
-- Appointments outside shift hours
SELECT 
    a.AppointmentId,
    d.FullName AS DoctorName,
    p.FullName AS PatientName,
    a.AppointmentDate,
    DATENAME(WEEKDAY, a.AppointmentDate) AS DayOfWeek,
    CAST(a.AppointmentDate AS TIME) AS AppointmentTime,
    a.Status,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM DoctorShifts ds 
            WHERE ds.DoctorId = a.DoctorId 
            AND ds.DayOfWeek = DATENAME(WEEKDAY, a.AppointmentDate)
            AND ds.StartTime <= CAST(a.AppointmentDate AS TIME)
            AND ds.EndTime >= CAST(a.AppointmentDate AS TIME)
            AND ds.IsActive = 1
        ) THEN 'Within Shift'
        ELSE 'Outside Shift'
    END AS AvailabilityStatus
FROM Appointments a
JOIN Doctors d ON a.DoctorId = d.DoctorId
JOIN Patients p ON a.PatientId = p.PatientId
WHERE a.Status = 'Pending'
ORDER BY a.AppointmentDate;
```

---

## Execution Order

**Run these in order for a fresh setup:**

1. ✅ **cleanup_users_without_roles.sql** - Remove problematic users
2. ✅ **setup_doctor_shifts.sql** - Create shifts table
3. ✅ Restart your application
4. ✅ Create new test accounts with proper roles
5. ✅ Test the shift management features

---

## Common Issues & Solutions

### Issue: "Foreign key constraint error"
**Solution**: Make sure the Doctors table exists before creating DoctorShifts table.

### Issue: "Users still routing incorrectly"
**Solution**: 
1. Run the cleanup query again
2. Clear browser cache and cookies
3. Restart the application
4. Create fresh user accounts

### Issue: "Cannot delete users"
**Solution**: The cleanup script handles cascading deletes. If it fails, check for other foreign key constraints.

---

## Backup Recommendation

**Before running cleanup query, backup your database:**

```sql
-- Create a backup
BACKUP DATABASE [HospitalManagement]
TO DISK = 'C:\Backups\HospitalManagement_BeforeCleanup.bak'
WITH FORMAT, INIT, NAME = 'Full Backup Before User Cleanup';
```

---

**Need Help?** Refer to `SHIFT_MANAGEMENT_GUIDE.md` for detailed documentation.
