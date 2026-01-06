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
        public decimal CommissionRate { get; set; } = 100.00m; // Percentage of consultation fee doctor receives
    }

    public class DoctorPayment
    {
        public int PaymentId { get; set; }
        public int DoctorId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; } = "Processed";
        public string? Notes { get; set; }

        // Optional Joined Properties
        public string? DoctorName { get; set; }
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
        public int? ShiftId { get; set; }
        public string? CreatedBy { get; set; }

        // Optional Joined Properties
        public string? PatientName { get; set; }
        public int? AdmissionId { get; set; }
        public List<BillItem> Items { get; set; } = new List<BillItem>();
    }

    public class BillItem
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = "General"; // Room, Doctor, Medicine, Lab, etc.
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
        public int? PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled"; // Scheduled, In Progress, Completed
        public DateTime ScheduledDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string? Urgency { get; set; } // Low, Medium, High, Critical

        // New Detailed Recommendation Fields
        public int ExpectedStayDays { get; set; }
        public string? RecommendedMedicines { get; set; }
        public string? RecommendedEquipment { get; set; }
        public int? TheaterId { get; set; }
        public int DurationMinutes { get; set; } = 60; // Default 1 hour
        public DateTime? ActualStartTime { get; set; }
        public DateTime EndDate => (ActualStartTime ?? ScheduledDate).AddMinutes(DurationMinutes);
        public string? TheaterName { get; set; } // Joined

        // Admin Approved Estimation Fields
        public decimal? AgreedOperationCost { get; set; }
        public decimal? AgreedMedicineCost { get; set; }
        public decimal? AgreedEquipmentCost { get; set; }

        public string? PatientName { get; set; } // Optional Joined
        public string? DoctorName { get; set; } // Optional Joined
        public bool IsTransferred { get; set; }
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
        public string? TargetRole { get; set; }
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

    public class Staff
    {
        public int StaffId { get; set; }
        public string UserId { get; set; } = string.Empty; // IdentityUser Id

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; // Admin, Nurse, Receptionist, Pharmacist

        public string? Department { get; set; }

        public string? Shift { get; set; } // Morning, Evening, Night

        public decimal Salary { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }

    public class Admission
    {
        public int AdmissionId { get; set; }
        public int PatientId { get; set; }
        public int BedId { get; set; }
        public DateTime AdmissionDate { get; set; } = DateTime.Now;
        public DateTime? DischargeDate { get; set; }
        public string Status { get; set; } = "Admitted"; // Admitted, Discharged
        public string Notes { get; set; } = string.Empty;

        // Optional Joined Properties
        public string? PatientName { get; set; }
        public string? BedNumber { get; set; }
        public string? WardName { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal DailyRate { get; set; }
    }

    public class Ward
    {
        public int WardId { get; set; }
        [Required]
        public string WardName { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Wing { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class RoomType
    {
        public int RoomTypeId { get; set; }
        [Required]
        public string TypeName { get; set; } = string.Empty; // General, Private, Deluxe
        public decimal DailyRate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class Room
    {
        public int RoomId { get; set; }
        public int WardId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional Joined Properties
        public string? WardName { get; set; }
        public string? RoomTypeName { get; set; }
    }

    public class Bed
    {
        public int BedId { get; set; }
        public int RoomId { get; set; }
        public string BedNumber { get; set; } = string.Empty; // e.g. "ICU-101-A"
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance, Cleaning
        public bool IsActive { get; set; } = true;

        // Optional Joined Properties
        public string? RoomNumber { get; set; }
        public string? WardName { get; set; }
    }

    public class UserShift
    {
        public int ShiftId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public decimal StartingCash { get; set; }
        public decimal? EndingCash { get; set; }
        public decimal? ActualCash { get; set; }
        public string Status { get; set; } = "Open";
        public string? Notes { get; set; }

        // Optional Joined Properties
        public string? TellerName { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class DashboardStats
    {
        public decimal TodayRevenue { get; set; }
        public int OccupiedBeds { get; set; }
        public int TotalBeds { get; set; }
        public int StaffOnShift { get; set; }
        public int SurgeriesToday { get; set; }
    }
    public class Payment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, BankTransfer
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? ReferenceNumber { get; set; }
        public string TellerId { get; set; } = string.Empty;
        public int ShiftId { get; set; }
        public string? Remarks { get; set; }
    }

    public class OperationTheater
    {
        public int TheaterId { get; set; }
        [Required]
        public string TheaterName { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Maintenance, InUse
        public bool IsActive { get; set; } = true;
    }
}
