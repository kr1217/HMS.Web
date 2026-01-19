using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Represents an event where a patient was lost due to capacity, capability, or pricing failures.
    /// </summary>
    public class PatientLossEvent
    {
        public int LossId { get; set; }
        public int? PatientId { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.Now;
        public string EntryPoint { get; set; } = string.Empty; // Appointment, Surgery, Admission
        public int? RequestedDepartmentId { get; set; }
        public string? RequestedSpecialization { get; set; }
        public int? RequestedProcedureId { get; set; }
        public int? RequestedDoctorId { get; set; }
        public LossReasonCode LossReasonCode { get; set; }
        public string? LossReasonDetail { get; set; }
        public decimal EstimatedValue { get; set; }
        public string Status { get; set; } = "Lost"; // Lost, Recovered
        public string RecordedBy { get; set; } = "System";
    }
}
