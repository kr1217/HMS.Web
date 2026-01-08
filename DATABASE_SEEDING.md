# Database Seeding Guide

## Overview
The HMS.Web application includes a comprehensive database seeder that populates all tables with realistic test data.

## Seeding Statistics
- **Total entries per table**: 1,000
- **Working user profiles**: 41 (1 Admin + 10 Doctors + 10 Patients + 10 Tellers + 10 OT Staff)

## How to Seed the Database

### Method 1: Using the Admin Panel (Recommended)
1. Login as an admin user
2. Navigate to **Admin → Seed Database** from the sidebar
3. Click the "Start Seeding" button
4. Wait for the process to complete (may take 5-10 minutes)

### Method 2: Programmatically
```csharp
var seeder = new DatabaseSeeder(databaseHelper, userManager, roleManager);
await seeder.SeedAllData();
```

## Test User Credentials

All test users have been created with the following pattern:

### Admin Account
- **Email**: `admin@antigravity.hospital`
- **Password**: `Admin@123`
- **Count**: 1

### Doctor Accounts
- **Email Pattern**: `doctor1@antigravity.hospital` to `doctor10@antigravity.hospital`
- **Password**: `Doctor@123`
- **Count**: 10
- **Features**: Complete profiles with specializations, shifts, consultation fees, and medical license numbers

### Patient Accounts
- **Email Pattern**: `patient1@antigravity.hospital` to `patient10@antigravity.hospital`
- **Password**: `Patient@123`
- **Count**: 10
- **Features**: Complete profiles with medical history, emergency contacts, and CNIC

### Teller Accounts
- **Email Pattern**: `teller1@antigravity.hospital` to `teller10@antigravity.hospital`
- **Password**: `Teller@123`
- **Count**: 10
- **Features**: Complete staff profiles with shift assignments

### OT Staff Accounts
- **Email Pattern**: `otstaff1@antigravity.hospital` to `otstaff10@antigravity.hospital`
- **Password**: `OTStaff@123`
- **Count**: 10
- **Features**: Complete staff profiles for operation theater management

## Seeded Tables

The seeder populates the following tables with 1,000 entries each:

### Core Infrastructure
- **Wards**: Hospital wards with floor and wing information
- **RoomTypes**: Different room categories (General, Private, Deluxe, ICU, CCU)
- **Rooms**: Individual rooms mapped to wards
- **Beds**: Bed inventory with availability status
- **OperationTheaters**: Surgical theaters with status tracking
- **OperationPackages**: Pre-defined surgery packages with costs

### User Profiles
- **Patients**: Patient records with complete medical and personal information
- **Doctors**: Doctor profiles with specializations, qualifications, and schedules
- **Staff**: Administrative and support staff records

### Operational Data
- **DoctorShifts**: Doctor availability schedules
- **Appointments**: Patient-doctor appointments with various statuses
- **Prescriptions**: Medical prescriptions with medication details
- **Reports**: Medical test reports (Blood Test, X-Ray, MRI, CT Scan, etc.)
- **Admissions**: Patient admission records
- **PatientOperations**: Scheduled and completed surgeries
- **Bills**: Patient billing records
- **BillItems**: Itemized bill entries
- **Payments**: Payment transactions
- **UserShifts**: Teller shift records
- **Notifications**: System notifications for users
- **SupportTickets**: Patient support requests
- **DoctorPayments**: Doctor compensation records

## Data Characteristics

### Realistic Data Generation
The seeder uses realistic Pakistani names, cities, and contact information:
- **Names**: Common Pakistani first and last names
- **Cities**: Major Pakistani cities (Karachi, Lahore, Islamabad, etc.)
- **Phone Numbers**: Pakistani mobile number format (03XXXXXXXXX)
- **CNIC**: Pakistani CNIC format (XXXXX-XXXXXXX-X)
- **Blood Groups**: All standard blood types (A+, A-, B+, B-, AB+, AB-, O+, O-)

### Date Ranges
- **Appointments**: -30 to +30 days from current date
- **Admissions**: Last 365 days
- **Bills**: Last 180 days
- **Operations**: -30 to +60 days from current date
- **Prescriptions**: Last 90 days
- **Reports**: Last 180 days

### Financial Data
- **Consultation Fees**: PKR 1,000 - 5,000
- **Room Rates**: PKR 1,000 - 10,000 per day
- **Operation Costs**: PKR 50,000 - 500,000
- **Bill Amounts**: PKR 5,000 - 100,000
- **Staff Salaries**: PKR 30,000 - 150,000

## Important Notes

1. **Backup First**: Always backup your database before seeding
2. **Duplicate Prevention**: The seeder checks existing record counts to avoid duplicates
3. **Performance**: Seeding 1,000 entries per table may take 5-10 minutes
4. **Identity Integration**: The first 10 profiles of each role are linked to actual Identity users
5. **Additional Records**: Records beyond the first 10 use generated GUIDs for user references

## Troubleshooting

### Seeding Fails
- Check database connection string
- Ensure all tables exist (run migrations first)
- Verify admin user has proper permissions

### Slow Performance
- This is normal for large data sets
- Consider seeding in batches if needed
- Ensure database has adequate resources

### Duplicate Data
- The seeder checks for existing records
- If tables already have 1,000+ records, seeding is skipped for that table

## Development Tips

### Quick Test Login
For quick testing, use these credentials:
```
Doctor: doctor1@antigravity.hospital / Doctor@123
Patient: patient1@antigravity.hospital / Patient@123
Admin: admin@antigravity.hospital / Admin@123
```

### Resetting Data
To reset and re-seed:
1. Truncate all tables (except AspNetUsers, AspNetRoles, AspNetUserRoles)
2. Run the seeder again

### Custom Seeding
To modify seeding behavior, edit `Services/DatabaseSeeder.cs`

## Employee ID Format (For Registration)

When registering new staff members, use these ID prefixes:
- **Doctors**: `DR-001`, `DR-002`, etc.
- **Admin Staff**: `AS-001`, `AS-002`, etc.
- **OT Staff**: `OS-001`, `OS-002`, etc.
- **Tellers**: `FB-001`, `FB-002`, etc. (Financial Bureau)
- **Patients**: Leave Employee ID empty

---

**Last Updated**: January 2026
**Version**: 1.0
