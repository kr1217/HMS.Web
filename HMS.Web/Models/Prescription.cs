/*
 * FILE: Prescription.cs
 * PURPOSE: Data model representing the Prescription entity.
 * COMMUNICATES WITH: PrescriptionRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Clinical prescription data.
    /// OPTIMIZATION: [JSON Flexibility] Medications are serialized to handle dynamic clinical requirements.
    /// OPTIMIZATION: [Schema Guarding] SchemaVersion ensures future-proof JSON deserialization.
    /// </summary>
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

        // OPTIMIZATION: [JSON Guardrail] 
        // Versioning helps us safely migrate JSON structure in the future without breaking old records.
        public int SchemaVersion { get; set; } = 1;

        public bool IsLocked { get; set; } = false;
        public string? DigitalSignature { get; set; }
    }
}

