/*
 * FILE: Admission.cs
 * PURPOSE: Data model representing the Admission entity.
 * COMMUNICATES WITH: AdmissionRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Records a patient's stay in a hospital bed.
    /// OPTIMIZATION: [State Management] Bridges patients, beds, and final discharge billing.
    /// </summary>
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
        public string? RoomNumber { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal DailyRate { get; set; }
        public bool HasPendingBill { get; set; }
    }
}

