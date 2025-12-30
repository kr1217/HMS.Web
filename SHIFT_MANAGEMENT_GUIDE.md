# Doctor Shift Management Implementation - Complete Guide

## Overview
This implementation adds comprehensive shift management functionality to the Hospital Management System, allowing doctors to:
- Define their working hours by day of the week
- View shift schedules on their dashboard
- Automatically check appointment availability against shift hours
- Send custom rejection messages to patients with shift information

## Files Modified/Created

### 1. **Models/Entities.cs** - Added DoctorShift Entity
- New `DoctorShift` class with properties:
  - ShiftId, DoctorId, DayOfWeek, StartTime, EndTime
  - ShiftType (Morning/Evening/Night/Full Day)
  - IsActive, Notes, CreatedAt
- Full validation attributes included

### 2. **DAL/Repositories.cs** - Added DoctorShiftRepository
- `GetShiftsByDoctorId()` - Retrieve all active shifts for a doctor
- `GetShiftById()` - Get specific shift details
- `CreateShift()` - Add new shift
- `UpdateShift()` - Modify existing shift
- `DeleteShift()` - Soft delete (sets IsActive = 0)
- `IsAvailableAtTime()` - Check if doctor is available at specific date/time

### 3. **Program.cs** - Registered DoctorShiftRepository
- Added service registration for dependency injection

### 4. **Components/Pages/Doctor/Dashboard.razor** - Complete Overhaul
Enhanced with:
- **New "My Shifts" Tab**: Full CRUD interface for shift management
- **Shift Overview on Dashboard**: Quick view of weekly schedule
- **Enhanced Appointment Requests**:
  - Visual availability indicators (green/yellow alerts)
  - Shows doctor's shifts for the requested day
  - Custom rejection dialog with message input
  - Automatic shift information in rejection messages
- **Improved UI/UX**: Better organization and visual feedback

### 5. **setup_doctor_shifts.sql** - Database Schema
SQL script to create the DoctorShifts table with proper foreign keys

## Database Setup Instructions

### Step 1: Run the Shift Table Creation Script
Execute this SQL script in your database:

```sql
-- File: setup_doctor_shifts.sql
```

### Step 2: Clean Up Users Without Roles (IMPORTANT!)
This removes all users created before the doctor module was implemented who don't have assigned roles:

```sql
-- File: cleanup_users.sql (already exists)
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

**⚠️ WARNING**: This will permanently delete all users without roles and their associated data. Make sure to backup your database first!

## Features Implemented

### 1. Shift Management Interface
- **Add Shifts**: Form with validation for day, start time, end time, shift type, and notes
- **Edit Shifts**: Click edit button to modify existing shifts
- **Delete Shifts**: Soft delete to maintain data integrity
- **Visual Organization**: Shifts sorted by day of week and time

### 2. Availability Checking
- Automatic validation when viewing appointment requests
- Visual indicators:
  - ✅ **Green Alert**: Appointment within shift hours
  - ⚠️ **Yellow Alert**: Appointment outside shift hours with shift details shown

### 3. Enhanced Appointment Handling
- **Approve**: One-click approval with automatic patient notification
- **Reject with Custom Message**: 
  - Modal dialog for detailed rejection reason
  - Shows doctor's shift schedule in the dialog
  - Sends personalized message to patient
  - Useful for suggesting alternative times

### 4. Dashboard Improvements
- **Quick Shift Overview**: See this week's schedule at a glance
- **Better Statistics**: Active cases, completed today, revenue
- **Today's Schedule**: All appointments for the current day

## How to Use (Doctor Workflow)

### Setting Up Shifts
1. Log in as a doctor
2. Navigate to "My Shifts" tab
3. Fill in the shift form:
   - Select day of week
   - Set start and end times
   - Optionally add shift type and notes
4. Click "Add Shift"
5. Repeat for all working days

### Managing Appointment Requests
1. Go to "Appointment Requests" tab
2. Review each request:
   - Check the availability indicator
   - If outside shift hours, see which shifts you have that day
3. **To Approve**: Click "Approve" button
4. **To Reject**: 
   - Click "Reject" button
   - Enter a detailed message explaining why
   - Suggest alternative times based on your shifts
   - Click "Send Rejection"

### Example Rejection Messages
- "I'm not available at 8:00 PM on Monday. My shift ends at 5:00 PM. Please request an appointment between 9:00 AM - 5:00 PM."
- "I don't work on Sundays. I'm available Monday-Friday from 9:00 AM to 5:00 PM. Please reschedule."

## Validation Rules

### Shift Form Validations
- **Day of Week**: Required
- **Start Time**: Required, must be valid time
- **End Time**: Required, must be valid time, should be after start time
- **Shift Type**: Optional
- **Notes**: Optional, max 500 characters

### Business Logic
- Cannot delete shifts (soft delete only)
- Shifts are doctor-specific
- Multiple shifts per day allowed (e.g., split shifts)
- Inactive shifts don't affect availability checking

## Technical Details

### Time Handling
- Uses `TimeSpan` for start/end times
- Supports 24-hour format
- Automatic conversion for display (12-hour with AM/PM)

### Database Relations
- Foreign key: DoctorShifts.DoctorId → Doctors.DoctorId
- Cascade delete: If doctor is deleted, shifts are deleted

### Performance
- Indexed queries for fast shift lookups
- Efficient availability checking with single query
- Minimal database calls per page load

## Next Steps - Phase 2 of Doctor Module

Now that the shift management is complete, you're ready to proceed with Phase 2. Potential features to consider:

1. **Patient Medical Records Management**
2. **Prescription Writing Interface**
3. **Appointment History & Notes**
4. **Video Consultation Integration**
5. **Doctor Analytics & Reports**
6. **Multi-doctor Scheduling Conflicts**
7. **Recurring Shift Patterns**
8. **Holiday/Leave Management**

## Testing Checklist

- [ ] Run `setup_doctor_shifts.sql` to create the table
- [ ] Run `cleanup_users.sql` to remove users without roles
- [ ] Restart the application
- [ ] Log in as a doctor
- [ ] Add at least 3 shifts for different days
- [ ] Edit a shift and verify changes
- [ ] Delete a shift and verify it's removed
- [ ] Create a patient account
- [ ] Request an appointment within shift hours
- [ ] Request an appointment outside shift hours
- [ ] As doctor, approve an appointment
- [ ] As doctor, reject an appointment with custom message
- [ ] Verify patient receives notifications

## Troubleshooting

### Issue: "DoctorShifts table doesn't exist"
**Solution**: Run the `setup_doctor_shifts.sql` script

### Issue: "Users still routing to wrong dashboard"
**Solution**: Run the `cleanup_users.sql` script to remove users without roles

### Issue: "Shift times not saving"
**Solution**: Check that time inputs are in HH:mm format (e.g., 09:00, 17:30)

### Issue: "Availability always shows as unavailable"
**Solution**: Ensure shifts are marked as IsActive = 1 and times are correct

## SQL Query Summary

**To remove all users without defined roles:**
```sql
USE [HospitalManagement];
GO

-- This query identifies and removes all users who don't have a role assigned
-- This fixes the routing issue for users created before the doctor module

SELECT Id INTO #UsersToDelete FROM AspNetUsers 
WHERE Id NOT IN (SELECT UserId FROM AspNetUserRoles);

-- Clean up all related data
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

**Implementation Complete!** 🎉

The doctor shift management system is now fully integrated into your Hospital Management System. Execute the SQL scripts and restart your application to see the new features in action.
