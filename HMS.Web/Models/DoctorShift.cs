/*
 * FILE: DoctorShift.cs
 * PURPOSE: Data model representing the DoctorShift entity.
 * COMMUNICATES WITH: DoctorShiftRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
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
}

