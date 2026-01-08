/*
 * FILE: PatientOperation.cs
 * PURPOSE: Data model representing the PatientOperation entity.
 * COMMUNICATES WITH: PatientOperationRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Tracks planned or completed surgeries.
    /// OPTIMIZATION: [Resource Allocation] Links patients to theaters and doctors with duration estimates.
    /// </summary>
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
}

