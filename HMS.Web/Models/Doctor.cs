/*
 * FILE: Doctor.cs
 * PURPOSE: Data model representing the Doctor entity.
 * COMMUNICATES WITH: DoctorRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    /// <summary>
    /// Represents a medical doctor.
    /// OPTIMIZATION: [Financial Truth] Stores fees and commission rates used during billing calculations.
    /// </summary>
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
}

