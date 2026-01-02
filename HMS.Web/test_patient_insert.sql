-- Minimal Patient Seeding Test
USE [HospitalManagement];
GO

-- Test single insert first
INSERT INTO Patients (
    FullName, DateOfBirth, Gender, ContactNumber, Address,
    CNIC, BloodGroup, MaritalStatus, EmergencyContactName, 
    EmergencyContactNumber, RelationshipToEmergencyContact,
    RegistrationDate, IsActive
)
VALUES (
    'Test Patient 1',
    '1990-01-01',
    'Male',
    '+1234567890',
    '123 Test Street',
    '12345-1234567-1',
    'O+',
    'Single',
    'Emergency Contact',
    '+0987654321',
    'Friend',
    GETDATE(),
    1
);

SELECT 'Test insert successful' as Result, COUNT(*) as PatientCount FROM Patients;
GO
