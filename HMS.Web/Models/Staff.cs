/*
 * FILE: Staff.cs
 * PURPOSE: Data model representing the Staff entity.
 * COMMUNICATES WITH: StaffRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
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
}

