using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid Contact Number.")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string Address { get; set; } = string.Empty;

        // Mandatory Fields
        [Required(ErrorMessage = "CNIC/National ID is required.")]
        public string CNIC { get; set; } = string.Empty;

        [Required(ErrorMessage = "Blood Group is required.")]
        public string BloodGroup { get; set; } = string.Empty;

        [Required(ErrorMessage = "Marital Status is required.")]
        public string MaritalStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emergency Contact Name is required.")]
        public string EmergencyContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emergency Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid Emergency Contact Number.")]
        public string EmergencyContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relationship to Emergency Contact is required.")]
        public string RelationshipToEmergencyContact { get; set; } = string.Empty;

        // Secondary Fields (Optional)
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? DisabilityStatus { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public string? PatientType { get; set; }
        public string? Email { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public string? PrimaryDoctorId { get; set; }
    }

    public class Doctor
    {
        public int DoctorId { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number is required.")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Qualification is required.")]
        public string Qualification { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Medical License Number is required.")]
        public string MedicalLicenseNumber { get; set; } = string.Empty;

        public string? LicenseIssuingAuthority { get; set; }

        [Required(ErrorMessage = "Years of Experience is required.")]
        public int YearsOfExperience { get; set; }

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty; // Joined

        // Professional Practice
        public DateTime HospitalJoiningDate { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal FollowUpFee { get; set; }
        public string AvailableDays { get; set; } = string.Empty;
        public string AvailableTimeSlots { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public bool IsOnCall { get; set; }

        // System Fields
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsAvailable { get; set; }
    }

    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty; // Joined

        [Required(ErrorMessage = "Please select a doctor.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appointment Date is required.")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment Mode is required.")]
        public string AppointmentMode { get; set; } = "Physical";

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Rescheduled, Completed

        [Required(ErrorMessage = "Reason for appointment is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        public string? DoctorNotes { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RescheduledDate { get; set; }
    }

    public class Report
    {
        public int ReportId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Finalized"; // Finalized, Draft
        public string? Observations { get; set; }
    }

    public class Prescription
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime PrescribedDate { get; set; }
        public string Medications { get; set; } = string.Empty; // JSON or CSV of medicines
        public bool IsLocked { get; set; } = false;
        public string? DigitalSignature { get; set; }
    }

    public class Bill
    {
        public int BillId { get; set; }
        public int PatientId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Paid, Pending, Partial
        public DateTime BillDate { get; set; }
    }

    public class OperationPackage
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }

    public class PatientOperation
    {
        public int OperationId { get; set; }
        public int PatientId { get; set; }
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled"; // Scheduled, In Progress, Completed
        public DateTime ScheduledDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class SupportTicket
    {
        public int TicketId { get; set; }
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "Open"; // Open, Closed, In Progress
        public DateTime CreatedDate { get; set; }
        public string? Response { get; set; }
    }

    public class DoctorShift
    {
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "Doctor ID is required.")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Shift day is required.")]
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan EndTime { get; set; }

        public string? ShiftType { get; set; } // Morning, Evening, Night, Full Day

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }

    public class PrescriptionMedication
    {
        public string Name { get; set; } = "";
        public string Dosage { get; set; } = "";
        public string Frequency { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Instructions { get; set; } = "";
    }
}
