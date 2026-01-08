/*
 * FILE: Appointment.cs
 * PURPOSE: Data model representing the Appointment entity.
 * COMMUNICATES WITH: AppointmentRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    /// <summary>
    /// Records a patient-doctor clinical encounter request.
    /// OPTIMIZATION: [Snapshotting] ConsultationFeeAtBooking freezes historical truth at creation.
    /// </summary>
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

        // OPTIMIZATION: [Snapshot at Time of Transaction] 
        // We store the fee at booking so historical reports don't shift if the doctor raises their rates later.
        public decimal ConsultationFeeAtBooking { get; set; }

        public string? DoctorNotes { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RescheduledDate { get; set; }
    }
}

