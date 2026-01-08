/*
 * FILE: Patient.cs
 * PURPOSE: Data model representing the Patient entity.
 * COMMUNICATES WITH: PatientRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    /// <summary>
    /// Represents a patient in the hospital system.
    /// OPTIMIZATION: [Audit Retention] We use IsActive for soft deletes instead of physical removal.
    /// </summary>
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
}

